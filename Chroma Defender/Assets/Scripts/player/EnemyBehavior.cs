using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tipos de inimigos disponíveis na fase
/// </summary>
public enum EnemyType
{
    Rat,    // Rato: Apenas patrulha de um lado para o outro. Causa dano de contato.
    Slime,  // Slime: Patrulha no chão, persegue e golpeia com estocada ácida.
    Bat,    // Morcego: Voador, patrulha no ar, persegue e dá rasante cortante.
    Mimic   // Mímico: Camuflado, desperta perto do jogador e desfere mordidas brutais.
}

/// <summary>
/// Sistema completo de comportamento de inimigos (IA de patrulha, perseguição, ataque ativo e dano de contato).
/// Totalmente manipulável pelo Inspector do Unity para alterar vida, dano, velocidade, polaridade e animações.
/// Inclui assentamento automático no chão (anti-flutuação) e indicador visual de polaridade.
/// </summary>
[SelectionBase]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemyBehavior : MonoBehaviour, IDamageableTarget
{
    [Header("--- Tipo & Identificação ---")]
    [Tooltip("Selecione o tipo do monstro. Ao alterar, valores padrão ideais podem ser ajustados.")]
    [SerializeField] private EnemyType enemyType = EnemyType.Slime;

    public enum PolarityRuleMode
    {
        MatchElement,    // Inimigo de Gelo exige GELO; Inimigo de Fogo exige FOGO
        OppositeElement  // Inimigo de Gelo exige FOGO; Inimigo de Fogo exige GELO
    }

    [Header("--- Polaridade Elemental (Chroma Defender) ---")]
    [Tooltip("Neutral = Toma dano de tudo | Ice = Elemento Gelo | Fire = Elemento Fogo")]
    [SerializeField] private EnemyPolarityType polarity = EnemyPolarityType.Neutral;
    [Tooltip("OppositeElement (Padrão): Bater com o elemento oposto (Fogo queima Gelo, Gelo apaga Fogo). MatchElement: Bater com o mesmo elemento.")]
    [SerializeField] private PolarityRuleMode polarityRule = PolarityRuleMode.OppositeElement;
    [Tooltip("Inverte o espelhamento do sprite caso o asset original aponte para o lado oposto.")]
    [SerializeField] private bool invertFacing = false;

    [Header("--- Vida & Dano (Ajustável na IDE) ---")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private int currentHealth;
    [Tooltip("Dano causado simplesmente ao encostar no jogador.")]
    [SerializeField] private int contactDamage = 12;
    [Tooltip("Força horizontal do empurrão ao encostar (afasta o jogador para longe do mob).")]
    [SerializeField] private float contactKnockbackX = 8.5f;
    [Tooltip("Força vertical do empurrão ao encostar (mantida baixa para não jogar o jogador para cima do mob).")]
    [SerializeField] private float contactKnockbackY = 2.0f;
    [Tooltip("Dano do golpe físico ativo (para Slime, Morcego e Mímico).")]
    [SerializeField] private int activeAttackDamage = 20;
    [Tooltip("Força horizontal de empurrão aplicada ao jogador no golpe ativo.")]
    [SerializeField] private float attackKnockback = 9f;
    [Tooltip("Força vertical de empurrão no golpe ativo.")]
    [SerializeField] private float attackKnockbackY = 2.2f;
    [Tooltip("Intervalo mínimo em segundos entre danos de contato contínuos.")]
    [SerializeField] private float contactDamageCooldown = 0.8f;

    [Header("--- Comportamento & Ataque ---")]
    [Tooltip("Se falso (ex: Rato), ele apenas anda de um lado para o outro sem ataque ativo.")]
    [SerializeField] private bool canActivelyAttack = true;
    [Tooltip("Distância que o monstro enxerga o jogador para iniciar perseguição.")]
    [SerializeField] private float detectionRadius = 5f;
    [Tooltip("Distância máxima para desferir o golpe ativo.")]
    [SerializeField] private float attackRange = 1.3f;
    [Tooltip("Tempo de recarga entre ataques ativos.")]
    [SerializeField] private float attackCooldown = 1.6f;

    [Header("--- Movimentação & Patrulha ---")]
    [SerializeField] private float patrolSpeed = 1.5f;
    [SerializeField] private float chaseSpeed = 2.4f;
    [Tooltip("Distância em metros que o monstro patrulha para cada lado a partir do spawn.")]
    [SerializeField] private float patrolDistance = 3.5f;
    [Tooltip("Marque verdadeiro para o Morcego (desativa gravidade para planar).")]
    [SerializeField] private bool isFlying = false;

    [Header("--- Animações do Mob (Spritesheets) ---")]
    [SerializeField] private float animFps = 10f;
    [SerializeField] private Sprite[] idleSprites;
    [SerializeField] private Sprite[] moveSprites;
    [SerializeField] private Sprite[] attackSprites;
    [SerializeField] private Sprite[] hurtSprites;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float deathFrameDuration = 0.08f;
    [SerializeField] private bool autoLoadSprites = true;

    // Componentes internos
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private Animator anim;
    private Transform playerTransform;
    private PlayerBehavior playerBehavior;
    private EnemyPolarityHUD polarityHUD;

    // Estados da IA
    private enum State { Patrol, Chase, Attacking, Hurt, Dead }
    private State currentState = State.Patrol;

    // Estados da Animação Visual
    public enum EnemyAnimState { Idle, Move, Attack, Hurt, Death }
    private EnemyAnimState currentVisualState = EnemyAnimState.Idle;
    private Sprite[] activeAnimFrames;
    private int animFrameIndex;
    private float animFrameTimer;

    private float spawnX;
    private float spawnY;
    private int patrolDirection = 1; // 1 = direita, -1 = esquerda
    private float nextAttackTime;
    private float nextContactDamageTime;
    private bool isDead;

    // Propriedades Públicas
    public EnemyType Type => enemyType;
    public EnemyPolarityType Polarity => polarity;
    public PolarityRuleMode PolarityRule => polarityRule;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => isDead;

    public ElementPolarity? RequiredAttackPolarity
    {
        get
        {
            if (polarity == EnemyPolarityType.Neutral) return null;
            if (polarityRule == PolarityRuleMode.OppositeElement)
            {
                // Inimigo de Gelo fraco contra FOGO; Inimigo de Fogo fraco contra GELO (Padrão clássico)
                return (polarity == EnemyPolarityType.Ice) ? ElementPolarity.Fire : ElementPolarity.Ice;
            }
            else
            {
                // Inimigo de Gelo exige GELO; Inimigo de Fogo exige FOGO (Modo Match)
                return (polarity == EnemyPolarityType.Ice) ? ElementPolarity.Ice : ElementPolarity.Fire;
            }
        }
    }

    private void Awake()
    {
        // Se por engano o inimigo foi colocado como filho de outro inimigo, desanexa para manter a física livre
        if (transform.parent != null && (transform.parent.name.Contains("Enemy") || transform.parent.GetComponent<EnemyBehavior>() != null))
        {
            transform.SetParent(null, true);
        }

        // Garante que a escala base X seja positiva para evitar bugs de espelhamento duplo
        if (transform.localScale.x < 0)
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x);
            transform.localScale = s;
        }

        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponent<Animator>();

        if (anim != null && anim.runtimeAnimatorController == null)
        {
            anim.enabled = false;
        }

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (isFlying)
            {
                rb.gravityScale = 0f;
                rb.linearDamping = 1f;
            }
            else
            {
                rb.gravityScale = 2.5f;
                rb.linearDamping = 0.05f;
            }
        }

        currentHealth = maxHealth;
        spawnX = transform.position.x;
        spawnY = transform.position.y;

        if (enemyType == EnemyType.Rat)
        {
            canActivelyAttack = false;
        }

        // NÃO sobrescrevemos nem bloqueamos o colisor do usuário! O usuário tem controle livre no Inspector.
        LoadAllSpritesIfEmpty();
        ApplyElementalTint();
        SetupPolarityHUD();
    }

    private void Start()
    {
        spawnX = transform.position.x;
        spawnY = transform.position.y;
        FindPlayer();
        PlayAnimState(EnemyAnimState.Idle);
    }

    private void FindPlayer()
    {
        if (playerBehavior == null)
        {
            playerBehavior = FindFirstObjectByType<PlayerBehavior>();
            if (playerBehavior != null) playerTransform = playerBehavior.transform;
        }
    }

    private void SetupPolarityHUD()
    {
        polarityHUD = GetComponent<EnemyPolarityHUD>();
        if (polarityHUD == null) polarityHUD = gameObject.AddComponent<EnemyPolarityHUD>();
        polarityHUD.Initialize(this);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (isDead || currentState == State.Dead)
        {
            UpdateAnimation(dt);
            return;
        }

        if (playerTransform == null) FindPlayer();

        // Máquina de estados
        switch (currentState)
        {
            case State.Patrol:
                UpdatePatrol();
                CheckForPlayerDetection();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Attacking:
                // Movimento suspenso durante o golpe ativo
                break;
        }

        UpdateAnimation(dt);
    }

    private void FixedUpdate()
    {
        if (isDead || currentState == State.Dead || currentState == State.Attacking || currentState == State.Hurt)
            return;

        if (currentState == State.Patrol)
        {
            if (isFlying)
            {
                // Morcego flutuando suavemente em onda senoidal no ar
                float hoverY = spawnY + Mathf.Sin(Time.time * 2.5f) * 0.45f;
                rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, (hoverY - transform.position.y) * 3f);
            }
            else if (rb != null)
            {
                // Mobs terrestres (Rato, Slime, Mímico): move suavemente com a física e gravidade natural do Unity
                rb.linearVelocity = new Vector2(patrolDirection * patrolSpeed, rb.linearVelocity.y);
            }
        }
        else if (currentState == State.Chase && rb != null)
        {
            if (playerTransform != null)
            {
                if (isFlying)
                {
                    Vector2 flyDir = ((Vector2)playerTransform.position + Vector2.up * 0.5f - (Vector2)transform.position).normalized;
                    rb.linearVelocity = flyDir * chaseSpeed;
                }
                else
                {
                    float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
                    rb.linearVelocity = new Vector2(dirX * chaseSpeed, rb.linearVelocity.y);
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Lógica de Patrulha & Mudança de Direção Inteligente
    // ------------------------------------------------------------------
    private void UpdatePatrol()
    {
        float currentX = transform.position.x;

        // 1. Inverte direção ao atingir os limites de patrulha
        if (patrolDirection > 0 && currentX >= spawnX + patrolDistance)
        {
            patrolDirection = -1;
        }
        else if (patrolDirection < 0 && currentX <= spawnX - patrolDistance)
        {
            patrolDirection = 1;
        }

        // 2. Inverte direção ao detectar parede ou obstáculo à frente
        if (CheckObstacleAhead())
        {
            patrolDirection = -patrolDirection;
        }

        // 3. Mobs terrestres invertem ao atingir beiradas de plataformas
        if (!isFlying && CheckLedgeAhead())
        {
            patrolDirection = -patrolDirection;
        }

        UpdateFacing(patrolDirection);

        if (currentVisualState != EnemyAnimState.Hurt && currentVisualState != EnemyAnimState.Attack)
        {
            PlayAnimState(EnemyAnimState.Move);
        }
    }

    private bool CheckObstacleAhead()
    {
        if (col == null) return false;
        Vector2 origin = (Vector2)col.bounds.center;
        Vector2 dir = Vector2.right * patrolDirection;
        float checkDist = col.bounds.extents.x + 0.35f;

        RaycastHit2D hit = Physics2D.Raycast(origin, dir, checkDist, LayerMask.GetMask("Ground", "Default"));
        if (hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject && !hit.collider.transform.IsChildOf(transform))
        {
            return true;
        }
        return false;
    }

    private bool CheckLedgeAhead()
    {
        if (isFlying || col == null) return false;
        Vector2 aheadOrigin = new Vector2(transform.position.x + patrolDirection * (col.bounds.extents.x + 0.15f), col.bounds.min.y + 0.1f);
        RaycastHit2D groundHit = Physics2D.Raycast(aheadOrigin, Vector2.down, 0.75f, LayerMask.GetMask("Ground", "Default"));
        return groundHit.collider == null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Se bater contra uma parede lateral ou obstáculo, inverte imediatamente
        if (currentState == State.Patrol && !collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.4f)
                {
                    if (Mathf.Sign(contact.normal.x) != patrolDirection)
                    {
                        patrolDirection = -patrolDirection;
                        UpdateFacing(patrolDirection);
                        break;
                    }
                }
            }
        }
    }

    // ------------------------------------------------------------------
    // Detecção e Perseguição
    // ------------------------------------------------------------------
    private void CheckForPlayerDetection()
    {
        if (!canActivelyAttack || playerTransform == null || playerBehavior.IsDead) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= detectionRadius)
        {
            currentState = State.Chase;
        }
    }

    private void UpdateChase()
    {
        if (playerTransform == null || playerBehavior.IsDead)
        {
            currentState = State.Patrol;
            PlayAnimState(EnemyAnimState.Idle);
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        // Se o jogador fugiu para longe, retorna à patrulha
        if (dist > detectionRadius * 1.35f)
        {
            currentState = State.Patrol;
            PlayAnimState(EnemyAnimState.Idle);
            return;
        }

        // Se está ao alcance do golpe ativo e fora de cooldown, ataca!
        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(ActiveAttackRoutine());
            return;
        }

        // Persegue o jogador
        float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);
        UpdateFacing((int)dirX);

        if (currentVisualState != EnemyAnimState.Hurt && currentVisualState != EnemyAnimState.Attack)
        {
            PlayAnimState(EnemyAnimState.Move);
        }
    }

    // ------------------------------------------------------------------
    // Ataque Ativo
    // ------------------------------------------------------------------
    private IEnumerator ActiveAttackRoutine()
    {
        currentState = State.Attacking;
        nextAttackTime = Time.time + attackCooldown;

        // Para movimentação durante o golpe
        if (rb != null) rb.linearVelocity = new Vector2(0f, isFlying ? 0f : rb.linearVelocity.y);

        float facing = Mathf.Sign(playerTransform.position.x - transform.position.x);
        UpdateFacing((int)facing);

        // Toca animação de ataque
        PlayAnimState(EnemyAnimState.Attack, true);

        // Dá um pequeno salto / bote na direção do jogador
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(facing * (chaseSpeed * 1.3f), isFlying ? -1.5f : 2.5f);
        }

        // Tempo de antecipação do golpe
        yield return new WaitForSeconds(0.28f);

        // Verifica acerto no jogador
        if (playerTransform != null && !playerBehavior.IsDead && !playerBehavior.IsInvulnerable)
        {
            float hitDist = Vector2.Distance(transform.position, playerTransform.position);
            if (hitDist <= attackRange * 1.35f)
            {
                Vector2 knock = new Vector2(facing * attackKnockback, attackKnockbackY);
                playerBehavior.TakeDamage(activeAttackDamage, knock);

                // Feedback visual de golpe do monstro
                if (polarity == EnemyPolarityType.Ice) IceVFX.SpawnFrostBurst(playerTransform.position, 12);
                else if (polarity == EnemyPolarityType.Fire) FireVFX.SpawnFlameBurst(playerTransform.position, 14);
            }
        }

        // Tempo de recuperação do ataque
        yield return new WaitForSeconds(0.25f);

        // Se for mob voador (Morcego), executa recuo tático para ganhar distância
        // e inverte a posição ao voar para trás para não dar a impressão de voar de costas sem animação
        if (isFlying && rb != null && currentState != State.Dead)
        {
            float retreatDir = -facing;
            UpdateFacing((int)retreatDir); // Inverte posição para o voo de recuo
            rb.linearVelocity = new Vector2(retreatDir * (chaseSpeed * 1.1f), 2.2f);
            yield return new WaitForSeconds(0.45f);
        }

        if (currentState != State.Dead)
        {
            currentState = State.Chase;
            PlayAnimState(EnemyAnimState.Move);
        }
    }

    // ------------------------------------------------------------------
    // Dano de Contato (Ao Encostar no Jogador)
    // ------------------------------------------------------------------
    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealContactDamage(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDealContactDamage(other.gameObject);
    }

    private void TryDealContactDamage(GameObject target)
    {
        if (isDead || currentState == State.Dead) return;
        if (Time.time < nextContactDamageTime) return;

        PlayerBehavior player = target.GetComponent<PlayerBehavior>();
        if (player == null) player = target.GetComponentInParent<PlayerBehavior>();

        if (player != null && !player.IsDead && !player.IsInvulnerable)
        {
            nextContactDamageTime = Time.time + contactDamageCooldown;

            float pushDir = Mathf.Sign(player.transform.position.x - transform.position.x);
            if (pushDir == 0) pushDir = patrolDirection != 0 ? patrolDirection : 1f;

            // Knockback forte horizontal que arremessa para longe, com elevação baixa para não cair em cima do mob
            Vector2 knockback = new Vector2(pushDir * contactKnockbackX, contactKnockbackY);
            player.TakeDamage(contactDamage, knockback);
        }
    }

    // ------------------------------------------------------------------
    // Interface IDamageableTarget (Receber Dano do Cavaleiro)
    // ------------------------------------------------------------------
    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (isDead) return;

        // Regra de Polaridade Rigorosa (Chroma Defender)
        ElementPolarity? required = RequiredAttackPolarity;
        if (required.HasValue && attackPolarity != required.Value)
        {
            string reqStr = (required.Value == ElementPolarity.Fire) ? "<color=orange>FOGO</color>" : "<color=cyan>GELO</color>";
            string attStr = (attackPolarity == ElementPolarity.Fire) ? "<color=orange>FOGO</color>" : "<color=cyan>GELO</color>";
            Debug.Log($"<color=gray>[IMUNE]</color> Inimigo resistiu ao golpe de {attStr}! É obrigatório usar: {reqStr}!");
            StartCoroutine(FlashRoutine(Color.gray));
            return;
        }

        currentHealth -= damage;
        if (polarityHUD != null) polarityHUD.UpdateHealthDisplay();

        string elemStr = (attackPolarity == ElementPolarity.Fire) ? "<color=orange>FOGO</color>" : "<color=cyan>GELO</color>";
        Debug.Log($"<color=white>[{enemyType}]</color> Recebeu {damage} de {elemStr}! Vida restante: {Mathf.Max(0, currentHealth)}/{maxHealth}");

        // Aplica Knockback
        if (rb != null)
        {
            rb.linearVelocity = knockback;
        }

        // Partículas de Impacto
        Vector3 center = col != null ? (Vector3)col.bounds.center : transform.position;
        if (attackPolarity == ElementPolarity.Ice) IceVFX.SpawnFrostBurst(center, 14);
        else FireVFX.SpawnFlameBurst(center, 16);

        StartCoroutine(FlashRoutine(Color.white));
        PlayAnimState(EnemyAnimState.Hurt, true);

        if (currentHealth <= 0)
        {
            Die(attackPolarity);
        }
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        if (sr == null) yield break;
        Color original = sr.color;
        sr.color = flashColor;
        yield return new WaitForSeconds(0.12f);
        if (sr != null) sr.color = original;
    }

    private void Die(ElementPolarity deathElement)
    {
        if (isDead) return;
        isDead = true;
        currentState = State.Dead;

        if (col != null) col.enabled = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (polarityHUD != null) polarityHUD.UpdateHealthDisplay();

        // Explosão de morte elemental
        Vector3 center = col != null ? (Vector3)col.bounds.center : transform.position;
        if (deathElement == ElementPolarity.Ice) IceVFX.SpawnFrostBurst(center, 28);
        else FireVFX.SpawnFlameBurst(center, 30);

        PlayAnimState(EnemyAnimState.Death, true);
        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        if (deathFrames != null && deathFrames.Length > 0 && sr != null)
        {
            for (int i = 0; i < deathFrames.Length; i++)
            {
                if (deathFrames[i] != null) sr.sprite = deathFrames[i];
                yield return new WaitForSeconds(deathFrameDuration);
            }
        }
        else
        {
            yield return new WaitForSeconds(0.35f);
        }

        // Fade-out suave
        if (sr != null)
        {
            float elapsed = 0f;
            Color c = sr.color;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(1f, 0f, elapsed / 0.4f);
                sr.color = c;
                yield return null;
            }
        }

        Destroy(gameObject);
    }

    // ------------------------------------------------------------------
    // Motor de Animação de Sprites do Mob
    // ------------------------------------------------------------------
    public void PlayAnimState(EnemyAnimState state, bool forceRestart = false)
    {
        if (currentVisualState == state && !forceRestart) return;
        if (currentVisualState == EnemyAnimState.Death && state != EnemyAnimState.Death) return;

        currentVisualState = state;
        animFrameIndex = 0;
        animFrameTimer = 0f;

        switch (state)
        {
            case EnemyAnimState.Move:
                activeAnimFrames = (moveSprites != null && moveSprites.Length > 0) ? moveSprites : idleSprites;
                break;
            case EnemyAnimState.Attack:
                activeAnimFrames = (attackSprites != null && attackSprites.Length > 0) ? attackSprites : moveSprites;
                break;
            case EnemyAnimState.Hurt:
                activeAnimFrames = (hurtSprites != null && hurtSprites.Length > 0) ? hurtSprites : idleSprites;
                break;
            case EnemyAnimState.Death:
                activeAnimFrames = (deathFrames != null && deathFrames.Length > 0) ? deathFrames : idleSprites;
                break;
            default:
                activeAnimFrames = (idleSprites != null && idleSprites.Length > 0) ? idleSprites : moveSprites;
                break;
        }

        if (activeAnimFrames != null && activeAnimFrames.Length > 0 && sr != null)
        {
            sr.sprite = activeAnimFrames[0];
        }
    }

    private void UpdateAnimation(float dt)
    {
        if (activeAnimFrames == null || activeAnimFrames.Length == 0 || sr == null) return;

        animFrameTimer += dt;
        float frameDuration = 1f / Mathf.Max(1f, animFps);
        if (animFrameTimer >= frameDuration)
        {
            animFrameTimer -= frameDuration;
            animFrameIndex++;

            if (animFrameIndex >= activeAnimFrames.Length)
            {
                if (currentVisualState == EnemyAnimState.Attack || currentVisualState == EnemyAnimState.Hurt)
                {
                    PlayAnimState(currentState == State.Chase || currentState == State.Patrol ? EnemyAnimState.Move : EnemyAnimState.Idle);
                    return;
                }
                else if (currentVisualState == EnemyAnimState.Death)
                {
                    animFrameIndex = activeAnimFrames.Length - 1;
                }
                else
                {
                    animFrameIndex = 0;
                }
            }

            if (animFrameIndex < activeAnimFrames.Length && activeAnimFrames[animFrameIndex] != null)
            {
                sr.sprite = activeAnimFrames[animFrameIndex];
            }
        }
    }

    private void UpdateFacing(int dir)
    {
        if (dir == 0 || sr == null) return;

        // O asset original do Morcego, Rato e Slime foram desenhados olhando para a DIREITA.
        bool defaultFacingRight = (enemyType == EnemyType.Bat || enemyType == EnemyType.Rat || enemyType == EnemyType.Slime);

        bool flip;
        if (defaultFacingRight)
        {
            // Sprite desenhado para a direita: flipX ao mover para a esquerda
            flip = (dir < 0);
        }
        else
        {
            // Sprite desenhado para a esquerda: flipX ao mover para a direita
            flip = (dir > 0);
        }

        if (invertFacing) flip = !flip;
        sr.flipX = flip;
    }

    public void ApplyElementalTint()
    {
        if (sr == null) return;
        switch (polarity)
        {
            case EnemyPolarityType.Ice:
                sr.color = new Color(0.45f, 0.85f, 1f, 1f);
                break;
            case EnemyPolarityType.Fire:
                sr.color = new Color(1f, 0.55f, 0.25f, 1f);
                break;
            default:
                sr.color = Color.white;
                break;
        }

        if (polarityHUD != null) polarityHUD.UpdatePolarityDisplay();
    }

    private void LoadAllSpritesIfEmpty()
    {
        if (!autoLoadSprites) return;

#if UNITY_EDITOR
        string folder = "";
        switch (enemyType)
        {
            case EnemyType.Rat:
                folder = "Assets/Monsters Creatures Fantasy 2/Sprites/Rat";
                if (idleSprites == null || idleSprites.Length == 0) idleSprites = LoadSpritesFromPath($"{folder}/idle.png");
                if (moveSprites == null || moveSprites.Length == 0) moveSprites = LoadSpritesFromPath($"{folder}/run.png");
                if (attackSprites == null || attackSprites.Length == 0) attackSprites = LoadSpritesFromPath($"{folder}/attack_bite.png");
                if (hurtSprites == null || hurtSprites.Length == 0) hurtSprites = LoadSpritesFromPath($"{folder}/hurt.png");
                if (deathFrames == null || deathFrames.Length == 0) deathFrames = LoadSpritesFromPath($"{folder}/rat-death.png");
                break;

            case EnemyType.Slime:
                folder = "Assets/Monsters Creatures Fantasy 2/Sprites/Slime";
                if (idleSprites == null || idleSprites.Length == 0) idleSprites = LoadSpritesFromPath($"{folder}/idle.png");
                if (moveSprites == null || moveSprites.Length == 0) moveSprites = LoadSpritesFromPath($"{folder}/walk.png");
                if (attackSprites == null || attackSprites.Length == 0) attackSprites = LoadSpritesFromPath($"{folder}/attack.png");
                if (hurtSprites == null || hurtSprites.Length == 0) hurtSprites = LoadSpritesFromPath($"{folder}/hurt.png");
                if (deathFrames == null || deathFrames.Length == 0) deathFrames = LoadSpritesFromPath($"{folder}/death.png");
                break;

            case EnemyType.Bat:
                folder = "Assets/Monsters Creatures Fantasy 2/Sprites/Bat";
                if (idleSprites == null || idleSprites.Length == 0) idleSprites = LoadSpritesFromPath($"{folder}/fly.png");
                if (moveSprites == null || moveSprites.Length == 0) moveSprites = LoadSpritesFromPath($"{folder}/fly.png");
                if (attackSprites == null || attackSprites.Length == 0) attackSprites = LoadSpritesFromPath($"{folder}/attack.png");
                if (hurtSprites == null || hurtSprites.Length == 0) hurtSprites = LoadSpritesFromPath($"{folder}/hurt.png");
                if (deathFrames == null || deathFrames.Length == 0) deathFrames = LoadSpritesFromPath($"{folder}/death.png");
                break;

            case EnemyType.Mimic:
                folder = "Assets/Monsters Creatures Fantasy 2/Sprites/Mimic";
                if (idleSprites == null || idleSprites.Length == 0) idleSprites = LoadSpritesFromPath($"{folder}/Idle_closed.png");
                if (moveSprites == null || moveSprites.Length == 0) moveSprites = LoadSpritesFromPath($"{folder}/walk.png");
                if (attackSprites == null || attackSprites.Length == 0) attackSprites = LoadSpritesFromPath($"{folder}/attack_1.png");
                if (hurtSprites == null || hurtSprites.Length == 0) hurtSprites = LoadSpritesFromPath($"{folder}/hurt.png");
                if (deathFrames == null || deathFrames.Length == 0) deathFrames = LoadSpritesFromPath($"{folder}/death.png");
                break;
        }
#endif
    }

    private Sprite[] LoadSpritesFromPath(string path)
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(path)) return null;
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
        var list = new List<Sprite>();
        foreach (var a in assets)
        {
            if (a is Sprite s) list.Add(s);
        }
        if (list.Count > 0)
        {
            list.Sort((a, b) => AlphanumericSort(a.name, b.name));
            return list.ToArray();
        }
