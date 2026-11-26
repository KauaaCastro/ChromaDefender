using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Chefe da Fase 3 (Área Mágica): "Arquimago do Caos" (asset Evil Wizard 3).
///
/// Mecânicas:
///  - TELETRANSPORTE: some e reaparece em outro ponto da arena, trocando o escudo (Fogo ↔ Gelo).
///    Enquanto está sumido, não toma dano.
///  - ORBES ELEMENTAIS da cor do escudo dele. Se o cavaleiro estiver com o MESMO elemento, ABSORVE o orbe
///    (sem dano). Mas para FERIR o mago precisa do elemento OPOSTO → troque de elemento na hora certa!
///  - PILARES: marca o chão sob o cavaleiro (e ao lado) e, depois de um aviso, uma explosão sobe dali. Saia de cima.
///  - Furioso (abaixo de 50%): leque de orbes alternando Fogo/Gelo, magias mais rápidas.
///  - Invoca lacaios ao chegar em 66% e 33% da vida.
///  - Chefe final da demo: ao morrer, abre a tela de vitória.
/// </summary>
public class BossArchmage : BossBase
{
    private enum State { Idle, Walk, CastOrbs, CastPillars, TeleportOut, TeleportIn, Summon, Hurt }

    [Header("--- Animações (listas de sprites) ---")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private Sprite[] attackFrames;
    [SerializeField] private Sprite[] hurtFrames;
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private Sprite[] projectileFrames;
    [SerializeField] private float animFps = 10f;
    [SerializeField] private float castFps = 14f;

    [Header("--- Movimento ---")]
    [SerializeField] private float walkSpeed = 1.4f;
    [Tooltip("Distância que ele tenta manter do cavaleiro.")]
    [SerializeField] private float preferredDistance = 6f;

    [Header("--- Orbes ---")]
    [SerializeField] private int orbDamage = 16;
    [SerializeField] private float orbSpeed = 6.5f;
    [SerializeField] private float orbHoming = 1.2f;
    [SerializeField] private float orbScale = 2.2f;
    [Range(0.1f, 0.95f)]
    [SerializeField] private float castReleasePoint = 0.6f;
    [SerializeField] private float orbCooldown = 2.4f;
    [SerializeField] private int enragedFanCount = 5;
    [SerializeField] private float enragedFanAngle = 50f;

    [Header("--- Pilares ---")]
    [SerializeField] private int pillarDamage = 20;
    [SerializeField] private float pillarWarning = 0.9f;
    [SerializeField] private float pillarRadius = 0.9f;
    [SerializeField] private float pillarSpacing = 2.6f;
    [SerializeField] private float pillarCooldown = 7f;

    [Header("--- Teletransporte ---")]
    [SerializeField] private float teleportEvery = 8f;
    [SerializeField] private float teleportFadeTime = 0.4f;
    [SerializeField] private float teleportMinDistanceFromPlayer = 4.5f;

    [Header("--- Lacaios ---")]
    [Tooltip("Prefab de inimigo comum (com EnemyBehavior) invocado em 66% e 33% da vida. Vazio = sem invocação.")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionsPerSummon = 2;

    private State state = State.Idle;
    private float stateTimer;
    private bool released;
    private float nextOrbTime;
    private float nextPillarTime;
    private float nextTeleportTime;
    private float teleportTargetX;
    private bool summoned66;
    private bool summoned33;
    private bool shownAbsorbHint;
    private readonly List<GameObject> minions = new List<GameObject>();

    protected override Sprite[] DeathClip => deathFrames;
    protected override void OnIdleVisual() => PlayClip(idleFrames, animFps, true);

    protected override string AwakenMessage =>
        $"<color=#C77DFF>{bossName}</color> abriu o portal do caos!\n" +
        "Fique com o MESMO elemento dos orbes para absorvê-los, e troque para o OPOSTO para feri-lo.";

    protected override void OnAwaken()
    {
        state = State.Idle;
        stateTimer = 0f;
        nextOrbTime = Time.time + 1.2f;
        nextPillarTime = Time.time + 5f;
        nextTeleportTime = Time.time + teleportEvery;
    }

    protected override void OnResetBoss()
    {
        state = State.Idle;
        stateTimer = 0f;
        summoned66 = false;
        summoned33 = false;
        SetAlpha(1f);
        foreach (var m in minions) if (m != null) Destroy(m);
        minions.Clear();
    }

    protected override void OnDeathStarted()
    {
        SetAlpha(1f);
        // Os lacaios somem junto com o mestre.
        foreach (var m in minions)
        {
            if (m == null) continue;
            FireVFX.SpawnFlameBurst(m.transform.position + Vector3.up * 0.5f, 14);
            Destroy(m);
        }
        minions.Clear();
    }

    protected override void OnPlayerDeadIdle()
    {
        if (state == State.Walk) { state = State.Idle; OnIdleVisual(); }
    }

    protected override bool CanTakeDamage() => state != State.TeleportOut && state != State.TeleportIn;
    protected override bool ContactDamageActive => CanTakeDamage();

    protected override void OnDamaged(int amount)
    {
        // Interrompe só quando está parado/andando (não cancela magias já começadas).
        if (state == State.Idle || state == State.Walk)
        {
            state = State.Hurt;
            stateTimer = 0f;
            PlayClip(hurtFrames, 12f, false, true);
        }

        if (minionPrefab != null)
        {
            if (!summoned66 && Health01 <= 0.66f) { summoned66 = true; QueueSummon(); }
            else if (!summoned33 && Health01 <= 0.33f) { summoned33 = true; QueueSummon(); }
        }
    }

    private bool pendingSummon;
    private void QueueSummon() => pendingSummon = true;

    // ================================================================== IA

    protected override void TickAI(float dt, float dx)
    {
        stateTimer += dt;
        float dist = Mathf.Abs(dx);

        switch (state)
        {
            case State.Idle:
            case State.Walk:
                FaceTowards(dx);
                if (pendingSummon) StartSummon();
                else if (Time.time >= nextTeleportTime || (dist < 1.8f && Random.value < dt * 1.5f)) StartTeleport();
                else if (Time.time >= nextPillarTime) StartCast(State.CastPillars);
                else if (Time.time >= nextOrbTime) StartCast(State.CastOrbs);
                else if (dist < preferredDistance * 0.6f || dist > preferredDistance * 1.6f) { state = State.Walk; PlayClip(walkFrames, animFps, true); }
                else { state = State.Idle; OnIdleVisual(); }
                break;

            case State.Hurt:
                if (ClipDone || stateTimer > 0.5f) BackToIdle();
                break;

            case State.CastOrbs:
                if (!released && ClipNormalized >= castReleasePoint) { released = true; ReleaseOrbs(); }
                if (ClipDone) BackToIdle();
                break;

            case State.CastPillars:
                if (!released && ClipNormalized >= castReleasePoint) { released = true; StartCoroutine(PillarsRoutine()); }
                if (ClipDone) BackToIdle();
                break;

            case State.Summon:
                if (!released && ClipNormalized >= castReleasePoint) { released = true; DoSummon(); }
                if (ClipDone) BackToIdle();
                break;

            case State.TeleportOut:
                SetAlpha(1f - Mathf.Clamp01(stateTimer / teleportFadeTime));
                if (stateTimer >= teleportFadeTime)
                {
                    rb.position = new Vector2(teleportTargetX, rb.position.y);
                    transform.position = new Vector3(teleportTargetX, transform.position.y, transform.position.z);
                    SwapShield(false);
                    FaceTowards(dx);
                    state = State.TeleportIn;
                    stateTimer = 0f;
                }
                break;

            case State.TeleportIn:
                SetAlpha(Mathf.Clamp01(stateTimer / teleportFadeTime));
                if (stateTimer >= teleportFadeTime)
                {
                    SetAlpha(1f);
                    SetShield(shield); // efeito visual da nova cor
                    nextOrbTime = Mathf.Min(nextOrbTime, Time.time + 0.4f);
                    BackToIdle();
                }
                break;
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        float vx = 0f;
        if (state == State.Walk && player != null && !player.IsDead)
        {
            float dx = player.transform.position.x - transform.position.x;
            float dist = Mathf.Abs(dx);
            // Muito perto: se afasta. Muito longe: se aproxima.
            float dir = dist < preferredDistance ? -Mathf.Sign(dx) : Mathf.Sign(dx);
            vx = dir * walkSpeed;
        }
        rb.linearVelocity = new Vector2(ClampVelocityToArena(vx), rb.linearVelocity.y);
    }

    private void BackToIdle()
    {
        state = State.Idle;
        stateTimer = 0f;
        OnIdleVisual();
    }

    // ================================================================== magias

    private void StartCast(State s)
    {
        state = s;
        stateTimer = 0f;
        released = false;
        PlayClip(attackFrames, castFps, false, true);
        if (s == State.CastOrbs) nextOrbTime = Time.time + orbCooldown * (Enraged ? 0.7f : 1f);
        else nextPillarTime = Time.time + pillarCooldown * (Enraged ? 0.75f : 1f);
    }

    private Vector3 HandPosition => transform.position + new Vector3(facing * (bodyWidth * 0.5f + 0.5f), bodyHeight * 0.6f, 0f);

    private void ReleaseOrbs()
    {
        if (player == null) return;
        Vector3 origin = HandPosition;
        Vector2 aim = ((Vector2)player.transform.position + Vector2.up * 0.8f - (Vector2)origin).normalized;

        if (!Enraged)
        {
            FireOrb(origin, aim, ShieldAsEnemyElement, orbHoming);
        }
        else
        {
            int n = Mathf.Max(1, enragedFanCount);
            for (int i = 0; i < n; i++)
            {
                float t = n == 1 ? 0.5f : (float)i / (n - 1);
                float ang = Mathf.Lerp(-enragedFanAngle * 0.5f, enragedFanAngle * 0.5f, t);
                Vector2 dir = Quaternion.Euler(0f, 0f, ang) * aim;
                var elem = (i % 2 == 0) ? EnemyPolarityType.Fire : EnemyPolarityType.Ice;
                FireOrb(origin, dir, elem, 0f);
            }
        }

        if (!shownAbsorbHint)
        {
            shownAbsorbHint = true;
            ProgressHud.Toast("Dica: com o <color=#FFD54A>mesmo elemento</color> do orbe você o absorve sem levar dano!", 3.5f);
        }
    }

    private void FireOrb(Vector3 origin, Vector2 dir, EnemyPolarityType elem, float homing)
    {
        int order = sr != null ? sr.sortingOrder + 2 : 50;
        EnemyProjectile.Spawn(origin, dir * orbSpeed, orbDamage, elem, projectileFrames, orbScale, 5f, homing, order);
    }

    private IEnumerator PillarsRoutine()
    {
        if (player == null) yield break;
        float baseX = player.transform.position.x;
        float y = transform.position.y;
        int count = Enraged ? 5 : 3;
        var xs = new List<float>();
        for (int i = 0; i < count; i++)
        {
            float offset = (i - (count - 1) * 0.5f) * pillarSpacing;
            xs.Add(Mathf.Clamp(baseX + offset, arenaMinX, arenaMaxX));
        }

        // Aviso: faíscas no chão onde o pilar vai subir.
        float warn = pillarWarning * (Enraged ? 0.8f : 1f);
        float t = 0f;
        while (t < warn)
        {
            foreach (float x in xs)
            {
                Vector3 p = new Vector3(x + Random.Range(-0.3f, 0.3f), y + 0.1f, 0f);
                if (shield == ElementPolarity.Fire) FireVFX.SpawnFlameBurst(p, 2);
                else IceVFX.SpawnFrostBurst(p, 2);
            }
            t += 0.12f;
            yield return new WaitForSeconds(0.12f);
        }

        // Explosão
        Collider2D pc = player != null ? player.GetComponent<Collider2D>() : null;
        bool hit = false;
        foreach (float x in xs)
        {
            for (int k = 0; k < 4; k++)
            {
                Vector3 p = new Vector3(x, y + 0.4f + k * 0.8f, 0f);
                FireVFX.SpawnFlameBurst(p, 8);
                IceVFX.SpawnFrostBurst(p, 6);
            }
            if (hit || player == null || player.IsDead) continue;
            float feet = pc != null ? pc.bounds.min.y : player.transform.position.y;
            if (Mathf.Abs(player.transform.position.x - x) <= pillarRadius && feet - y < 3.2f && feet - y > -1f)
            {
                hit = true;
                player.TakeDamage(pillarDamage, new Vector2(Mathf.Sign(player.transform.position.x - x) * 4f, 7f));
            }
        }
    }

    // ================================================================== teletransporte

    private void StartTeleport()
    {
        nextTeleportTime = Time.time + teleportEvery * (Enraged ? 0.7f : 1f);
        state = State.TeleportOut;
        stateTimer = 0f;
        OnIdleVisual();
        IceVFX.SpawnFrostBurst(BodyCenter, 14);
        FireVFX.SpawnFlameBurst(BodyCenter, 14);

        // Escolhe um ponto da arena longe do jogador.
        float px = player != null ? player.transform.position.x : transform.position.x;
        float best = transform.position.x;
        float bestScore = -1f;
        for (int i = 0; i < 8; i++)
        {
            float x = Random.Range(arenaMinX + 1f, arenaMaxX - 1f);
            float score = Mathf.Abs(x - px);
            if (score >= teleportMinDistanceFromPlayer && score < preferredDistance * 1.6f) score += 100f; // distância ideal
            if (score > bestScore) { bestScore = score; best = x; }
        }
        if (arenaMaxX - arenaMinX > 9000f) best = px + (Random.value < 0.5f ? -1f : 1f) * preferredDistance; // arena não configurada
        teleportTargetX = best;
    }

    // ================================================================== invocação

    private void StartSummon()
    {
        pendingSummon = false;
        state = State.Summon;
        stateTimer = 0f;
        released = false;
        PlayClip(attackFrames, castFps * 0.8f, false, true);
        ProgressHud.Toast($"{bossName} invocou lacaios!", 2.5f);
    }

    private void DoSummon()
    {
        if (minionPrefab == null) return;
        minions.RemoveAll(m => m == null);
        for (int i = 0; i < minionsPerSummon; i++)
        {
            float side = (i % 2 == 0) ? 1f : -1f;
            float x = Mathf.Clamp(transform.position.x + side * (2f + i), arenaMinX + 0.5f, arenaMaxX - 0.5f);
            Vector3 pos = new Vector3(x, transform.position.y + 0.6f, 0f);
            var m = Instantiate(minionPrefab, pos, Quaternion.identity);
            m.name = minionPrefab.name + "_Invocado";
            minions.Add(m);
            FireVFX.SpawnFlameBurst(pos, 16);
            IceVFX.SpawnFrostBurst(pos, 10);
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = new Color(0.8f, 0.4f, 1f);
        Gizmos.DrawWireSphere(transform.position, preferredDistance);
    }
}
