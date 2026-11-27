using System.Collections;
using UnityEngine;

/// <summary>
/// Chefe final — Senhor do Abismo.
///  - Gigante (arte do Bringer of Death ampliada), muita vida.
///  - Escudo elemental que alterna Fogo/Gelo de tempos em tempos (só o elemento OPOSTO causa dano).
///  - DEFESA: enquanto erguida, só é desgastada pelo TIPO de golpe pedido (Ágil = ataque leve, Pesado ou Especial),
///    e sempre com o elemento certo. Quebrada → ele fica atordoado e vulnerável por alguns segundos (dano extra).
///    Depois a defesa volta pedindo o próximo tipo (Ágil → Pesado → Especial → ...).
///  - Ataques: foice (perto), colunas sombrias que caem do céu (longe) e rajada de orbes (longe).
///  - Sela a porta da sala enquanto a luta acontece.
/// </summary>
public class BossAbismo : BossBase
{
    [Header("--- Animações (quadros do Bringer of Death) ---")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] castFrames;
    [SerializeField] private Sprite[] spellFrames;
    [SerializeField] private Sprite[] hurtFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] orbFrames;
    [SerializeField] private float animFps = 10f;

    [Header("--- Movimento ---")]
    [SerializeField] private float walkSpeed = 1.6f;
    [SerializeField] private float enragedWalkSpeed = 2.3f;

    [Header("--- Foice (perto) ---")]
    [SerializeField] private int scytheDamage = 26;
    [SerializeField] private float scytheTrigger = 3.6f;
    [SerializeField] private float scytheReach = 5f;
    [SerializeField] private float scytheHeight = 4.5f;
    [SerializeField] private float scytheHitPoint = 0.55f;
    [SerializeField] private float scytheCooldown = 2.2f;

    [Header("--- Colunas sombrias (longe) ---")]
    [SerializeField] private int spellDamage = 22;
    [SerializeField] private float spellCooldown = 6f;
    [SerializeField] private float spellWarning = 0.8f;
    [SerializeField] private float spellRadius = 1.2f;
    [SerializeField] private float spellScale = 6f;

    [Header("--- Orbes (longe) ---")]
    [SerializeField] private int orbDamage = 16;
    [SerializeField] private float orbSpeed = 6.5f;
    [SerializeField] private float orbCooldown = 4.5f;
    [SerializeField] private float orbScale = 2.4f;

    [Header("--- Escudo e Defesa ---")]
    [SerializeField] private float shieldSwapSeconds = 10f;
    [SerializeField] private float enragedShieldSwapSeconds = 7f;
    [Tooltip("Golpes necessários para quebrar a defesa: Ágil (leve), Pesado, Especial.")]
    [SerializeField] private int lightHitsToBreak = 6;
    [SerializeField] private int heavyHitsToBreak = 3;
    [SerializeField] private int specialHitsToBreak = 4;
    [SerializeField] private float stunDuration = 6f;
    [SerializeField] private float stunnedDamageMultiplier = 1.5f;

    [Header("--- Sala ---")]
    [SerializeField] private CastleDoor sealDoor;

    private enum State { Idle, Walk, Scythe, Cast, Orbs, Stunned, Hurt }
    private State state = State.Idle;
    private float stateTimer;
    private bool released;
    private float nextScythe, nextSpell, nextOrbs, nextShieldSwap;

    private PlayerBehavior.HitKind guardKind = PlayerBehavior.HitKind.Light;
    private int guardLeft;
    private bool guardUp = true;
    private float nextGuardToast;

    private int GuardMax(PlayerBehavior.HitKind k) => k == PlayerBehavior.HitKind.Light ? lightHitsToBreak : k == PlayerBehavior.HitKind.Heavy ? heavyHitsToBreak : specialHitsToBreak;
    private static string KindName(PlayerBehavior.HitKind k) => k == PlayerBehavior.HitKind.Light ? "ÁGEIS (ataque leve)" : k == PlayerBehavior.HitKind.Heavy ? "PESADOS" : "ESPECIAIS (F/E)";

    protected override Sprite[] DeathClip => deathFrames;
    protected override void OnIdleVisual() => PlayClip(idleFrames, animFps, true);
    protected override string AwakenMessage =>
        $"<color=#FF5050>{bossName}</color> despertou!\nEle alterna escudo de FOGO/GELO e ergue uma DEFESA que só cede ao tipo de golpe pedido.";

    // ------------------------------------------------------------------ ciclo
    protected override void OnAwaken()
    {
        nextShieldSwap = Time.time + shieldSwapSeconds;
        nextSpell = Time.time + 3f;
        nextOrbs = Time.time + 5f;
        nextScythe = Time.time + 1f;
        RaiseGuard(PlayerBehavior.HitKind.Light);
        if (sealDoor != null) sealDoor.SetSealed(true, $"A porta foi selada por {bossName}!");
    }

    protected override void OnResetBoss()
    {
        state = State.Idle;
        guardUp = true;
        guardKind = PlayerBehavior.HitKind.Light;
        guardLeft = GuardMax(guardKind);
        if (sealDoor != null) sealDoor.SetSealed(false);
    }

    protected override void OnDeathStarted()
    {
        if (sealDoor != null) sealDoor.SetSealed(false);
    }

    protected override void OnPlayerDeadIdle()
    {
        base.OnPlayerDeadIdle();
        state = State.Idle;
    }

    // ------------------------------------------------------------------ defesa por tipo de golpe
    private void RaiseGuard(PlayerBehavior.HitKind kind)
    {
        guardUp = true;
        guardKind = kind;
        guardLeft = GuardMax(kind);
        ProgressHud.Toast($"A defesa do {bossName} se ergueu!\nQuebre-a com golpes <color=#FFD54A>{KindName(kind)}</color> do elemento certo.", 3.5f);
        UpdateBossBar();
    }

    protected override bool InterceptHit(int damage, ElementPolarity attackPolarity)
    {
        if (!guardUp) return false;
        bool rightElement = !useElementalShield || attackPolarity == RequiredElement;
        bool rightKind = PlayerBehavior.LastHitKind == guardKind;
        if (rightElement && rightKind)
        {
            guardLeft--;
            Flash(new Color(1f, 0.85f, 0.3f));
            FloatingText.Spawn(BodyCenter + Vector3.up * 2f, guardLeft > 0 ? $"Defesa {guardLeft}" : "QUEBROU!", new Color(1f, 0.85f, 0.3f));
            if (guardLeft <= 0) BreakGuard();
            else UpdateBossBar();
        }
        else
        {
            Flash(new Color(0.55f, 0.55f, 0.6f));
            if (Time.time >= nextGuardToast)
            {
                nextGuardToast = Time.time + 3f;
                string why = !rightKind ? $"Use golpes <color=#FFD54A>{KindName(guardKind)}</color>!"
                                        : $"Elemento errado: ataque com {(RequiredElement == ElementPolarity.Ice ? "<color=#7FD4FF>GELO</color>" : "<color=#FF8A50>FOGO</color>")}!";
                ProgressHud.Toast($"<color=#BBBBBB>Defesa!</color> {why}", 2.2f);
            }
        }
        return true;
    }

    private void BreakGuard()
    {
        guardUp = false;
        state = State.Stunned;
        stateTimer = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        PlayClip(hurtFrames, animFps * 0.6f, true, true);
        FireVFX.SpawnFlameBurst(BodyCenter, 30);
        IceVFX.SpawnFrostBurst(BodyCenter, 30);
        ProgressHud.Toast($"<color=#FFD54A>DEFESA QUEBRADA!</color>\n{bossName} está atordoado — ataque agora (dano extra)!", 3f);
        UpdateBossBar();
    }

    protected override int ModifyIncomingDamage(int damage) =>
        state == State.Stunned ? Mathf.RoundToInt(damage * stunnedDamageMultiplier) : damage;

    protected override void OnDamaged(int amount)
    {
        if (state == State.Stunned) return;
        // Sem defesa (janela curta depois do atordoamento) ainda reage, mas sem ser interrompido nos ataques.
    }

    protected override string ShieldSubtitle()
    {
        string elem = base.ShieldSubtitle();
        string guard = guardUp
            ? $"Defesa: golpes <color=#FFD54A>{KindName(guardKind)}</color> ({GuardMax(guardKind) - guardLeft}/{GuardMax(guardKind)})"
            : "<color=#FFD54A>DEFESA QUEBRADA — ataque!</color>";
        return elem + "\n" + guard;
    }

    // ------------------------------------------------------------------ IA
    protected override void TickAI(float dt, float dx)
    {
        stateTimer += dt;
        float dist = Mathf.Abs(dx);

        if (state != State.Stunned && Time.time >= nextShieldSwap)
        {
            SwapShield();
            nextShieldSwap = Time.time + (Enraged ? enragedShieldSwapSeconds : shieldSwapSeconds);
        }

        switch (state)
        {
            case State.Idle:
            case State.Walk:
                FaceTowards(dx);
                if (dist <= scytheTrigger && Time.time >= nextScythe) StartAttack(State.Scythe, attackFrames, 0.9f);
                else if (Time.time >= nextSpell && dist > 2.5f) StartAttack(State.Cast, castFrames, 1f);
                else if (Time.time >= nextOrbs && dist > 4f) StartAttack(State.Orbs, castFrames, 1.2f);
                else if (dist > scytheTrigger * 0.8f) { state = State.Walk; PlayClip(walkFrames, animFps, true); }
                else { state = State.Idle; OnIdleVisual(); }
                break;

            case State.Scythe:
                if (!released && ClipNormalized >= scytheHitPoint)
                {
                    released = true;
                    HitBoxInFront(scytheReach, scytheHeight, Enraged ? scytheDamage + 8 : scytheDamage, new Vector2(9f, 4f), out _);
                    var c = BodyCenter + new Vector3(facing * 2.5f, 0f, 0f);
                    if (shield == ElementPolarity.Fire) FireVFX.SpawnFlameBurst(c, 14); else IceVFX.SpawnFrostBurst(c, 14);
                }
                if (ClipDone) { nextScythe = Time.time + scytheCooldown * (Enraged ? 0.75f : 1f); BackToIdle(); }
                break;

            case State.Cast:
                if (!released && ClipNormalized >= 0.6f) { released = true; StartCoroutine(SpellRoutine()); }
                if (ClipDone) { nextSpell = Time.time + spellCooldown * (Enraged ? 0.7f : 1f); BackToIdle(); }
                break;

            case State.Orbs:
                if (!released && ClipNormalized >= 0.6f) { released = true; ReleaseOrbs(); }
                if (ClipDone) { nextOrbs = Time.time + orbCooldown * (Enraged ? 0.7f : 1f); BackToIdle(); }
                break;

            case State.Stunned:
                if (stateTimer >= stunDuration)
                {
                    var next = guardKind == PlayerBehavior.HitKind.Light ? PlayerBehavior.HitKind.Heavy
                             : guardKind == PlayerBehavior.HitKind.Heavy ? PlayerBehavior.HitKind.Special : PlayerBehavior.HitKind.Light;
                    RaiseGuard(next);
                    SwapShield();
                    nextShieldSwap = Time.time + (Enraged ? enragedShieldSwapSeconds : shieldSwapSeconds);
                    BackToIdle();
                }
                break;
        }
    }

    private void StartAttack(State s, Sprite[] frames, float speed)
    {
        state = s;
        stateTimer = 0f;
        released = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        PlayClip(frames, animFps * speed, false, true);
    }

    private void BackToIdle()
    {
        state = State.Idle;
        stateTimer = 0f;
        OnIdleVisual();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        float vx = 0f;
        if (state == State.Walk && player != null && !player.IsDead)
            vx = facing * (Enraged ? enragedWalkSpeed : walkSpeed);
        rb.linearVelocity = new Vector2(ClampVelocityToArena(vx), rb.linearVelocity.y);
    }

    // ------------------------------------------------------------------ ataques à distância
    private IEnumerator SpellRoutine()
    {
        if (player == null) yield break;
        int count = Enraged ? 3 : 1;
        float px = player.transform.position.x;
        float groundY = transform.position.y;
        for (int i = 0; i < count; i++)
        {
            float x = Mathf.Clamp(px + (i - (count - 1) * 0.5f) * 3.2f, arenaMinX, arenaMaxX);
            StartCoroutine(OneColumn(x, groundY));
            yield return new WaitForSeconds(0.25f);
        }
    }

    private IEnumerator OneColumn(float x, float groundY)
    {
        // Aviso no chão.
        for (float t = 0f; t < spellWarning; t += 0.1f)
        {
            var p = new Vector3(x + Random.Range(-0.4f, 0.4f), groundY + 0.15f, 0f);
            if (shield == ElementPolarity.Fire) FireVFX.SpawnFlameBurst(p, 2); else IceVFX.SpawnFrostBurst(p, 2);
            yield return new WaitForSeconds(0.1f);
        }
        // Coluna (animação "Spell" do Bringer).
        var go = new GameObject("Coluna_Abismo", typeof(SpriteRenderer));
        go.transform.position = new Vector3(x, groundY, 0f);
        go.transform.localScale = new Vector3(spellScale, spellScale, 1f);
        var s = go.GetComponent<SpriteRenderer>();
        s.sortingOrder = (sr != null ? sr.sortingOrder : 40) + 2;
        s.color = shield == ElementPolarity.Fire ? new Color(1f, 0.7f, 0.6f) : new Color(0.7f, 0.85f, 1f);
        float len = spellFrames != null && spellFrames.Length > 0 ? spellFrames.Length / 14f : 0.8f;
        bool hitDone = false;
        for (float t = 0f; t < len; t += Time.deltaTime)
        {
            if (spellFrames != null && spellFrames.Length > 0)
            {
                s.sprite = spellFrames[Mathf.Min(spellFrames.Length - 1, Mathf.FloorToInt(t * 14f))];
                // Alinha a base do quadro ao chão.
                var b = s.sprite.bounds;
                go.transform.position = new Vector3(x - b.center.x * spellScale, groundY - b.min.y * spellScale, 0f);
            }
            if (!hitDone && t >= len * 0.45f)
            {
                hitDone = true;
                if (player != null && !player.IsDead && Mathf.Abs(player.transform.position.x - x) <= spellRadius
                    && player.transform.position.y - groundY < 6f)
                    player.TakeDamage(Enraged ? spellDamage + 6 : spellDamage, new Vector2(Mathf.Sign(player.transform.position.x - x) * 5f, 5f));
            }
            yield return null;
        }
        Destroy(go);
    }

    private void ReleaseOrbs()
    {
        if (player == null) return;
        var elem = shield == ElementPolarity.Fire ? EnemyPolarityType.Fire : EnemyPolarityType.Ice;
        Vector3 origin = BodyCenter + new Vector3(facing * 1.5f, 1f, 0f);
        Vector2 baseDir = ((Vector2)(player.transform.position + Vector3.up * 0.6f - origin)).normalized;
        int count = Enraged ? 5 : 3;
        float spread = Enraged ? 40f : 24f;
        for (int i = 0; i < count; i++)
        {
            float a = (i - (count - 1) * 0.5f) * (spread / Mathf.Max(1, count - 1));
            Vector2 dir = Quaternion.Euler(0f, 0f, a) * baseDir;
            EnemyProjectile.Spawn(origin, dir * orbSpeed, orbDamage, elem, orbFrames, orbScale, 5f, 0.6f, (sr != null ? sr.sortingOrder : 40) + 1);
        }
    }
}
