using System.Collections;
using UnityEngine;

/// <summary>
/// Base dos chefes das Fases 2 e 3 (o Ceifador da Fase 1 continua no BossBringer).
///
/// Cuida de tudo que é comum:
///  - Desperta quando o cavaleiro entra na arena; barra de vida no topo da tela (ProgressHud).
///  - Escudo elemental: só toma dano do elemento OPOSTO ao escudo; o errado é absorvido (e pode curar o chefe).
///  - Animação por listas de sprites (os pacotes Martial Hero / Evil Wizard não têm Animator pronto).
///  - Dano de contato, piscar ao levar golpe, virar para o jogador sem desalinhar a arte do colisor.
///  - Jogador morreu → chefe volta ao início, adormecido e com vida cheia.
///  - Morte → salva a flag, solta a chave (opcional) e pode abrir a tela de vitória.
///
/// Estrutura (criada pelo menu Chroma Defender → Chefes):
///  Boss (Rigidbody2D + BoxCollider2D + script do chefe)   ← posição = pés do chefe
///   └─ Visual (SpriteRenderer)
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public abstract class BossBase : MonoBehaviour, IDamageableTarget
{
    [Header("--- Identidade ---")]
    [SerializeField] protected string bossName = "Chefe";
    [Tooltip("Flag salva em GameProgress quando o chefe morre (ele não volta mais nesta sessão).")]
    [SerializeField] protected string defeatedFlag = "";
    [Tooltip("Chave que cai quando ele morre (vazio = nenhuma).")]
    [SerializeField] protected string dropKeyId = "";
    [SerializeField] protected Sprite dropKeySprite;
    [Tooltip("Abre a tela de vitória quando o chefe morre (use no chefe final).")]
    [SerializeField] protected bool triggerVictoryOnDeath = false;
    [SerializeField] protected float victoryDelay = 3f;
    [SerializeField, TextArea] protected string defeatMessage = "";

    [Header("--- Vida ---")]
    [SerializeField] protected int maxHealth = 600;
    [SerializeField] protected int currentHealth;

    [Header("--- Arena ---")]
    [Tooltip("Distância (em X) em que o chefe desperta ao ver o jogador.")]
    [SerializeField] protected float activationRange = 10f;
    [SerializeField] protected float arenaMinX = -9999f;
    [SerializeField] protected float arenaMaxX = 9999f;

    [Header("--- Escudo Elemental ---")]
    [SerializeField] protected bool useElementalShield = true;
    [SerializeField] protected ElementPolarity startingShield = ElementPolarity.Fire;
    [Tooltip("Golpe com o elemento ERRADO cura o chefe nesta proporção do dano (0 = só bloqueia).")]
    [SerializeField] protected float wrongElementHealMultiplier = 0.5f;

    [Header("--- Contato ---")]
    [SerializeField] protected int contactDamage = 10;
    [SerializeField] protected float contactCooldown = 1f;

    [Header("--- Visual ---")]
    [SerializeField] protected Transform visual;
    [SerializeField] protected float visualScale = 5.5f;
    [Tooltip("Marque se a arte original olha para a DIREITA.")]
    [SerializeField] protected bool spriteFacesRight = true;
    [Tooltip("Pixels entre o centro do quadro do sprite e os pés do personagem (alinha os pés com o chão).")]
    [SerializeField] protected float feetOffsetPixels = 22f;
    [Tooltip("Pixels entre o centro do quadro e o centro do corpo, na arte original (positivo = corpo à direita).")]
    [SerializeField] protected float bodyOffsetPixels = 0f;
    [SerializeField] protected float spritePixelsPerUnit = 100f;

    [Header("--- Área de Toque (corpo) ---")]
    [SerializeField] protected float bodyWidth = 1.1f;
    [SerializeField] protected float bodyHeight = 2.7f;

    // ------------------------------------------------------------------ estado comum
    protected Rigidbody2D rb;
    protected BoxCollider2D col;
    protected SpriteRenderer sr;
    protected PlayerBehavior player;

    protected bool isAwake;
    protected bool isDead;
    protected int facing = -1;
    protected ElementPolarity shield;
    protected Vector3 spawnPosition;

    private float nextContactTime;
    private float nextHealToastTime;
    private Coroutine flashRoutine;
    private float baseAlpha = 1f;

    // ------------------------------------------------------------------ animação por sprites
    private Sprite[] clip;
    private float clipFps = 10f;
    private bool clipLoop;
    protected float clipTime;

    public bool IsDead => isDead;
    public bool IsAwake => isAwake;
    protected bool Enraged => currentHealth <= maxHealth * 0.5f;
    protected float Health01 => (float)currentHealth / Mathf.Max(1, maxHealth);
    protected ElementPolarity RequiredElement => shield == ElementPolarity.Fire ? ElementPolarity.Ice : ElementPolarity.Fire;
    protected EnemyPolarityType ShieldAsEnemyElement => shield == ElementPolarity.Fire ? EnemyPolarityType.Fire : EnemyPolarityType.Ice;

    public void ConfigureArena(float minX, float maxX)
    {
        arenaMinX = minX;
        arenaMaxX = maxX;
    }

    // ================================================================== ciclo de vida

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (visual != null) sr = visual.GetComponent<SpriteRenderer>();

        rb.freezeRotation = true;
        col.size = new Vector2(bodyWidth, bodyHeight);
        col.offset = new Vector2(0f, bodyHeight * 0.5f);
        currentHealth = maxHealth;
        shield = startingShield;
    }

    protected virtual void Start()
    {
        if (!string.IsNullOrEmpty(defeatedFlag) && GameProgress.HasFlag(defeatedFlag))
        {
            // Já derrotado nesta sessão. Se a chave ficou para trás, devolve ela ao chão.
            if (!string.IsNullOrEmpty(dropKeyId) && !GameProgress.HasKey(dropKeyId)) SpawnKey();
            Destroy(gameObject);
            return;
        }

        spawnPosition = transform.position;
        player = FindAnyObjectByType<PlayerBehavior>();
        if (player != null) player.OnPlayerRespawn += HandlePlayerRespawn;
        ApplyFacing();
        ApplyTint();
        OnIdleVisual();
    }

    protected virtual void OnDestroy()
    {
        if (player != null) player.OnPlayerRespawn -= HandlePlayerRespawn;
    }

    protected virtual void Update()
    {
        float dt = Time.deltaTime;
        TickClip(dt);
        if (isDead) return;

        if (player == null)
        {
            player = FindAnyObjectByType<PlayerBehavior>();
            if (player != null) player.OnPlayerRespawn += HandlePlayerRespawn;
        }
        if (player == null) return;

        float dx = player.transform.position.x - transform.position.x;

        if (!isAwake)
        {
            bool sameLevel = Mathf.Abs(player.transform.position.y - transform.position.y) < 5f;
            if (Mathf.Abs(dx) < activationRange && sameLevel) Awaken();
            return;
        }

        UpdateBossBar();

        if (player.IsDead)
        {
            OnPlayerDeadIdle();
            return;
        }

        TickAI(dt, dx);
    }

    // ================================================================== ganchos dos chefes

    /// <summary>Lógica de IA do chefe, chamada todo quadro enquanto acordado e o jogador vivo.</summary>
    protected abstract void TickAI(float dt, float dxToPlayer);

    /// <summary>Animação de parado (chamada ao iniciar e ao resetar).</summary>
    protected abstract void OnIdleVisual();

    /// <summary>Animação de morte (Sprite[]).</summary>
    protected abstract Sprite[] DeathClip { get; }

    protected virtual void OnAwaken() { }
    protected virtual void OnPlayerDeadIdle() { rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); OnIdleVisual(); }
    protected virtual void OnResetBoss() { }
    protected virtual void OnDamaged(int amount) { }
    protected virtual void OnDeathStarted() { }
    /// <summary>Retorne falso para ignorar golpes (ex.: durante o teletransporte).</summary>
    protected virtual bool CanTakeDamage() => true;
    /// <summary>Chamado antes de tudo em TakeDamage. Retorne true para consumir o golpe (ex.: defesa que exige um tipo de golpe).</summary>
    protected virtual bool InterceptHit(int damage, ElementPolarity attackPolarity) => false;
    /// <summary>Permite multiplicar o dano recebido (ex.: atordoado toma mais dano).</summary>
    protected virtual int ModifyIncomingDamage(int damage) => damage;
    protected virtual bool ContactDamageActive => true;
    protected virtual string AwakenMessage =>
        $"<color=#FF6060>{bossName}</color> despertou!\nAtaque com o elemento OPOSTO ao escudo dele (TAB ou C troca Fogo/Gelo).";

    // ================================================================== despertar / reset

    protected void Awaken()
    {
        if (isAwake || isDead) return;
        isAwake = true;
        ProgressHud.Toast(AwakenMessage, 5f);
        OnAwaken();
        UpdateBossBar();
    }

    private void HandlePlayerRespawn()
    {
        if (isDead) return;
        StopAllCoroutines();
        flashRoutine = null;
        currentHealth = maxHealth;
        isAwake = false;
        shield = startingShield;
        baseAlpha = 1f;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        rb.position = spawnPosition;
        transform.position = spawnPosition;
        col.enabled = true;
        OnResetBoss();
        ApplyTint();
        OnIdleVisual();
        ProgressHud.HideBoss();
    }

    // ================================================================== dano recebido

    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (isDead) return;
        if (!isAwake) Awaken();

        if (InterceptHit(damage, attackPolarity)) return;

        if (!CanTakeDamage())
        {
            Flash(new Color(0.6f, 0.6f, 0.6f));
            return;
        }

        if (useElementalShield && attackPolarity != RequiredElement)
        {
            int heal = Mathf.RoundToInt(damage * wrongElementHealMultiplier);
            if (heal > 0) currentHealth = Mathf.Min(maxHealth, currentHealth + heal);
            Flash(heal > 0 ? new Color(0.4f, 1f, 0.4f) : new Color(0.6f, 0.6f, 0.6f));
            UpdateBossBar();
            if (Time.time >= nextHealToastTime)
            {
                nextHealToastTime = Time.time + 4f;
                string what = heal > 0 ? "O escudo absorveu o golpe e o chefe se regenerou." : "O escudo bloqueou o golpe.";
                ProgressHud.Toast($"<color=#7CFF7C>Elemento errado!</color> {what}", 2.2f);
            }
            return;
        }

        damage = ModifyIncomingDamage(damage);
        currentHealth -= damage;
        Vector3 c = col.bounds.center;
        if (attackPolarity == ElementPolarity.Ice) IceVFX.SpawnFrostBurst(c, 12);
        else FireVFX.SpawnFlameBurst(c, 14);
        Flash(Color.white);
        OnDamaged(damage);
        UpdateBossBar();

        if (currentHealth <= 0) Die();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isDead || !isAwake || !ContactDamageActive || Time.time < nextContactTime) return;
        var p = collision.collider.GetComponentInParent<PlayerBehavior>();
        if (p == null || p.IsDead || p.IsInvulnerable) return;
        nextContactTime = Time.time + contactCooldown;
        float dir = Mathf.Sign(p.transform.position.x - transform.position.x);
        p.TakeDamage(contactDamage, new Vector2(dir * 7f, 2.5f));
    }

    // ================================================================== morte

    private void Die()
    {
        isDead = true;
        currentHealth = 0;
        StopAllCoroutines();
        flashRoutine = null;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        col.enabled = false;
        if (!string.IsNullOrEmpty(defeatedFlag)) GameProgress.SetFlag(defeatedFlag);
        ProgressHud.HideBoss();
        baseAlpha = 1f;
        if (sr != null) sr.color = Color.white;
        OnDeathStarted();
        PlayClip(DeathClip, 10f, false, true);
        FireVFX.SpawnFlameBurst(transform.position + Vector3.up * 1.5f, 40);
        IceVFX.SpawnFrostBurst(transform.position + Vector3.up * 1.5f, 30);
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float len = Mathf.Max(0.6f, ClipLength);
        yield return new WaitForSeconds(len);

        if (!string.IsNullOrEmpty(dropKeyId)) SpawnKey();
        string msg = string.IsNullOrEmpty(defeatMessage) ? $"{bossName} foi derrotado!" : defeatMessage;
        ProgressHud.Toast(msg, 5f);

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

        if (triggerVictoryOnDeath)
        {
            if (sr != null) sr.enabled = false;
            yield return new WaitForSeconds(Mathf.Max(0f, victoryDelay - 1f));
            VictoryController.Trigger();
        }
        Destroy(gameObject);
    }

    protected void SpawnKey()
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

    // ================================================================== escudo

    protected void SetShield(ElementPolarity newShield, bool vfx = true)
    {
        shield = newShield;
        if (vfx)
        {
            Vector3 c = col.bounds.center;
            if (shield == ElementPolarity.Fire) FireVFX.SpawnFlameBurst(c, 26);
            else IceVFX.SpawnFrostBurst(c, 26);
        }
        ApplyTint();
        UpdateBossBar();
    }

    protected void SwapShield(bool vfx = true)
    {
        SetShield(shield == ElementPolarity.Fire ? ElementPolarity.Ice : ElementPolarity.Fire, vfx);
    }

    // ================================================================== movimento / utilidades

    protected float ClampVelocityToArena(float vx)
    {
        float nextX = rb.position.x + vx * Time.fixedDeltaTime;
        if ((nextX < arenaMinX && vx < 0f) || (nextX > arenaMaxX && vx > 0f)) return 0f;
        return vx;
    }

    protected bool IsGrounded()
    {
        Vector2 origin = new Vector2(col.bounds.center.x, col.bounds.min.y + 0.05f);
        var hit = Physics2D.Raycast(origin, Vector2.down, 0.2f, LayerMask.GetMask("Ground", "Default"));
        return hit.collider != null && !hit.collider.isTrigger && hit.collider != col;
    }

    protected void FaceTowards(float dx)
    {
        int newFacing = dx >= 0 ? 1 : -1;
        if (newFacing == facing) return;
        facing = newFacing;
        ApplyFacing();
    }

    protected void ApplyFacing()
    {
        if (visual == null) return;
        bool flip = spriteFacesRight ? facing < 0 : facing > 0;
        if (sr != null) sr.flipX = flip;
        float s = visualScale;
        float body = bodyOffsetPixels / spritePixelsPerUnit * s;
        float feet = feetOffsetPixels / spritePixelsPerUnit * s;
        visual.localScale = new Vector3(s, s, 1f);
        // Empurra a arte para que o CORPO (não o centro do quadro) fique sobre o colisor.
        visual.localPosition = new Vector3(flip ? body : -body, feet, 0f);
    }

    protected Vector3 BodyCenter => col != null ? col.bounds.center : transform.position + Vector3.up;

    /// <summary>Acerta o jogador se ele estiver dentro da caixa à frente do chefe. Retorna true se acertou (ou foi bloqueado).</summary>
    protected bool HitBoxInFront(float reachFromCenter, float height, int damage, Vector2 knockback, out bool parried)
    {
        parried = false;
        if (player == null || player.IsDead) return false;
        Vector2 size = new Vector2(reachFromCenter, height);
        Vector2 center = (Vector2)transform.position + new Vector2(facing * reachFromCenter * 0.5f, height * 0.5f);
        foreach (var hit in Physics2D.OverlapBoxAll(center, size, 0f))
        {
            var p = hit.GetComponentInParent<PlayerBehavior>();
            if (p == null) continue;
            if (p.IsParrying) parried = true;
            p.TakeDamage(damage, new Vector2(facing * knockback.x, knockback.y));
            return true;
        }
        return false;
    }

    // ================================================================== visual

    protected void PlayClip(Sprite[] frames, float fps, bool loop, bool restart = false)
    {
        if (frames == null || frames.Length == 0 || sr == null) return;
        if (!restart && frames == clip) return;
        clip = frames;
        clipFps = Mathf.Max(1f, fps);
        clipLoop = loop;
        clipTime = 0f;
        sr.sprite = frames[0];
    }

    protected float ClipLength => clip == null ? 0f : clip.Length / clipFps;
    protected float ClipNormalized => ClipLength <= 0f ? 1f : Mathf.Clamp01(clipTime / ClipLength);
    protected bool ClipDone => clip == null || (!clipLoop && clipTime >= ClipLength);
    protected bool IsPlaying(Sprite[] frames) => clip == frames;

    private void TickClip(float dt)
    {
        if (clip == null || clip.Length == 0 || sr == null) return;
        clipTime += dt;
        int idx = Mathf.FloorToInt(clipTime * clipFps);
        idx = clipLoop ? idx % clip.Length : Mathf.Min(idx, clip.Length - 1);
        if (clip[idx] != null) sr.sprite = clip[idx];
    }

    protected void SetAlpha(float a)
    {
        baseAlpha = a;
        ApplyTint();
    }

    protected void ApplyTint()
    {
        if (sr == null) return;
        Color c = BaseTint();
        c.a = baseAlpha;
        sr.color = c;
    }

    protected virtual Color BaseTint()
    {
        if (!useElementalShield) return Color.white;
        return shield == ElementPolarity.Fire ? new Color(1f, 0.62f, 0.5f) : new Color(0.6f, 0.82f, 1f);
    }

    protected void Flash(Color c)
    {
        if (sr == null) return;
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine(c));
    }

    private IEnumerator FlashRoutine(Color c)
    {
        c.a = baseAlpha;
        sr.color = c;
        yield return new WaitForSeconds(0.1f);
        if (!isDead) ApplyTint();
        flashRoutine = null;
    }

    protected virtual string ShieldSubtitle()
    {
        if (!useElementalShield) return "";
        return shield == ElementPolarity.Fire
            ? "Escudo de <color=#FF8A50>FOGO</color> → ataque com <color=#7FD4FF>GELO</color>"
            : "Escudo de <color=#7FD4FF>GELO</color> → ataque com <color=#FF8A50>FOGO</color>";
    }

    protected void UpdateBossBar()
    {
        if (!isAwake || isDead) return;
        Color bar = !useElementalShield ? new Color(0.8f, 0.15f, 0.15f)
            : shield == ElementPolarity.Fire ? new Color(0.9f, 0.35f, 0.15f) : new Color(0.25f, 0.6f, 1f);
        ProgressHud.ShowBoss(bossName, Health01, ShieldSubtitle(), bar);
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(arenaMinX, transform.position.y, 0), new Vector3(arenaMaxX, transform.position.y, 0));
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * bodyHeight * 0.5f, new Vector3(bodyWidth, bodyHeight, 0f));
    }
}
