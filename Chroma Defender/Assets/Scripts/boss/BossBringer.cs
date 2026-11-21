using System.Collections;
using UnityEngine;

/// <summary>
/// Chefe da Fase 1: "Ceifador Sombrio" (asset Bringer Of Death).
///
/// Mecânicas:
///  - Fica adormecido até o cavaleiro entrar na arena (área aberta, basta chegar).
///  - Escudo elemental que alterna entre FOGO e GELO de tempos em tempos.
///    Só toma dano do elemento OPOSTO ao escudo (mesma regra dos inimigos comuns).
///  - Golpe de foice corpo a corpo e magia que cai do céu sobre o jogador.
///  - Ao morrer, solta a Chave Dourada do Castelo (Fase 2) e não volta mais.
///
/// Estrutura esperada (criada pelo builder):
///  Boss (Rigidbody2D + BoxCollider2D + este script)
///   └─ Visual (SpriteRenderer + Animator com "Bringer Of Death.controller")
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class BossBringer : MonoBehaviour, IDamageableTarget
{
    private enum State { Dormant, Idle, Chase, Melee, Cast, Dead }

    [Header("--- Identidade ---")]
    [SerializeField] private string bossName = "Ceifador Sombrio";
    [SerializeField] private string defeatedFlag = GameProgress.FlagBossFase1;
    [SerializeField] private string dropKeyId = GameProgress.KeyCastelo;
    [SerializeField] private Sprite dropKeySprite;

    [Header("--- Vida ---")]
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private int currentHealth;

    [Header("--- Arena ---")]
    [Tooltip("Distância (em X) em que o chefe desperta ao ver o jogador.")]
    [SerializeField] private float activationRange = 10f;
    [SerializeField] private float arenaMinX = -9999f;
    [SerializeField] private float arenaMaxX = 9999f;

    [Header("--- Movimento ---")]
    [SerializeField] private float moveSpeed = 1.8f;
    [SerializeField] private float enragedMoveSpeed = 2.6f;

    [Header("--- Golpe de Foice ---")]
    [SerializeField] private int meleeDamage = 22;
    [Tooltip("Distância (centro a centro) em que o chefe decide dar o golpe de foice.")]
    [SerializeField] private float meleeTriggerDistance = 1.9f;
    [Tooltip("Alcance da foice para a frente, medido do centro do chefe. O golpe do cavaleiro alcança ~2.4, então mantenha abaixo disso para dar espaço de ataque.")]
    [SerializeField] private float meleeReach = 1.7f;
    [SerializeField] private float meleeHeight = 3.0f;
    [Tooltip("Velocidade da animação do golpe (menor = mais lento e fácil de esquivar).")]
    [SerializeField] private float meleeAnimSpeed = 0.65f;
    [Tooltip("Momento do dano dentro da animação do golpe (0 = início, 1 = fim). O golpe toca UMA vez e o dano acontece neste ponto, seja qual for a velocidade.")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float meleeHitPoint = 0.6f;
    [Tooltip("Segundos de descanso depois de cada golpe de foice antes de poder golpear de novo.")]
    [SerializeField] private float meleeRestTime = 4.5f;
    [Tooltip("Furioso (vida abaixo de 50%): multiplica o descanso. 1 = igual, 0.8 = 20% mais rápido.")]
    [SerializeField] private float enragedRestMultiplier = 0.8f;
    [SerializeField] private Vector2 meleeKnockback = new Vector2(7f, 3f);

    [Header("--- Magia (cai do céu) ---")]
    [SerializeField] private int spellDamage = 18;
    [SerializeField] private float spellCooldown = 3f;
    [Tooltip("Chance de usar a magia mesmo quando o jogador está perto (em vez da foice).")]
    [SerializeField, Range(0f, 1f)] private float spellChanceWhenClose = 0.45f;
    [SerializeField] private float castDuration = 0.9f;
    [SerializeField] private float spellHitDelay = 0.6f;
    [SerializeField] private float spellRadius = 1.1f;

    [Header("--- Escudo Elemental ---")]
    [SerializeField] private bool useElementalShield = true;
    [Tooltip("Segundos entre cada troca de escudo (Fogo <-> Gelo).")]
    [SerializeField] private float shieldSwapSeconds = 9f;
    [Tooltip("Furioso: multiplica o tempo entre trocas. 1 = igual, 0.8 = 20% mais rápido.")]
    [SerializeField] private float enragedShieldMultiplier = 0.8f;
    [Tooltip("Golpe com o elemento ERRADO cura o chefe nesta proporção do dano.")]
    [SerializeField] private float wrongElementHealMultiplier = 1f;
    [SerializeField] private ElementPolarity startingShield = ElementPolarity.Fire;

    [Header("--- Contato ---")]
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float contactCooldown = 1f;

    [Header("--- Visual ---")]
    [SerializeField] private Transform visual;
    [SerializeField] private float visualScale = 4.2f;
    [Tooltip("O sprite original do Bringer olha para a ESQUERDA.")]
    [SerializeField] private bool spriteFacesLeft = true;
    [SerializeField] private Vector2 visualOffset = new Vector2(0f, 1.9f);
    [Tooltip("Multiplica o tamanho do chefe (altura e largura, sem distorcer a pixel art).")]
    [SerializeField] private float sizeMultiplier = 1.3f;

    [Tooltip("Quantos pixels o corpo do Ceifador está deslocado do centro do quadro do sprite (olhando para a esquerda). Alinha a arte com o colisor.")]
    [SerializeField] private float spriteBodyOffsetPixels = 35f;
    [SerializeField] private float spritePixelsPerUnit = 100f;

    [Header("--- Área de Toque (corpo) ---")]
    [Tooltip("Largura do colisor do corpo. Menor = mais fácil chegar perto sem levar dano de contato.")]
    [SerializeField] private float bodyWidth = 0.9f;
    [Tooltip("Altura do colisor do corpo (acompanha o tamanho do chefe).")]
    [SerializeField] private float bodyHeight = 4.3f;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Animator anim;
    private SpriteRenderer sr;
    private PlayerBehavior player;

    private State state = State.Dormant;
    private int facing = -1;
    private float stateTimer;
    private float nextMeleeTime;
    private float nextSpellTime;
    private float nextShieldSwap;
    private float nextContactTime;
    private bool meleeHitDone;
    private float currentMeleeLength = 1f;
    private float attackClipLength = -1f;
    private ElementPolarity shield;
    private string currentAnim;
    private Coroutine flashRoutine;
    private Vector3 spawnPosition;
    private float nextHealToastTime;

    public bool IsDead => state == State.Dead;
    private bool Enraged => currentHealth <= maxHealth * 0.5f;
    private ElementPolarity RequiredElement => shield == ElementPolarity.Fire ? ElementPolarity.Ice : ElementPolarity.Fire;

    public void Configure(Transform visualTransform, Sprite keySprite, float minX, float maxX)
    {
        visual = visualTransform;
        dropKeySprite = keySprite;
        arenaMinX = minX;
        arenaMaxX = maxX;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (visual != null)
        {
            anim = visual.GetComponent<Animator>();
            sr = visual.GetComponent<SpriteRenderer>();
        }
        rb.freezeRotation = true;
        col.size = new Vector2(bodyWidth, bodyHeight);
        col.offset = new Vector2(0f, bodyHeight * 0.5f);
        currentHealth = maxHealth;
        shield = startingShield;
    }

    private void Start()
    {
        if (GameProgress.HasFlag(defeatedFlag))
        {
            // Chefe já foi derrotado nesta sessão. Se a chave ficou para trás, devolve ela ao chão.
            if (!GameProgress.HasKey(dropKeyId)) SpawnKey();
            Destroy(gameObject);
            return;
        }

        spawnPosition = transform.position;
        player = FindFirstObjectByType<PlayerBehavior>();
        if (player != null) player.OnPlayerRespawn += ResetBoss;
        ApplyFacing();
        ApplyShieldTint();
        Play("Idle");
    }

    private void Update()
    {
        if (state == State.Dead) return;
        if (player == null) player = FindFirstObjectByType<PlayerBehavior>();
        if (player == null) return;

        float dx = player.transform.position.x - transform.position.x;
        float dist = Mathf.Abs(dx);

        if (state == State.Dormant)
        {
            bool sameLevel = Mathf.Abs(player.transform.position.y - transform.position.y) < 4f;
            if (dist < activationRange && sameLevel) Awaken();
            return;
        }

        if (useElementalShield && Time.time >= nextShieldSwap) SwapShield();

        UpdateBossBar();

        if (player.IsDead)
        {
            if (state == State.Chase) { state = State.Idle; Play("Idle"); }
            return;
        }

        stateTimer += Time.deltaTime;

        switch (state)
        {
            case State.Idle:
            case State.Chase:
                FaceTowards(dx);
                if (Time.time >= nextSpellTime && (dist > meleeTriggerDistance * 1.5f || Random.value < spellChanceWhenClose * Time.deltaTime * 4f))
                    StartCast();
                else if (dist <= meleeTriggerDistance && Time.time >= nextMeleeTime) StartMelee();
                else if (dist > meleeTriggerDistance * 0.85f) { state = State.Chase; Play("Walk"); }
                else { state = State.Idle; Play("Idle"); }
                break;

            case State.Melee:
                if (!meleeHitDone && stateTimer >= currentMeleeLength * meleeHitPoint)
                {
                    meleeHitDone = true;
                    DoMeleeHit();
                }
                if (stateTimer >= currentMeleeLength) BackToIdle();
                break;

            case State.Cast:
                if (stateTimer >= castDuration)
                {
                    SpawnSpell(player.transform.position);
                    if (Enraged) StartCoroutine(DelayedSpell(0.45f));
                    BackToIdle();
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (state == State.Chase)
        {
            float speed = Enraged ? enragedMoveSpeed : moveSpeed;
            float vx = facing * speed;
            float nextX = rb.position.x + vx * Time.fixedDeltaTime;
            if (nextX < arenaMinX || nextX > arenaMaxX) vx = 0f;
            rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
    }

    // ---------------------------------------------------------------- Estados

    private void Awaken()
    {
        state = State.Idle;
        nextShieldSwap = Time.time + shieldSwapSeconds;
        nextSpellTime = Time.time + 3f;
        nextMeleeTime = Time.time + 0.8f;
        ProgressHud.Toast($"<color=#FF6060>{bossName}</color> despertou!\nO escudo dele muda de elemento: ataque com o elemento OPOSTO (TAB ou C troca Fogo/Gelo).", 5f);
        UpdateBossBar();
    }

    private void BackToIdle()
    {
        if (anim != null) anim.speed = 1f;
        state = State.Idle;
        stateTimer = 0f;
        Play("Idle");
    }

    private void StartMelee()
    {
        state = State.Melee;
        stateTimer = 0f;
        meleeHitDone = false;
        float speed = Mathf.Max(0.1f, meleeAnimSpeed);
        // Duração = exatamente UMA passada da animação de ataque (o clipe é em loop, então não pode passar disso).
        currentMeleeLength = AttackClipLength() / speed;
        nextMeleeTime = Time.time + currentMeleeLength + meleeRestTime * (Enraged ? enragedRestMultiplier : 1f);
        Play("Attack", true);
        if (anim != null) anim.speed = speed;
    }

    private float AttackClipLength()
    {
        if (attackClipLength > 0f) return attackClipLength;
        attackClipLength = 1f;
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            foreach (var clip in anim.runtimeAnimatorController.animationClips)
            {
                if (clip != null && clip.name == "Attack") { attackClipLength = clip.length; break; }
            }
        }
        return attackClipLength;
    }

    private void StartCast()
    {
        state = State.Cast;
        stateTimer = 0f;
        nextSpellTime = Time.time + (Enraged ? spellCooldown * 0.6f : spellCooldown);
        Play("Cast", true);
    }

    private void DoMeleeHit()
    {
        // A foice sempre passa um pouco da borda do corpo (senão, com corpo largo, ela nunca alcançaria o cavaleiro).
        float reach = Mathf.Max(meleeReach, bodyWidth * 0.5f + 0.5f);
        Vector2 size = new Vector2(reach, meleeHeight);
        Vector2 center = (Vector2)transform.position + new Vector2(facing * reach * 0.5f, meleeHeight * 0.5f);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            var p = hit.GetComponentInParent<PlayerBehavior>();
            if (p == null) continue;
            p.TakeDamage(meleeDamage, new Vector2(facing * meleeKnockback.x, meleeKnockback.y));
            break;
        }
    }

    private IEnumerator DelayedSpell(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (state != State.Dead && player != null) SpawnSpell(player.transform.position);
    }

    private void SpawnSpell(Vector3 target)
    {
        // Usa a animação "Spell" do próprio asset (coluna sombria que cai do céu).
        var go = new GameObject("BossSpell");
        go.transform.position = new Vector3(target.x, transform.position.y + visualOffset.y * sizeMultiplier, 0f);
        go.transform.localScale = Vector3.one * visualScale * sizeMultiplier;
        var s = go.AddComponent<SpriteRenderer>();
        if (sr != null)
        {
            s.sortingLayerID = sr.sortingLayerID;
            s.sortingOrder = sr.sortingOrder + 1;
        }
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            var a = go.AddComponent<Animator>();
            a.runtimeAnimatorController = anim.runtimeAnimatorController;
            a.Play("Spell", 0, 0f);
        }
        StartCoroutine(SpellDamageRoutine(go.transform.position));
        Destroy(go, 1.2f);
    }

    private IEnumerator SpellDamageRoutine(Vector3 pos)
    {
        yield return new WaitForSeconds(spellHitDelay);
        if (player == null || player.IsDead) yield break;
        Vector3 p = player.transform.position;
        if (Mathf.Abs(p.x - pos.x) <= spellRadius && Mathf.Abs(p.y - pos.y) <= 3f)
        {
            player.TakeDamage(spellDamage, new Vector2(Mathf.Sign(p.x - pos.x) * 3f, 4f));
        }
    }

    private void SwapShield()
    {
        shield = shield == ElementPolarity.Fire ? ElementPolarity.Ice : ElementPolarity.Fire;
        nextShieldSwap = Time.time + shieldSwapSeconds * (Enraged ? enragedShieldMultiplier : 1f);
        Vector3 c = col.bounds.center;
        if (shield == ElementPolarity.Fire) FireVFX.SpawnFlameBurst(c, 26);
        else IceVFX.SpawnFrostBurst(c, 26);
        ApplyShieldTint();
    }

    // ---------------------------------------------------------------- Dano

    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (state == State.Dead) return;
        if (state == State.Dormant) Awaken();

        if (useElementalShield && attackPolarity != RequiredElement)
        {
            // Elemento errado: o golpe é absorvido pelo escudo e REGENERA o chefe.
            int heal = Mathf.RoundToInt(damage * wrongElementHealMultiplier);
            currentHealth = Mathf.Min(maxHealth, currentHealth + heal);
            Flash(new Color(0.4f, 1f, 0.4f));
            UpdateBossBar();
            if (Time.time >= nextHealToastTime)
            {
                nextHealToastTime = Time.time + 4f;
                ProgressHud.Toast("<color=#7CFF7C>Elemento errado!</color> O escudo absorveu o golpe e o Ceifador se regenerou.", 2.2f);
            }
            return;
        }

        currentHealth -= damage;
        Vector3 c = col.bounds.center;
        if (attackPolarity == ElementPolarity.Ice) IceVFX.SpawnFrostBurst(c, 12);
        else FireVFX.SpawnFlameBurst(c, 14);
        Flash(Color.white);
        UpdateBossBar();

        if (currentHealth <= 0) Die();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (state == State.Dead || state == State.Dormant || Time.time < nextContactTime) return;
        var p = collision.collider.GetComponentInParent<PlayerBehavior>();
        if (p == null || p.IsDead || p.IsInvulnerable) return;
        nextContactTime = Time.time + contactCooldown;
        float dir = Mathf.Sign(p.transform.position.x - transform.position.x);
        p.TakeDamage(contactDamage, new Vector2(dir * 7f, 2.5f));
    }

    /// <summary>Jogador morreu/renasceu: o chefe volta ao ponto inicial, adormecido e com vida cheia.</summary>
    private void ResetBoss()
    {
        if (state == State.Dead) return;
        StopAllCoroutines();
        flashRoutine = null;
        currentHealth = maxHealth;
        state = State.Dormant;
        stateTimer = 0f;
        shield = startingShield;
        rb.linearVelocity = Vector2.zero;
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        if (anim != null) anim.speed = 1f;
        ApplyShieldTint();
        currentAnim = null;
        Play("Idle");
        ProgressHud.HideBoss();
    }

    private void OnDestroy()
    {
        if (player != null) player.OnPlayerRespawn -= ResetBoss;
    }

    private void Die()
    {
        state = State.Dead;
        currentHealth = 0;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        if (anim != null) anim.speed = 1f;
        GameProgress.SetFlag(defeatedFlag);
        ProgressHud.HideBoss();
        if (sr != null) sr.color = Color.white;
        Play("Death", true);
        FireVFX.SpawnFlameBurst(transform.position + Vector3.up * 1.5f, 40);
        IceVFX.SpawnFrostBurst(transform.position + Vector3.up * 1.5f, 30);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        if (anim != null) anim.enabled = false; // congela no último quadro
        SpawnKey();
        ProgressHud.Toast($"{bossName} foi derrotado! Pegue a <color=#FFD54A>Chave Dourada</color> e siga para o Castelo.", 5f);

        if (sr != null)
        {
            float t = 0f;
            Color start = sr.color;
            while (t < 1f)
            {
                t += Time.deltaTime;
                sr.color = new Color(start.r, start.g, start.b, 1f - t);
                yield return null;
            }
        }
        Destroy(gameObject);
    }

    private void SpawnKey()
    {
        if (string.IsNullOrEmpty(dropKeyId)) return;
        var key = new GameObject("Chave_" + dropKeyId);
        key.transform.position = transform.position + Vector3.up * 1.2f;
        key.transform.localScale = Vector3.one * 1.3f;
        var ksr = key.AddComponent<SpriteRenderer>();
        ksr.sprite = dropKeySprite;
        ksr.sortingOrder = 60;
        var c = key.AddComponent<CircleCollider2D>();
        c.isTrigger = true;
        c.radius = 0.6f;
        key.AddComponent<ProgressKeyPickup>().Configure(dropKeyId);
    }

    // ---------------------------------------------------------------- Visual

    private void FaceTowards(float dx)
    {
        int newFacing = dx >= 0 ? 1 : -1;
        if (newFacing == facing) return;
        facing = newFacing;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        if (visual == null) return;
        bool flip = spriteFacesLeft ? facing > 0 : facing < 0;
        float s = visualScale * sizeMultiplier;
        visual.localScale = new Vector3(s * (flip ? -1f : 1f), s, 1f);
        // No sprite o corpo fica deslocado do centro do quadro (o resto é espaço da foice).
        // Empurra a arte para o lado oposto, assim o corpo visível fica exatamente sobre o colisor.
        float bodyShift = spriteBodyOffsetPixels / spritePixelsPerUnit * s * (flip ? -1f : 1f);
        visual.localPosition = new Vector3(visualOffset.x * sizeMultiplier * (flip ? -1f : 1f) - bodyShift, visualOffset.y * sizeMultiplier, 0f);
    }

    private void ApplyShieldTint()
    {
        if (sr == null) return;
        sr.color = BaseTint();
    }

    private Color BaseTint()
    {
        if (!useElementalShield) return Color.white;
        return shield == ElementPolarity.Fire ? new Color(1f, 0.62f, 0.5f) : new Color(0.6f, 0.82f, 1f);
    }

    private void Flash(Color c)
    {
        if (sr == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(c));
    }

    private IEnumerator FlashRoutine(Color c)
    {
        sr.color = c;
        yield return new WaitForSeconds(0.1f);
        if (state != State.Dead) sr.color = BaseTint();
        flashRoutine = null;
    }

    private void Play(string animName, bool restart = false)
    {
        if (anim == null || !anim.enabled) return;
        if (!restart && currentAnim == animName) return;
        currentAnim = animName;
        anim.Play(animName, 0, 0f);
    }

    private void UpdateBossBar()
    {
        if (state == State.Dormant || state == State.Dead) return;
        string sub = !useElementalShield ? "" :
            shield == ElementPolarity.Fire
                ? "Escudo de <color=#FF8A50>FOGO</color> → ataque com <color=#7FD4FF>GELO</color>"
                : "Escudo de <color=#7FD4FF>GELO</color> → ataque com <color=#FF8A50>FOGO</color>";
        Color bar = shield == ElementPolarity.Fire ? new Color(0.9f, 0.35f, 0.15f) : new Color(0.25f, 0.6f, 1f);
        ProgressHud.ShowBoss(bossName, (float)currentHealth / maxHealth, sub, bar);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        float reach = Mathf.Max(meleeReach, bodyWidth * 0.5f + 0.5f);
        Gizmos.DrawWireCube(transform.position + new Vector3(facing * reach * 0.5f, meleeHeight * 0.5f), new Vector3(reach, meleeHeight, 0f));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(arenaMinX, transform.position.y, 0), new Vector3(arenaMaxX, transform.position.y, 0));
    }
}
