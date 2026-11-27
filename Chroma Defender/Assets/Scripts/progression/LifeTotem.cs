using UnityEngine;

/// <summary>
/// Totem de Vida da luta contra o chefe final.
/// Enquanto o chefe estiver acordado, aparece a cada <see cref="interval"/> segundos e fica ativo por
/// <see cref="activeDuration"/> segundos. Parado perto dele, o jogador recupera vida aos poucos
/// (no máximo <see cref="healCap"/> por aparição). Some quando o tempo acaba ou a cura se esgota.
/// </summary>
public class LifeTotem : MonoBehaviour
{
    [SerializeField] private BossBase boss;
    [SerializeField] private GameObject visual;
    [SerializeField] private SpriteRenderer glow;
    [SerializeField] private float interval = 90f;
    [SerializeField] private float activeDuration = 20f;
    [SerializeField] private float radius = 2.4f;
    [SerializeField] private float healPerSecond = 10f;
    [SerializeField] private int healCap = 60;

    private PlayerBehavior player;
    private float timer;
    private bool active;
    private float activeTimer;
    private int healed;
    private float pending;
    private GUIStyle style;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerBehavior>();
        if (player != null) player.OnPlayerRespawn += ResetTotem;
        ResetTotem();
    }

    private void OnDestroy()
    {
        if (player != null) player.OnPlayerRespawn -= ResetTotem;
    }

    private void ResetTotem()
    {
        timer = 0f;
        Show(false);
    }

    private void Show(bool on)
    {
        active = on;
        activeTimer = 0f;
        healed = 0;
        pending = 0f;
        if (visual != null) visual.SetActive(on);
    }

    private bool Fighting => boss != null && boss.IsAwake && !boss.IsDead && player != null && !player.IsDead;

    private void Update()
    {
        if (boss == null || boss.IsDead) { if (active) Show(false); return; }
        if (!Fighting) return;

        if (!active)
        {
            timer += Time.deltaTime;
            if (timer >= interval)
            {
                timer = 0f;
                Show(true);
                FireVFX.SpawnFlameBurst(transform.position + Vector3.up, 20);
                ProgressHud.Toast($"<color=#7CFF9A>O Totem de Vida surgiu!</color>\nFique perto dele para recuperar até {healCap} de vida ({activeDuration:0}s).", 3.5f);
            }
            return;
        }

        activeTimer += Time.deltaTime;
        bool near = Vector2.Distance(player.transform.position, transform.position) <= radius;
        if (near && healed < healCap && player.CurrentHealth < player.MaxHealth)
        {
            pending += healPerSecond * Time.deltaTime;
            int whole = Mathf.FloorToInt(pending);
            if (whole > 0) { pending -= whole; healed += player.Heal(Mathf.Min(whole, healCap - healed)); }
        }
        if (glow != null)
        {
            float left = 1f - healed / (float)healCap;
            var c = glow.color; c.a = Mathf.Lerp(0.15f, 1f, left) * (0.6f + 0.4f * Mathf.Sin(Time.time * (near ? 9f : 3f))); glow.color = c;
        }
        if (activeTimer >= activeDuration || healed >= healCap) Show(false);
    }

    private void OnGUI()
    {
        if (!Fighting) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperLeft };
        float scale = Mathf.Max(0.75f, Screen.height / 720f);
        style.fontSize = Mathf.RoundToInt(16 * scale);
        string text = active ? $"Totem de Vida: ATIVO ({Mathf.CeilToInt(activeDuration - activeTimer)}s)  cura {healed}/{healCap}"
                             : $"Totem de Vida em {Mathf.CeilToInt(interval - timer)}s";
        var rect = new Rect(16f * scale, 110f * scale, 600f * scale, 30f * scale);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = active ? new Color(0.5f, 1f, 0.6f) : new Color(0.85f, 0.85f, 0.85f);
        GUI.Label(rect, text, style);
    }
}
