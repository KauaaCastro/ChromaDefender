using System.Collections;
using UnityEngine;

/// <summary>
/// Chefe da Fase 2 (Castelo): "Mestre de Armas" (asset Martial Hero).
///
/// Mecânicas:
///  - Postura elemental: troca entre FOGO e GELO de tempos em tempos. Só toma dano do elemento OPOSTO.
///  - Combo de espada (2 golpes) quando o cavaleiro está perto.
///  - Investida: pisca, avança em alta velocidade e atravessa a arena. Role (Shift/K) ou pule para escapar.
///  - Furioso (abaixo de 50%): salto com impacto que solta ONDAS DE CHOQUE pelo chão — pule por cima.
///  - CONTRA-ATAQUE: bloqueie (Q) no instante do golpe de espada ou da investida → ele fica atordoado
///    e toma dano extra por alguns segundos.
///  - Ao morrer, solta a Chave de Prata do Poço (abre a Fase 3 na floresta).
/// </summary>
public class BossDuelist : BossBase
{
    private enum State { Idle, Chase, Attack1, Attack2, DashWindup, Dash, Recover, Jump, Stunned }

    [Header("--- Animações (listas de sprites) ---")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] runFrames;
    [SerializeField] private Sprite[] attack1Frames;
    [SerializeField] private Sprite[] attack2Frames;
    [SerializeField] private Sprite[] jumpFrames;
    [SerializeField] private Sprite[] fallFrames;
    [SerializeField] private Sprite[] hurtFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float animFps = 10f;
    [SerializeField] private float attackFps = 11f;

    [Header("--- Movimento ---")]
    [SerializeField] private float moveSpeed = 2.4f;
    [SerializeField] private float enragedMoveSpeed = 3.3f;

    [Header("--- Combo de Espada ---")]
    [SerializeField] private int slashDamage = 16;
    [SerializeField] private int finisherDamage = 22;
    [Tooltip("Distância do centro do chefe em que ele decide atacar.")]
    [SerializeField] private float comboTriggerDistance = 2.6f;
    [Tooltip("Alcance do golpe a partir do centro do chefe.")]
    [SerializeField] private float slashReach = 3.4f;
    [SerializeField] private float slashHeight = 2.8f;
    [Range(0.1f, 0.95f)]
    [SerializeField] private float slashHitPoint = 0.55f;
    [Tooltip("Chance de emendar o segundo golpe (furioso sempre emenda).")]
    [SerializeField, Range(0f, 1f)] private float secondHitChance = 0.5f;
    [SerializeField] private float comboRest = 1.7f;
    [SerializeField] private float enragedComboRest = 1.0f;
    [SerializeField] private Vector2 slashKnockback = new Vector2(7f, 3f);

    [Header("--- Investida ---")]
    [SerializeField] private int dashDamage = 24;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private float dashWindup = 0.7f;
    [SerializeField] private float dashMaxTime = 0.6f;
    [Tooltip("Quanto ele passa do jogador antes de parar.")]
    [SerializeField] private float dashOvershoot = 2.5f;
    [SerializeField] private float dashCooldown = 6f;
    [SerializeField] private float dashMinDistance = 4.5f;
    [SerializeField] private float dashRecover = 1.0f;

    [Header("--- Salto com Ondas de Choque (furioso) ---")]
    [SerializeField] private float jumpCooldown = 9f;
    [SerializeField] private float jumpVelocity = 14f;
    [SerializeField] private int shockwaveDamage = 18;
    [SerializeField] private float shockwaveSpeed = 9f;
    [SerializeField] private float shockwaveRange = 10f;

    [Header("--- Postura (troca de escudo) ---")]
    [SerializeField] private float stanceSwapSeconds = 10f;
    [SerializeField] private float enragedStanceSwapSeconds = 7f;

    [Header("--- Contra-ataque (Parry) ---")]
    [SerializeField] private float parryStunTime = 2.2f;
    [SerializeField] private float stunnedDamageMultiplier = 1.5f;

