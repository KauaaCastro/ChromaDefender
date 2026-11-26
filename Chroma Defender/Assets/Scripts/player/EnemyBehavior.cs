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
    Mimic,  // Mímico: Camuflado, desperta perto do jogador e desfere mordidas brutais.

    // --- Fase 2: Castelo (asset "Hero and Opponents") ---
    SkeletonSword,  // Esqueleto Espadachim: corpo a corpo, golpe com espada.
    SkeletonShield, // Esqueleto Escudeiro: o escudo BLOQUEIA golpes pela frente (ataque pelas costas ou com golpe pesado).
    SkeletonSpear,  // Esqueleto Lanceiro: alcance longo com a lança.
    Archer,         // Arqueiro: mantém distância e atira flechas.

    // --- Fase 3: Área Mágica ---
    ShadowSpikes,   // Sombra Espinhosa: monte de sombra lento que explode em espinhos quando o jogador chega perto.
    DarkMage        // Mago Sombrio (asset "EVil Wizard"): conjura orbes elementais à distância.
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

    [Header("--- Balanceamento de Combate ---")]
    [Tooltip("Segundos atordoado depois de levar um golpe: o empurrão funciona, ele não persegue e não causa dano de contato.")]
    [SerializeField] private float hurtStunTime = 0.45f;
    [Tooltip("Levar um golpe cancela o ataque que o mob estava preparando.")]
    [SerializeField] private bool hitInterruptsAttack = true;
    [Tooltip("Vida devolvida ao jogador quando este mob é derrotado. -1 = automático pelo tipo (Rato 6, Slime 12, Morcego 12, Mímico 20).")]
    [SerializeField] private int healOnDeath = -1;

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

    [Header("--- Golpe Ativo (ajuste fino) ---")]
    [Tooltip("Segundos entre o início da animação de ataque e o momento do acerto (ou do disparo).")]
    [SerializeField] private float attackWindup = 0.28f;
    [Tooltip("Força do 'bote' na direção do jogador ao atacar (multiplica a velocidade de perseguição). 0 = ataca parado.")]
    [SerializeField] private float attackLungeMultiplier = 1.3f;
    [Tooltip("Aviso ANTES do golpe: o inimigo para, pisca e mostra \"!\" por este tempo. Durante o aviso e o golpe ele fica " +
             "vulnerável (o escudo abaixa) — janela para contra-atacar. 0 = sem aviso.")]
    [SerializeField] private float attackTelegraph = 0f;
    [Tooltip("Segundos parado depois do golpe (recuperação, ainda vulnerável).")]
    [SerializeField] private float attackRecovery = 0.25f;
    [Tooltip("Segundos que leva para se virar quando o jogador passa para as suas costas. 0 = vira na hora.")]
    [SerializeField] private float turnAroundDelay = 0f;

    [Header("--- Ataque à Distância (Arqueiro / Mago) ---")]
    [SerializeField] private bool rangedAttack = false;
    [SerializeField] private Sprite[] projectileSprites;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileScale = 1f;
    [Tooltip("Curva do projétil em direção ao jogador (0 = reto).")]
    [SerializeField] private float projectileHoming = 0f;
    [Tooltip("Ponto de saída do projétil, relativo aos pés do inimigo (X para a frente).")]
    [SerializeField] private Vector2 projectileSpawnOffset = new Vector2(0.5f, 0.6f);
    [Tooltip("Ligado: projétil do mesmo elemento do inimigo (pode ser absorvido pelo jogador com o mesmo elemento). Desligado: neutro.")]
    [SerializeField] private bool projectileUsesPolarity = false;
    [Tooltip("Se o jogador chegar mais perto que isso, o inimigo recua (0 = nunca recua).")]
    [SerializeField] private float keepDistance = 0f;

    [Header("--- Escudo Frontal (Esqueleto Escudeiro) ---")]
    [SerializeField] private bool blocksFrontalHits = false;
    [Tooltip("Golpes com dano igual ou maior que este quebram a guarda mesmo pela frente (golpes pesados).")]
    [SerializeField] private int guardBreakDamage = 50;

    [Header("--- Alinhamento da Arte ---")]
    [Tooltip("Distância (em unidades, já com a escala) do centro do quadro até o centro do corpo, na arte original. " +
             "Só funciona com o SpriteRenderer num objeto filho (\"Visual\").")]
    [SerializeField] private float spriteBodyOffsetX = 0f;

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
    private float hurtUntil;
    private Coroutine attackRoutine;
    private int facingDir = 1; // 1 = olhando para a direita, -1 = esquerda
    private float nextBlockTextTime;
    private float turnTimer;    // tempo acumulado com o jogador às costas (turnAroundDelay)
    private bool isTurning;     // jogador às costas, ainda "percebendo" — fica parado sem atacar
    private bool telegraphing;
    private Color telegraphBaseColor = Color.white;

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

            case State.Hurt:
                // Atordoado: o empurrão do golpe age livremente (FixedUpdate não mexe na velocidade)
                if (Time.time >= hurtUntil)
                {
                    currentState = canActivelyAttack ? State.Chase : State.Patrol;
                    if (isFlying && rb != null) rb.linearVelocity *= 0.3f;
                }
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
                    float dx = playerTransform.position.x - transform.position.x;
                    float dirX = Mathf.Sign(dx);
                    float vx = isTurning ? 0f : dirX * chaseSpeed; // virando: fica parado (não anda de costas)
                    float adx = Mathf.Abs(dx);
                    if (keepDistance > 0f && adx < keepDistance) vx = -dirX * chaseSpeed * 0.8f; // recua para manter distância
                    else if (rangedAttack && adx <= attackRange * 0.85f) vx = 0f;               // já no alcance: para e mira
                    if (vx != 0f && (rangedAttack || keepDistance > 0f) && CheckLedgeInDirection((int)Mathf.Sign(vx))) vx = 0f; // não cai da plataforma
                    rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
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

    private bool CheckLedgeAhead() => CheckLedgeInDirection(patrolDirection);

    private bool CheckLedgeInDirection(int dir)
    {
        if (isFlying || col == null || dir == 0) return false;
        Vector2 aheadOrigin = new Vector2(col.bounds.center.x + dir * (col.bounds.extents.x + 0.15f), col.bounds.min.y + 0.1f);
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

        float dirX = Mathf.Sign(playerTransform.position.x - transform.position.x);

        // Demora para se virar quando o jogador passa para as costas (Escudeiro): janela para golpes por trás.
        isTurning = false;
        if (turnAroundDelay > 0f && (int)dirX != facingDir && Mathf.Abs(playerTransform.position.x - transform.position.x) > 0.15f)
        {
            turnTimer += Time.deltaTime;
            if (turnTimer < turnAroundDelay)
            {
                isTurning = true;
                if (currentVisualState != EnemyAnimState.Hurt && currentVisualState != EnemyAnimState.Attack)
                    PlayAnimState(EnemyAnimState.Idle);
                return; // não vira e não ataca ainda
            }
            // Terminou de virar: não ataca no mesmo instante.
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + 0.4f);
        }
        turnTimer = 0f;

        // Se está ao alcance do golpe ativo e fora de cooldown, ataca!
        if (dist <= attackRange && Time.time >= nextAttackTime)
        {
            attackRoutine = StartCoroutine(ActiveAttackRoutine());
            return;
        }

        // Persegue o jogador
        UpdateFacing((int)dirX);

        if (currentVisualState != EnemyAnimState.Hurt && currentVisualState != EnemyAnimState.Attack)
        {
            // Atirador parado no alcance, esperando a recarga: fica em pose de espera em vez de "andar no lugar".
            float adx = Mathf.Abs(playerTransform.position.x - transform.position.x);
            bool holding = rangedAttack && adx <= attackRange * 0.85f && !(keepDistance > 0f && adx < keepDistance);
            PlayAnimState(holding ? EnemyAnimState.Idle : EnemyAnimState.Move);
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

        // Aviso antes do golpe: parado, piscando, com "!" — o escudo já está abaixado (estado Attacking).
        if (attackTelegraph > 0f)
        {
            PlayAnimState(EnemyAnimState.Idle, true);
            FloatingText.Spawn(transform.position + Vector3.up * 2f, "!", new Color(1f, 0.55f, 0.2f));
            Color baseColor = sr != null ? sr.color : Color.white;
            telegraphBaseColor = baseColor;
            telegraphing = true;
            float t = 0f;
            while (t < attackTelegraph)
            {
                t += Time.deltaTime;
                if (sr != null) sr.color = Color.Lerp(baseColor, new Color(1f, 0.6f, 0.35f), Mathf.PingPong(t * 8f, 1f) * 0.6f);
                if (rb != null && !isFlying) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                yield return null;
            }
            telegraphing = false;
            if (sr != null) sr.color = baseColor;
            if (playerTransform == null || playerBehavior.IsDead) { currentState = State.Chase; yield break; }
        }

        // Toca animação de ataque
        PlayAnimState(EnemyAnimState.Attack, true);

        // Dá um pequeno salto / bote na direção do jogador
        if (rb != null && attackLungeMultiplier > 0f && !rangedAttack)
        {
            rb.linearVelocity = new Vector2(facing * (chaseSpeed * attackLungeMultiplier), isFlying ? -1.5f : 2.5f);
        }

        // Tempo de antecipação do golpe
        yield return new WaitForSeconds(attackWindup);

        if (rangedAttack)
        {
            FireProjectile(facing);
        }
        // Verifica acerto no jogador
        else if (playerTransform != null && !playerBehavior.IsDead && !playerBehavior.IsInvulnerable)
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
        yield return new WaitForSeconds(attackRecovery);

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

    /// <summary>Dispara uma flecha / orbe na direção do jogador (Arqueiro, Mago Sombrio).</summary>
    private void FireProjectile(float facing)
    {
        if (playerTransform == null || playerBehavior == null || playerBehavior.IsDead) return;

        Vector3 origin = transform.position + new Vector3(facing * projectileSpawnOffset.x, projectileSpawnOffset.y, 0f);
        if (col != null) origin.y = col.bounds.min.y + projectileSpawnOffset.y;

        Vector2 target = (Vector2)playerTransform.position + Vector2.up * 0.6f;
        Vector2 dir = target - (Vector2)origin;
        // Atiradores de chão miram quase na horizontal (a flecha não "persegue" o jogador no ar).
        if (projectileHoming <= 0f) dir.y = Mathf.Clamp(dir.y, -Mathf.Abs(dir.x) * 0.35f, Mathf.Abs(dir.x) * 0.35f);
        if (dir.sqrMagnitude < 0.001f) dir = new Vector2(facing, 0f);
        dir.Normalize();

        EnemyPolarityType elem = projectileUsesPolarity ? polarity : EnemyPolarityType.Neutral;
        int order = sr != null ? sr.sortingOrder + 1 : 20;
        Color? tint = elem == EnemyPolarityType.Neutral ? Color.white : (Color?)null;
        EnemyProjectile.Spawn(origin, dir * projectileSpeed, activeAttackDamage, elem, projectileSprites,
            projectileScale, 3.5f, projectileHoming, order, attackKnockback * 0.6f, tint);
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
        if (isDead || currentState == State.Dead || currentState == State.Hurt) return;
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

        // Golpe durante o aviso: devolve a cor normal antes do flash (senão o tom laranja ficaria preso).
        if (telegraphing && sr != null) { telegraphing = false; sr.color = telegraphBaseColor; }

        // Escudo frontal: golpes vindos da frente são bloqueados (exceto golpes pesados), a menos que ele esteja atacando.
        if (blocksFrontalHits && currentState != State.Attacking && damage < guardBreakDamage && IsAttackFromFront())
        {
            if (rb != null) rb.linearVelocity = new Vector2(-facingDir * 1.5f, rb.linearVelocity.y);
            StartCoroutine(FlashRoutine(new Color(0.75f, 0.75f, 0.75f)));
            if (Time.time >= nextBlockTextTime)
            {
                nextBlockTextTime = Time.time + 1.2f;
                FloatingText.Spawn(transform.position + Vector3.up * 1.8f, "Bloqueado!", new Color(0.85f, 0.85f, 0.85f));
            }
            return;
        }

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
            return;
        }

        EnterHurtStun();
    }

    /// <summary>
    /// Atordoamento curto ao levar golpe. Antes, o estado Hurt nunca era usado: a perseguição anulava o empurrão
    /// no quadro seguinte e o mob continuava colado no jogador, trocando golpe por dano de contato.
    /// </summary>
    private void EnterHurtStun()
    {
        if (hurtStunTime <= 0f) return;

        if (currentState == State.Attacking)
        {
            if (!hitInterruptsAttack) return;
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        currentState = State.Hurt;
        hurtUntil = Time.time + hurtStunTime;
        nextContactDamageTime = Mathf.Max(nextContactDamageTime, hurtUntil + 0.15f);
        nextAttackTime = Mathf.Max(nextAttackTime, hurtUntil + 0.35f);
    }

    private int HealAmount()
    {
        if (healOnDeath >= 0) return healOnDeath;
        switch (enemyType)
        {
            case EnemyType.Rat: return 6;
            case EnemyType.Mimic: return 20;
            default: return 12; // Slime e Morcego
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
        if (attackRoutine != null) { StopCoroutine(attackRoutine); attackRoutine = null; }

        // Recompensa: devolve um pouco de vida ao jogador
        int heal = HealAmount();
        if (heal > 0)
        {
            if (playerBehavior == null) FindPlayer();
            if (playerBehavior != null && !playerBehavior.IsDead)
            {
                int healed = playerBehavior.Heal(heal);
                if (healed > 0) FloatingText.Spawn(playerBehavior.transform.position + Vector3.up * 1.6f, "+" + healed, new Color(0.45f, 1f, 0.5f));
            }
        }

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

    /// <summary>O jogador (quem bate) está do lado para onde o inimigo está olhando?</summary>
    private bool IsAttackFromFront()
    {
        if (playerTransform == null) FindPlayer();
        if (playerTransform == null) return true;
        float side = Mathf.Sign(playerTransform.position.x - transform.position.x);
        return side == facingDir;
    }

    private void UpdateFacing(int dir)
    {
        if (dir == 0 || sr == null) return;
        facingDir = dir > 0 ? 1 : -1;

        // O asset original do Morcego, Rato e Slime foram desenhados olhando para a DIREITA
        // (assim como os esqueletos, o arqueiro, a sombra e o mago das Fases 2 e 3).
        bool defaultFacingRight = (enemyType == EnemyType.Bat || enemyType == EnemyType.Rat || enemyType == EnemyType.Slime
            || (int)enemyType >= (int)EnemyType.SkeletonSword);

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

        // Arte com o corpo fora do centro do quadro: desloca o Visual para o corpo ficar sobre o colisor.
        if (spriteBodyOffsetX != 0f && sr.transform != transform)
        {
            Vector3 lp = sr.transform.localPosition;
            sr.transform.localPosition = new Vector3(flip ? spriteBodyOffsetX : -spriteBodyOffsetX, lp.y, lp.z);
        }
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

            // --- Fase 2 / Fase 3: quadros soltos (idle-1.png, idle-2.png...) do pacote "Hero and Opponents" ---
            case EnemyType.SkeletonSword:  LoadHeroOpponentFrames("Enemy1", "walk", "attack-A"); break;
            case EnemyType.SkeletonShield: LoadHeroOpponentFrames("Enemy2", "walk", "attack-A"); break;
            case EnemyType.SkeletonSpear:  LoadHeroOpponentFrames("Enemy4", "walk", "attack-A"); break;
            case EnemyType.Archer:         LoadHeroOpponentFrames("Enemy5", "run", "attack-A"); break;
            case EnemyType.ShadowSpikes:   LoadHeroOpponentFrames("Enemy3", "walk", "attack-C"); break;

            case EnemyType.DarkMage:
                folder = "Assets/EVil Wizard/Sprites";
                if (idleSprites == null || idleSprites.Length == 0) idleSprites = LoadSpritesFromPath($"{folder}/Idle.png");
                if (moveSprites == null || moveSprites.Length == 0) moveSprites = LoadSpritesFromPath($"{folder}/Move.png");
                if (attackSprites == null || attackSprites.Length == 0) attackSprites = LoadSpritesFromPath($"{folder}/Attack.png");
                if (hurtSprites == null || hurtSprites.Length == 0) hurtSprites = LoadSpritesFromPath($"{folder}/Take Hit.png");
                if (deathFrames == null || deathFrames.Length == 0) deathFrames = LoadSpritesFromPath($"{folder}/Death.png");
                if (projectileSprites == null || projectileSprites.Length == 0) projectileSprites = LoadSpritesFromPath("Assets/Evil Wizard 3/Sprites/Projectile/Moving.png");
                break;
        }
#endif
    }

    private void LoadHeroOpponentFrames(string enemyFolder, string movePrefix, string attackPrefix)
    {
#if UNITY_EDITOR
        string folder = "Assets/Hero and Opponents/Sprites/" + enemyFolder;
        if (idleSprites == null || idleSprites.Length == 0) idleSprites = LoadFramesFromFolder(folder, "idle-");
        if (moveSprites == null || moveSprites.Length == 0) moveSprites = LoadFramesFromFolder(folder, movePrefix + "-");
        if (attackSprites == null || attackSprites.Length == 0) attackSprites = LoadFramesFromFolder(folder, attackPrefix);
        if (hurtSprites == null || hurtSprites.Length == 0) hurtSprites = LoadFramesFromFolder(folder, "hit-");
        if (deathFrames == null || deathFrames.Length == 0) deathFrames = LoadFramesFromFolder(folder, "dead-");
#endif
    }

    /// <summary>Carrega quadros soltos de uma pasta: prefixo + número (ex.: "attack-A1.png" ... "attack-A8.png"), em ordem numérica.</summary>
    public static Sprite[] LoadFramesFromFolder(string folder, string prefix)
    {
#if UNITY_EDITOR
        var found = new List<KeyValuePair<int, Sprite>>();
        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folder }))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            string file = System.IO.Path.GetFileNameWithoutExtension(path);
            if (!file.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!int.TryParse(file.Substring(prefix.Length), out int n)) continue;
            var s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null && !found.Exists(f => f.Key == n)) found.Add(new KeyValuePair<int, Sprite>(n, s));
        }
        if (found.Count == 0) return null;
        found.Sort((a, b) => a.Key.CompareTo(b.Key));
        var result = new Sprite[found.Count];
        for (int i = 0; i < found.Count; i++) result[i] = found[i].Value;
        return result;
#else
        return null;
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
