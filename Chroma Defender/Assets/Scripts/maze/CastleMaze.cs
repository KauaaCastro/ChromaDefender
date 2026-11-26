using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Labirinto do Castelo — gerenciador (um por cena).
///  - Descobre em que sala o cavaleiro está e mostra o nome da sala ao entrar.
///  - HUD no canto inferior esquerdo: sala atual e "Salas exploradas X / Y".
///  - Guarda as salas visitadas em GameProgress (continua valendo ao morrer ou sair e voltar ao castelo).
/// </summary>
public class CastleMaze : MonoBehaviour
{
    [SerializeField] private string mazeName = "Labirinto do Castelo";
    [SerializeField] private bool showHud = true;
    [SerializeField] private float bannerDuration = 2.2f;

    private static CastleMaze instance;

    private PlayerBehavior player;
    private CastleMazeRoom current;
    private float nextCheck;
    private string bannerText;
    private float bannerUntil;
    private GUIStyle hudStyle;
    private GUIStyle bannerStyle;
    private Texture2D panelTex;

    public static CastleMazeRoom CurrentRoom => instance != null ? instance.current : null;

    private void OnEnable() => instance = this;
    private void OnDisable() { if (instance == this) instance = null; }

    /// <summary>Chamado pelas portas logo após o teletransporte (atualiza a sala na hora).</summary>
    public static void NotifyTeleported()
    {
        if (instance != null) instance.nextCheck = 0f;
    }

    private void Update()
    {
        if (Time.time < nextCheck) return;
        nextCheck = Time.time + 0.15f;

        if (player == null) player = FindAnyObjectByType<PlayerBehavior>();
        if (player == null) return;

        var room = CastleMazeRoom.At(player.transform.position);
        if (room == current) return;
        current = room;
        if (room == null) return;

        bool first = !room.Visited;
        if (first) GameProgress.SetFlag(room.VisitedFlag);
        bannerText = room.RoomName;
        bannerUntil = Time.unscaledTime + bannerDuration;
        if (first && !string.IsNullOrEmpty(room.FirstVisitMessage)) ProgressHud.Toast(room.FirstVisitMessage, 4.5f);
    }

    private int VisitedCount()
    {
        int n = 0;
        foreach (var r in CastleMazeRoom.All) if (r != null && r.Visited) n++;
        return n;
    }

    private void OnGUI()
    {
        if (!showHud) return;
        float s = Screen.height / 720f;
        BuildStyles(s);

        // Nome da sala (banner que some sozinho)
        float left = bannerUntil - Time.unscaledTime;
        if (left > 0f && !string.IsNullOrEmpty(bannerText))
        {
            float a = Mathf.Clamp01(left / 0.6f) * Mathf.Clamp01((bannerDuration - left) / 0.25f + 0.2f);
            var r = new Rect(0, Screen.height * 0.16f, Screen.width, 50 * s);
            bannerStyle.normal.textColor = new Color(0f, 0f, 0f, 0.8f * a);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), bannerText, bannerStyle);
            bannerStyle.normal.textColor = new Color(1f, 0.86f, 0.45f, a);
            GUI.Label(r, bannerText, bannerStyle);
        }

        // Painel fixo
        int total = CastleMazeRoom.All.Count;
        if (total == 0) return;
        string where = current != null ? current.RoomName : "—";
        string text = $"<b>{mazeName.ToUpper()}</b>\n{where}\n<size={Mathf.RoundToInt(13 * s)}>Salas exploradas: {VisitedCount()} / {total}</size>";
        var panel = new Rect(12 * s, Screen.height - 86 * s, 260 * s, 74 * s);
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(panel, panelTex);
        GUI.color = Color.white;
        GUI.Label(new Rect(panel.x + 10 * s, panel.y + 6 * s, panel.width - 20 * s, panel.height - 12 * s), text, hudStyle);
    }

    private void BuildStyles(float s)
    {
        if (panelTex == null)
        {
            panelTex = new Texture2D(1, 1);
            panelTex.SetPixel(0, 0, Color.white);
            panelTex.Apply();
        }
        if (hudStyle == null) hudStyle = new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.UpperLeft };
        hudStyle.fontSize = Mathf.RoundToInt(15 * s);
        hudStyle.normal.textColor = new Color(0.92f, 0.88f, 0.78f);
        if (bannerStyle == null) bannerStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        bannerStyle.fontSize = Mathf.RoundToInt(34 * s);
    }
}

/// <summary>Utilidades de interface do labirinto (texto preso a um ponto do mundo, tecla de interação).</summary>
public static class MazeGui
{
    private static GUIStyle style;

    public static bool InteractPressed()
    {
        var kb = Keyboard.current;
        return kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame);
    }

    /// <summary>Desenha um texto com sombra num ponto do mundo. Chame dentro de OnGUI.</summary>
    public static void WorldLabel(Vector3 worldPos, string text, int fontSize, Color color, float width = 340f)
    {
        var cam = Camera.main;
        if (cam == null || string.IsNullOrEmpty(text)) return;
        Vector3 sp = cam.WorldToScreenPoint(worldPos);
        if (sp.z < 0f) return;

        if (style == null) style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, richText = true, wordWrap = true };
        float s = Screen.height / 720f;
        style.fontSize = Mathf.RoundToInt(fontSize * s);
        float w = width * s;
        float h = style.CalcHeight(new GUIContent(text), w);
        var rect = new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h * 0.5f, w, h);

        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f * color.a);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), StripColor(text), style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    private static string StripColor(string t) => System.Text.RegularExpressions.Regex.Replace(t, "</?color[^>]*>", "");
}