    private State state = State.Idle;
    private float stateTimer;
    private bool hitDone;
    private float nextComboTime;
    private float nextDashTime;
    private float nextJumpTime;
    private float nextStanceSwap;
    private int dashDir;
    private float dashTargetX;
    private float jumpVx;
    private bool shownJumpHint;
    private bool shownParryHint;
    private Sprite[] windupFrame;
    private Collider2D playerCollider;

    protected override Sprite[] DeathClip => deathFrames;
    protected override void OnIdleVisual() => PlayClip(idleFrames, animFps, true);

    protected override string AwakenMessage =>
        $"<color=#FF6060>{bossName}</color> desembainhou a espada!\n" +
        "Use o elemento OPOSTO à postura dele. Bloqueie (Q) no momento do golpe para atordoá-lo!";

    protected override void Awake()
    {
        base.Awake();
        if (attack1Frames != null && attack1Frames.Length > 0) windupFrame = new[] { attack1Frames[0] };
    }

    protected override void OnAwaken()
    {
        state = State.Idle;
        stateTimer = 0f;
        nextStanceSwap = Time.time + stanceSwapSeconds;
        nextComboTime = Time.time + 0.8f;
        nextDashTime = Time.time + 3f;
        nextJumpTime = Time.time + 2f;
    }

    protected override void OnResetBoss()
    {
        state = State.Idle;
        stateTimer = 0f;
    }

    protected override void OnPlayerDeadIdle()
    {
        if (state == State.Chase || state == State.Dash) { state = State.Idle; OnIdleVisual(); }
    }

    protected override bool ContactDamageActive => state != State.Stunned;

    protected override int ModifyIncomingDamage(int damage)
        => state == State.Stunned ? Mathf.RoundToInt(damage * stunnedDamageMultiplier) : damage;

    // ================================================================== IA

