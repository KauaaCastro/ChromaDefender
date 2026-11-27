using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Arena de batalha por ondas.
///  - O jogador entra no gatilho (BoxCollider2D trigger deste objeto) → a porta da arena é selada.
///  - Ondas de inimigos surgem nos pontos de spawn, das mais fracas às mais fortes.
///  - Entre as ondas o Santuário de Cura acende: parado perto dele o jogador recupera vida aos poucos,
///    no máximo <see cref="healPerBreak"/> pontos por intervalo.
///  - Depois da última onda surge o chefe (cópia do <see cref="bossTemplate"/>, que fica desativado na cena).
///  - Vitória: grava a flag, libera a porta e revela a recompensa.
///  - Se o jogador morrer, a arena é reiniciada (inimigos somem, porta abre) e pode ser tentada de novo.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ArenaWaves : MonoBehaviour
{
    [System.Serializable]
    public class Wave
    {
        public string title = "Onda";
        public GameObject[] enemies;
    }

    [Header("--- Arena ---")]
    [SerializeField] private string arenaName = "Arena de Batalha";
    [SerializeField] private string completedFlag = "arena_batalha_vencida";
    [SerializeField] private float arenaMinX = 0f;
    [SerializeField] private float arenaMaxX = 20f;
    [SerializeField] private CastleDoor arenaDoor;
    [SerializeField] private Transform[] spawnPoints;
    [Tooltip("Raio de detecção aplicado aos inimigos das ondas (para eles virem atrás do jogador pela arena toda).")]
    [SerializeField] private float enemyDetectionRadius = 14f;
    [Tooltip("Multiplica a vida dos inimigos das ondas (arena mais difícil que o mapa aberto).")]
    [SerializeField] private float enemyHealthMultiplier = 1.5f;

    [Header("--- Ondas ---")]
    [SerializeField] private Wave[] waves;
    [SerializeField] private float spawnInterval = 0.45f;

    [Header("--- Chefe final ---")]
    [Tooltip("Chefe DESATIVADO na cena; a arena cria uma cópia a cada tentativa.")]
    [SerializeField] private GameObject bossTemplate;

    [Header("--- Santuário de Cura (entre as ondas) ---")]
    [SerializeField] private Transform shrine;
    [SerializeField] private SpriteRenderer shrineGlow;
    [SerializeField] private float healRadius = 2.4f;
    [SerializeField] private int healPerBreak = 50;
    [SerializeField] private float healPerSecond = 8f;
    [SerializeField] private float breakDuration = 9f;

    [Header("--- Recompensa ---")]
    [Tooltip("Objeto desativado (ex.: baú) revelado ao vencer.")]
    [SerializeField] private GameObject reward;

    private readonly List<GameObject> alive = new List<GameObject>();
    private GameObject bossInstance;
    private PlayerBehavior player;
    private Coroutine routine;
    private bool running;
    private bool breakActive;
    private string hudText;
    private GUIStyle hudStyle;

    private bool Completed => GameProgress.HasFlag(completedFlag);

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
        if (bossTemplate != null) bossTemplate.SetActive(false);
        SetGlow(0f);
    }

    private void Start()
    {
        player = FindAnyObjectByType<PlayerBehavior>();
        if (player != null) player.OnPlayerRespawn += ResetArena;
        if (reward != null) reward.SetActive(Completed);
    }

    private void OnDestroy()
    {
        if (player != null) player.OnPlayerRespawn -= ResetArena;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (running || Completed) return;
        var p = other.GetComponentInParent<PlayerBehavior>();
        if (p == null || p.IsDead) return;
        player = p;
        routine = StartCoroutine(RunArena());
    }

    // ------------------------------------------------------------------ fluxo

    private IEnumerator RunArena()
    {
        running = true;
        if (arenaDoor != null) arenaDoor.SetSealed(true, "A porta foi selada! Vença todas as ondas para sair.");
        ProgressHud.Toast($"<color=#FF6060>{arenaName}</color>\nSobreviva a {waves.Length} ondas e derrote o chefe!", 3.5f);
        yield return new WaitForSeconds(2.5f);

        for (int i = 0; i < waves.Length; i++)
        {
            var w = waves[i];
            hudText = $"Onda {i + 1}/{waves.Length} — {w.title}";
            ProgressHud.Toast($"<color=#FFD54A>Onda {i + 1}/{waves.Length}</color>\n{w.title}", 2.5f);
            yield return SpawnWave(w);
            yield return new WaitUntil(() => AliveCount() == 0);

            bool last = i == waves.Length - 1;
            string next = last && bossTemplate != null ? "O CHEFE vem aí" : $"Onda {i + 2}";
            yield return HealBreak($"Onda {i + 1} vencida! {next} em {breakDuration:0}s.");
        }

        if (bossTemplate != null)
        {
            hudText = "Chefe final";
            bossInstance = Instantiate(bossTemplate, bossTemplate.transform.position, bossTemplate.transform.rotation, bossTemplate.transform.parent);
            bossInstance.name = bossTemplate.name + "_Luta";
            bossInstance.SetActive(true);
            var boss = bossInstance.GetComponent<BossBase>();
            if (boss != null) boss.ConfigureArena(arenaMinX, arenaMaxX);
            FireVFX.SpawnFlameBurst(bossInstance.transform.position + Vector3.up * 1.5f, 30);
            yield return new WaitUntil(() => bossInstance == null || (boss != null && boss.IsDead));
            yield return new WaitForSeconds(2f);
        }

        Victory();
    }

    private IEnumerator SpawnWave(Wave w)
    {
        if (w.enemies == null) yield break;
        for (int i = 0; i < w.enemies.Length; i++)
        {
            var prefab = w.enemies[i];
            if (prefab == null) continue;
            Transform sp = spawnPoints != null && spawnPoints.Length > 0 ? spawnPoints[i % spawnPoints.Length] : transform;
            FireVFX.SpawnFlameBurst(sp.position + Vector3.up * 0.6f, 12);
            var go = Instantiate(prefab, sp.position, Quaternion.identity, transform);
            ExtendDetection(go);
            alive.Add(go);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator HealBreak(string message)
    {
        breakActive = true;
        ProgressHud.Toast($"{message}\nFique no <color=#7CFF9A>Santuário</color> para recuperar até {healPerBreak} de vida.", 4f);

        int healed = 0;
        float pending = 0f;
        for (float t = 0f; t < breakDuration; t += Time.deltaTime)
        {
            bool canHeal = healed < healPerBreak && player != null && !player.IsDead && player.CurrentHealth < player.MaxHealth;
            bool near = canHeal && shrine != null && Vector2.Distance(player.transform.position, shrine.position) <= healRadius;
            if (near)
            {
                pending += healPerSecond * Time.deltaTime;
                int whole = Mathf.FloorToInt(pending);
                if (whole > 0)
                {
                    pending -= whole;
                    healed += player.Heal(Mathf.Min(whole, healPerBreak - healed));
                }
            }
            // Brilho: forte enquanto ainda há cura disponível, pulsando mais rápido quando o jogador está curando.
            float remaining = 1f - healed / (float)healPerBreak;
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.time * (near ? 9f : 3f));
            SetGlow(Mathf.Lerp(0.15f, 1f, remaining) * pulse);
            hudText = $"Intervalo — próxima onda em {Mathf.CeilToInt(breakDuration - t)}s   (cura: {healed}/{healPerBreak})";
            yield return null;
        }
        SetGlow(0f);
        breakActive = false;
    }

    private void Victory()
    {
        running = false;
        hudText = null;
        GameProgress.SetFlag(completedFlag);
        if (arenaDoor != null) arenaDoor.SetSealed(false);
        if (reward != null)
        {
            reward.SetActive(true);
            FireVFX.SpawnFlameBurst(reward.transform.position + Vector3.up * 0.7f, 26);
        }
        ProgressHud.Toast($"<color=#FFD54A>{arenaName} conquistada!</color>\nUm baú dourado apareceu no centro da arena.", 5f);
    }

    private void ResetArena()
    {
        if (!running) return;
        if (routine != null) StopCoroutine(routine);
        routine = null;
        foreach (var go in alive) if (go != null) Destroy(go);
        alive.Clear();
        if (bossInstance != null) Destroy(bossInstance);
        bossInstance = null;
        if (arenaDoor != null) arenaDoor.SetSealed(false);
        SetGlow(0f);
        running = false;
        breakActive = false;
        hudText = null;
    }

    // ------------------------------------------------------------------ utilidades

    private int AliveCount()
    {
        alive.RemoveAll(go => go == null || IsDead(go));
        return alive.Count;
    }

    private static bool IsDead(GameObject go)
    {
        var e = go.GetComponent<EnemyBehavior>();
        return e != null && e.IsDead;
    }

    private static readonly FieldInfo detectionField =
        typeof(EnemyBehavior).GetField("detectionRadius", BindingFlags.NonPublic | BindingFlags.Instance);

    private static readonly FieldInfo maxHealthField =
        typeof(EnemyBehavior).GetField("maxHealth", BindingFlags.NonPublic | BindingFlags.Instance);
    private static readonly FieldInfo currentHealthField =
        typeof(EnemyBehavior).GetField("currentHealth", BindingFlags.NonPublic | BindingFlags.Instance);

    private void ExtendDetection(GameObject go)
    {
        var e = go.GetComponent<EnemyBehavior>();
        if (e == null) return;
        if (detectionField != null) detectionField.SetValue(e, enemyDetectionRadius);
        if (maxHealthField != null && currentHealthField != null && enemyHealthMultiplier > 0f)
        {
            int hp = Mathf.RoundToInt((int)maxHealthField.GetValue(e) * enemyHealthMultiplier);
            maxHealthField.SetValue(e, hp);
            currentHealthField.SetValue(e, hp);
        }
    }

    private void SetGlow(float a)
    {
        if (shrineGlow == null) return;
        var c = shrineGlow.color;
        c.a = Mathf.Clamp01(a);
        shrineGlow.color = c;
    }

    private void OnGUI()
    {
        if (!running || string.IsNullOrEmpty(hudText)) return;
        if (hudStyle == null)
        {
            hudStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }
        float scale = Mathf.Max(0.75f, Screen.height / 720f);
        hudStyle.fontSize = Mathf.RoundToInt(18 * scale);
        var rect = new Rect(0f, 60f * scale, Screen.width, 30f * scale);
        hudStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), hudText, hudStyle);
        hudStyle.normal.textColor = breakActive ? new Color(0.55f, 1f, 0.65f) : new Color(1f, 0.85f, 0.35f);
        GUI.Label(rect, hudText, hudStyle);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(arenaMinX, transform.position.y, 0f), new Vector3(arenaMaxX, transform.position.y, 0f));
        if (shrine != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(shrine.position, healRadius);
        }
    }
}