#endif
        return null;
    }

    private int AlphanumericSort(string s1, string s2)
    {
        int n1 = ExtractTrailingNumber(s1);
        int n2 = ExtractTrailingNumber(s2);
        if (n1 >= 0 && n2 >= 0)
        {
            return n1.CompareTo(n2);
        }
        return string.Compare(s1, s2, System.StringComparison.OrdinalIgnoreCase);
    }

    private int ExtractTrailingNumber(string s)
    {
        int lastUnder = s.LastIndexOf('_');
        if (lastUnder >= 0 && lastUnder < s.Length - 1)
        {
            if (int.TryParse(s.Substring(lastUnder + 1), out int result)) return result;
        }
        int lastDash = s.LastIndexOf('-');
        if (lastDash >= 0 && lastDash < s.Length - 1)
        {
            if (int.TryParse(s.Substring(lastDash + 1), out int result)) return result;
        }
        return -1;
    }

    private void OnValidate()
    {
        // NÃO altera nem sobrescreve os colisores ajustados pelo usuário no Inspector!
        LoadAllSpritesIfEmpty();
        ApplyElementalTint();
        if (polarityHUD != null) polarityHUD.UpdatePolarityDisplay();
    }

    // ------------------------------------------------------------------
    // Visualização Intuitiva no Scene View (Gizmos)
    // ------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        Vector3 basePos = Application.isPlaying ? new Vector3(spawnX, spawnY, transform.position.z) : transform.position;

        // Rota de patrulha (Linha amarela com pontas)
        Gizmos.color = Color.yellow;
        Vector3 leftPoint = basePos + Vector3.left * patrolDistance;
        Vector3 rightPoint = basePos + Vector3.right * patrolDistance;
        Gizmos.DrawLine(leftPoint, rightPoint);
        Gizmos.DrawWireCube(leftPoint, new Vector3(0.3f, 0.6f, 0.1f));
        Gizmos.DrawWireCube(rightPoint, new Vector3(0.3f, 0.6f, 0.1f));

        // Raio de Visão / Detecção (Círculo verde)
        if (canActivelyAttack)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Alcance de Ataque (Círculo vermelho)
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