    protected override void TickAI(float dt, float dx)
    {
        stateTimer += dt;
        float dist = Mathf.Abs(dx);

        bool busy = state == State.Dash || state == State.DashWindup || state == State.Jump || state == State.Stunned;
        if (useElementalShield && !busy && Time.time >= nextStanceSwap)
        {
            SwapShield();
            nextStanceSwap = Time.time + (Enraged ? enragedStanceSwapSeconds : stanceSwapSeconds);
        }

        switch (state)
        {
            case State.Idle:
            case State.Chase:
                FaceTowards(dx);
                if (Enraged && Time.time >= nextJumpTime && dist > 2f) StartJump(dx);
                else if (Time.time >= nextDashTime && dist >= dashMinDistance) StartDashWindup(dx);
                else if (dist <= comboTriggerDistance && Time.time >= nextComboTime) StartAttack(State.Attack1, attack1Frames);
                else if (dist > comboTriggerDistance * 0.8f) { state = State.Chase; PlayClip(runFrames, animFps * 1.4f, true); }
                else { state = State.Idle; OnIdleVisual(); }
                break;

            case State.Attack1:
                if (!hitDone && ClipNormalized >= slashHitPoint) { hitDone = true; DoSlash(slashDamage); }
                if (state == State.Attack1 && ClipDone)
                {
                    bool chain = Enraged || Random.value < secondHitChance;
                    if (chain) { FaceTowards(dx); StartAttack(State.Attack2, attack2Frames); }
                    else EndCombo();
                }
                break;

            case State.Attack2:
                if (!hitDone && ClipNormalized >= slashHitPoint) { hitDone = true; DoSlash(finisherDamage); }
                if (state == State.Attack2 && ClipDone) EndCombo();
                break;

            case State.DashWindup:
                // Pisca na cor da postura para avisar a investida.
                if (sr != null) sr.color = (Mathf.FloorToInt(stateTimer * 12f) % 2 == 0) ? Color.white : BaseTint();
                if (stateTimer >= dashWindup) StartDash();
                break;

            case State.Dash:
                DashHitCheck();
                bool passed = dashDir > 0 ? transform.position.x >= dashTargetX : transform.position.x <= dashTargetX;
                if (state == State.Dash && (passed || stateTimer >= dashMaxTime)) EnterRecover();
                break;

            case State.Recover:
                if (stateTimer >= dashRecover) BackToIdle();
                break;

            case State.Jump:
                if (rb.linearVelocity.y > 0.1f) PlayClip(jumpFrames, animFps, true);
                else PlayClip(fallFrames, animFps, true);
                if (stateTimer > 0.25f && rb.linearVelocity.y <= 0.05f && IsGrounded()) Land();
                break;

            case State.Stunned:
                if (stateTimer >= parryStunTime) BackToIdle();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        float vx = 0f;
        if (isAwake && player != null && !player.IsDead)
        {
            switch (state)
            {
                case State.Chase: vx = facing * (Enraged ? enragedMoveSpeed : moveSpeed); break;
                case State.Dash: vx = dashDir * dashSpeed; break;
                case State.Jump: vx = jumpVx; break;
            }
        }

        float clamped = ClampVelocityToArena(vx);
        if (state == State.Dash && vx != 0f && clamped == 0f) EnterRecover(); // bateu no limite da arena
        rb.linearVelocity = new Vector2(clamped, rb.linearVelocity.y);
    }

    // ================================================================== combo

    private void StartAttack(State s, Sprite[] frames)
    {
        state = s;
        stateTimer = 0f;
        hitDone = false;
        PlayClip(frames, attackFps, false, true);
    }

    private void EndCombo()
    {
        nextComboTime = Time.time + (Enraged ? enragedComboRest : comboRest);
        BackToIdle();
    }

    private void DoSlash(int damage)
    {
        Vector3 tip = transform.position + new Vector3(facing * slashReach * 0.6f, slashHeight * 0.5f, 0f);
        if (shield == ElementPolarity.Fire) FireVFX.SpawnSlashArc(tip, facing, 0f, 2.2f);
        else IceVFX.SpawnSlashArc(tip, facing, 0f, 2.2f);

        if (HitBoxInFront(slashReach, slashHeight, damage, slashKnockback, out bool parried) && parried)
            Stun();
    }

    // ================================================================== investida

    private void StartDashWindup(float dx)
    {
        FaceTowards(dx);
        state = State.DashWindup;
        stateTimer = 0f;
        dashDir = facing;
        hitDone = false;
        if (windupFrame != null) PlayClip(windupFrame, 1f, true, true);
        nextDashTime = Time.time + dashCooldown * (Enraged ? 0.7f : 1f);
    }

    private void StartDash()
    {
        ApplyTint();
        state = State.Dash;
        stateTimer = 0f;
        float px = player != null ? player.transform.position.x : transform.position.x + dashDir * 6f;
        dashTargetX = Mathf.Clamp(px + dashDir * dashOvershoot, arenaMinX, arenaMaxX);
        PlayClip(attack1Frames, attackFps * 1.4f, false, true);
        if (shield == ElementPolarity.Fire) FireVFX.SpawnFlameBurst(BodyCenter, 18);
        else IceVFX.SpawnFrostBurst(BodyCenter, 18);
    }

    private void DashHitCheck()
    {
        if (hitDone || player == null || player.IsDead) return;
        Vector2 size = new Vector2(bodyWidth + 0.9f, bodyHeight);
        foreach (var hit in Physics2D.OverlapBoxAll(BodyCenter, size, 0f))
        {
            var p = hit.GetComponentInParent<PlayerBehavior>();
            if (p == null) continue;
            hitDone = true;
            if (p.IsParrying)
            {
                p.TakeDamage(dashDamage, new Vector2(dashDir * 8f, 3f)); // o parry anula o dano
                Stun();
            }
            else
            {
                p.TakeDamage(dashDamage, new Vector2(dashDir * 9f, 4f));
            }
            return;
        }
    }

    private void EnterRecover()
    {
        if (state == State.Stunned) return;
        state = State.Recover;
        stateTimer = 0f;
        OnIdleVisual();
    }

    // ================================================================== salto + ondas de choque

    private void StartJump(float dx)
    {
        state = State.Jump;
        stateTimer = 0f;
        nextJumpTime = Time.time + jumpCooldown;
        // Calcula a velocidade horizontal para cair perto do jogador.
        float airTime = 2f * jumpVelocity / Mathf.Max(0.1f, Physics2D.gravity.magnitude * rb.gravityScale);
        jumpVx = Mathf.Clamp(dx / Mathf.Max(0.3f, airTime), -8f, 8f);
        rb.linearVelocity = new Vector2(jumpVx, jumpVelocity);
        PlayClip(jumpFrames, animFps, true, true);

        if (!shownJumpHint)
        {
            shownJumpHint = true;
            ProgressHud.Toast("O Mestre de Armas está furioso! <color=#FFD54A>Pule</color> as ondas de choque!", 3f);
        }
    }

    private void Land()
    {
        jumpVx = 0f;
        Vector3 p = transform.position;
        if (shield == ElementPolarity.Fire) FireVFX.SpawnFlameBurst(p + Vector3.up * 0.3f, 30);
        else IceVFX.SpawnFrostBurst(p + Vector3.up * 0.3f, 30);
        StartCoroutine(Shockwave(p.x, p.y, 1));
        StartCoroutine(Shockwave(p.x, p.y, -1));
        state = State.Recover;
        stateTimer = 0f;
        PlayClip(attack2Frames, attackFps, false, true);
    }

    private IEnumerator Shockwave(float x, float groundY, int dir)
    {
        if (playerCollider == null && player != null) playerCollider = player.GetComponent<Collider2D>();
        float traveled = 0f;
        float vfxTimer = 0f;
        bool hit = false;
        bool fire = shield == ElementPolarity.Fire;

        while (traveled < shockwaveRange && !isDead)
        {
            float step = shockwaveSpeed * Time.deltaTime;
            x += dir * step;
            traveled += step;
            if (x < arenaMinX - 1f || x > arenaMaxX + 1f) yield break;

            vfxTimer -= Time.deltaTime;
            if (vfxTimer <= 0f)
            {
                vfxTimer = 0.06f;
                Vector3 pos = new Vector3(x, groundY + 0.2f, 0f);
                if (fire) FireVFX.SpawnFlameBurst(pos, 5);
                else IceVFX.SpawnFrostBurst(pos, 5);
            }

            if (!hit && player != null && !player.IsDead)
            {
                float feet = playerCollider != null ? playerCollider.bounds.min.y : player.transform.position.y;
                float px = player.transform.position.x;
                if (Mathf.Abs(px - x) < 0.6f && feet - groundY < 0.55f && feet - groundY > -1f)
                {
                    hit = true;
                    player.TakeDamage(shockwaveDamage, new Vector2(dir * 6f, 6f));
                }
            }
            yield return null;
        }
    }

    // ================================================================== atordoado

    private void Stun()
    {
        state = State.Stunned;
        stateTimer = 0f;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        PlayClip(hurtFrames, 8f, true, true);
        IceVFX.SpawnFrostBurst(BodyCenter + Vector3.up * 0.8f, 12);
        FireVFX.SpawnFlameBurst(BodyCenter + Vector3.up * 0.8f, 12);
        if (!shownParryHint)
        {
            shownParryHint = true;
            ProgressHud.Toast("<color=#FFD54A>Contra-ataque!</color> O Mestre de Armas está atordoado — ataque agora!", 3f);
        }
        else
        {
            FloatingText.Spawn(transform.position + Vector3.up * (bodyHeight + 0.4f), "Atordoado!", new Color(1f, 0.85f, 0.3f));
        }
    }

    private void BackToIdle()
    {
        ApplyTint();
        state = State.Idle;
        stateTimer = 0f;
        OnIdleVisual();
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.red;
        int f = Application.isPlaying ? facing : -1;
        Gizmos.DrawWireCube(transform.position + new Vector3(f * slashReach * 0.5f, slashHeight * 0.5f),
            new Vector3(slashReach, slashHeight, 0f));
    }
}
