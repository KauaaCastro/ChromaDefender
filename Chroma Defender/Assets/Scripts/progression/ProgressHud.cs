using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HUD leve de progressão (desenhada via OnGUI, sem precisar montar Canvas):
///  - Mensagens rápidas no centro-inferior da tela (Toast).
///  - Chaves que o jogador carrega (canto superior direito).
///  - Barra de vida do chefe (topo da tela).
/// Criada automaticamente ao iniciar o jogo e persiste entre cenas.
/// </summary>
public class ProgressHud : MonoBehaviour
{
    private static ProgressHud instance;

    private string toastText;
    private float toastUntil;

    private bool bossVisible;
    private string bossName;
    private float bossHealth01;
    private string bossSubtitle;
    private Color bossBarColor = new Color(0.8f, 0.15f, 0.15f);

    private GUIStyle toastStyle;
    private GUIStyle keyStyle;
    private GUIStyle bossStyle;
    private Texture2D whiteTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => EnsureInstance();

    private static ProgressHud EnsureInstance()
    {
        if (instance != null) return instance;
        var go = new GameObject("[ProgressHud]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ProgressHud>();
        return instance;
    }

    public static void Toast(string message, float duration = 3.5f)
    {
        var hud = EnsureInstance();
        hud.toastText = message;
        hud.toastUntil = Time.unscaledTime + duration;
    }

    public static void ShowBoss(string name, float health01, string subtitle, Color barColor)
    {
        var hud = EnsureInstance();
        hud.bossVisible = true;
        hud.bossName = name;
        hud.bossHealth01 = Mathf.Clamp01(health01);
        hud.bossSubtitle = subtitle;
        hud.bossBarColor = barColor;
    }

    public static void HideBoss()
    {
        if (instance != null) instance.bossVisible = false;
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        bossVisible = false;
    }

    private void BuildStyles(float scale)
    {
        if (whiteTex == null)
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }

        toastStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(22 * scale),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            richText = true
        };
        toastStyle.normal.textColor = Color.white;

        keyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = Mathf.RoundToInt(18 * scale),
            fontStyle = FontStyle.Bold,
            richText = true
        };
        keyStyle.normal.textColor = new Color(1f, 0.86f, 0.3f);

        bossStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(20 * scale),
            fontStyle = FontStyle.Bold,
            richText = true
        };
        bossStyle.normal.textColor = Color.white;
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name == "MainMenu") return;

        float scale = Screen.height / 720f;
        BuildStyles(scale);

        // --- Chaves carregadas ---
        var lines = new List<string>();
        foreach (var k in GameProgress.Keys) lines.Add("» " +GameProgress.KeyDisplayName(k));
        if (lines.Count > 0)
        {
            float w = 380 * scale, h = 26 * scale;
            for (int i = 0; i < lines.Count; i++)
            {
                Rect r = new Rect(Screen.width - w - 20 * scale, 90 * scale + i * h, w, h);
                DrawShadowLabel(r, lines[i], keyStyle);
            }
        }

        // --- Barra do chefe ---
        if (bossVisible)
        {
            float bw = Screen.width * 0.5f, bh = 18 * scale;
            float bx = (Screen.width - bw) * 0.5f, by = 40 * scale;
            DrawShadowLabel(new Rect(bx, by - 30 * scale, bw, 28 * scale), bossName, bossStyle);
            DrawRect(new Rect(bx - 3, by - 3, bw + 6, bh + 6), new Color(0, 0, 0, 0.8f));
            DrawRect(new Rect(bx, by, bw, bh), new Color(0.2f, 0.05f, 0.05f, 1f));
            DrawRect(new Rect(bx, by, bw * bossHealth01, bh), bossBarColor);
            if (!string.IsNullOrEmpty(bossSubtitle))
            {
                var sub = new GUIStyle(bossStyle) { fontSize = Mathf.RoundToInt(16 * scale) };
                DrawShadowLabel(new Rect(bx, by + bh + 4 * scale, bw, 24 * scale), bossSubtitle, sub);
            }
        }

        // --- Toast ---
        if (!string.IsNullOrEmpty(toastText) && Time.unscaledTime < toastUntil)
        {
            float remaining = toastUntil - Time.unscaledTime;
            float alpha = Mathf.Clamp01(remaining / 0.4f);
            float w = Mathf.Min(Screen.width * 0.8f, 900 * scale), h = 70 * scale;
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height * 0.72f, w, h);
            DrawRect(r, new Color(0f, 0f, 0f, 0.6f * alpha));
            Color prev = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.Label(r, toastText, toastStyle);
            GUI.color = prev;
        }
    }

    private void DrawRect(Rect r, Color c)
    {
        Color prev = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, whiteTex);
        GUI.color = prev;
    }

    private static void DrawShadowLabel(Rect r, string text, GUIStyle style)
    {
        Color prev = style.normal.textColor;
        style.normal.textColor = new Color(0, 0, 0, 0.85f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
        style.normal.textColor = prev;
        GUI.Label(r, text, style);
    }
}
