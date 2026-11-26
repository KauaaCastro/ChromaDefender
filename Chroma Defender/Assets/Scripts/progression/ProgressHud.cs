using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// HUD estilizada de progressão em Canvas (ScreenSpaceOverlay):
///  - Barra de vida do chefe no topo central (com barra de dano atrasada, escudo elemental e dica de contra-ataque).
///  - Painel de chaves coletadas no canto superior direito, abaixo da Polaridade (sem sobrepor os slots de Fogo/Gelo).
///  - Caixa de notificação (Toast) estilizada no centro-inferior para Nova Técnica, Baús, Chaves e Checkpoints.
/// Criada automaticamente ao iniciar o jogo e persiste entre cenas.
/// </summary>
public class ProgressHud : MonoBehaviour
{
    private static ProgressHud instance;

    private static readonly Color FireColor = new Color(1f, 0.45f, 0.1f);
    private static readonly Color IceColor = new Color(0.35f, 0.82f, 1f);
    private static readonly Color GoldColor = new Color(1f, 0.84f, 0.28f);
    private static readonly Color SilverColor = new Color(0.75f, 0.92f, 1f);

    // --- Estado do Toast ---
    private string toastText;
    private float toastUntil;
    private float toastShownAt;

    // --- Estado do Boss ---
    private bool bossVisible;
    private string bossName;
    private float bossHealth01 = 1f;
    private float bossTrail01 = 1f;
    private string bossSubtitle;
    private Color bossBarColor = new Color(0.8f, 0.15f, 0.15f);

    // --- Referências do Canvas ---
    private Canvas rootCanvas;
    private Font uiFont;

    // Boss UI
    private CanvasGroup bossGroup;
    private RectTransform bossPanel;
    private Image bossBorder;
    private RectTransform bossFill;
    private Image bossFillImage;
    private RectTransform bossTrailFill;
    private Text bossNameText;
    private Text bossPercentText;
    private Text bossSubText;
    private Image bossShieldFrame;
    private Image bossShieldIcon;

    // Keys UI
    private RectTransform keysContainer;
    private readonly List<GameObject> keyBadges = new List<GameObject>();
    private int lastKeyCount = -1;

    // Toast UI
    private CanvasGroup toastGroup;
    private RectTransform toastPanel;
    private Image toastBorder;
    private Image toastIconFrame;
    private Image toastIcon;
    private Text toastLabel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot() => EnsureInstance();

    private static ProgressHud EnsureInstance()
    {
        if (instance != null) return instance;
        var go = new GameObject("[ProgressHud]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<ProgressHud>();
        instance.BuildCanvas();
        return instance;
    }

    public static void Toast(string message, float duration = 3.5f)
    {
        var hud = EnsureInstance();
        hud.toastText = message;
        hud.toastShownAt = Time.unscaledTime;
        hud.toastUntil = Time.unscaledTime + duration;
        hud.ConfigureToastStyle(message);
    }

    public static void ShowBoss(string name, float health01, string subtitle, Color barColor)
    {
        var hud = EnsureInstance();
        float clamped = Mathf.Clamp01(health01);
        if (!hud.bossVisible)
        {
            hud.bossTrail01 = clamped;
        }
        hud.bossVisible = true;
        hud.bossName = name;
        hud.bossHealth01 = clamped;
        hud.bossSubtitle = subtitle;
        hud.bossBarColor = barColor;
        hud.RefreshBossImmediate();
    }

    public static void HideBoss()
    {
        if (instance != null) instance.bossVisible = false;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        GameProgress.OnChanged += RefreshKeys;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        GameProgress.OnChanged -= RefreshKeys;
    }

    private void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        bossVisible = false;
        RefreshKeys();
    }

    // ------------------------------------------------------------------
    // Construção do Canvas em tempo de execução
    // ------------------------------------------------------------------
    private void BuildCanvas()
    {
        if (rootCanvas != null) return;

        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 50;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildBossPanel();
        BuildKeysPanel();
        BuildToastPanel();
        RefreshKeys();
    }

    private void BuildBossPanel()
    {
        var root = NewRect("PainelBoss", transform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(820f, 126f), new Vector2(0.5f, 1f));
        bossPanel = root;
        bossGroup = root.gameObject.AddComponent<CanvasGroup>();
        bossGroup.alpha = 0f;
        bossGroup.interactable = false;
        bossGroup.blocksRaycasts = false;

        // Nome do Chefe
        bossNameText = NewText("NomeBoss", root, "CEIFADOR SOMBRIO", 28, TextAnchor.MiddleCenter);
        SetRect(bossNameText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(780f, 34f), new Vector2(0.5f, 1f));
        bossNameText.color = new Color(1f, 0.93f, 0.85f);

        // Moldura externa da barra do Boss
        var barOuter = NewRect("MolduraBarraBoss", root, new Vector2(0.5f, 1f), new Vector2(18f, -40f), new Vector2(730f, 34f), new Vector2(0.5f, 1f));
        bossBorder = barOuter.gameObject.AddComponent<Image>();
        bossBorder.color = new Color(1f, 0.45f, 0.1f, 0.85f);
        bossBorder.raycastTarget = false;

        var barBg = NewRect("FundoBarraBoss", barOuter, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(barBg, 3f);
        var bgImg = barBg.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.06f, 0.03f, 0.05f, 0.95f);
        bgImg.raycastTarget = false;

        // Barra de rastro de dano (branca/dourada que desce suavemente)
        bossTrailFill = NewRect("RastroDano", barBg, Vector2.zero, Vector2.zero, Vector2.zero);
        bossTrailFill.anchorMin = Vector2.zero;
        bossTrailFill.anchorMax = Vector2.one;
        bossTrailFill.pivot = new Vector2(0f, 0.5f);
        bossTrailFill.offsetMin = new Vector2(2f, 2f);
        bossTrailFill.offsetMax = new Vector2(-2f, -2f);
        var trailImg = bossTrailFill.gameObject.AddComponent<Image>();
        trailImg.color = new Color(1f, 0.88f, 0.55f, 0.7f);
        trailImg.raycastTarget = false;

        // Preenchimento principal de vida do Boss
        bossFill = NewRect("PreenchimentoBoss", barBg, Vector2.zero, Vector2.zero, Vector2.zero);
        bossFill.anchorMin = Vector2.zero;
        bossFill.anchorMax = Vector2.one;
        bossFill.pivot = new Vector2(0f, 0.5f);
        bossFill.offsetMin = new Vector2(2f, 2f);
        bossFill.offsetMax = new Vector2(-2f, -2f);
        bossFillImage = bossFill.gameObject.AddComponent<Image>();
        bossFillImage.color = bossBarColor;
        bossFillImage.raycastTarget = false;

        // Porcentagem de vida no centro da barra
        bossPercentText = NewText("TextoPorcentagemBoss", barOuter, "100%", 20, TextAnchor.MiddleCenter);
        Stretch(bossPercentText.rectTransform, 0f);

        // Medalhão de escudo elemental à esquerda da barra
        var medal = NewRect("MedalhaoEscudo", root, new Vector2(0.5f, 1f), new Vector2(-362f, -57f), new Vector2(58f, 58f), new Vector2(0.5f, 0.5f));
        bossShieldFrame = medal.gameObject.AddComponent<Image>();
        bossShieldFrame.sprite = HudSprites.Disc;
        bossShieldFrame.color = new Color(0.12f, 0.08f, 0.12f, 0.96f);
        bossShieldFrame.raycastTarget = false;

        var medalRing = NewRect("AnelEscudo", medal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f), new Vector2(0.5f, 0.5f));
        var ringImg = medalRing.gameObject.AddComponent<Image>();
        ringImg.sprite = HudSprites.Ring;
        ringImg.color = new Color(1f, 0.85f, 0.4f, 0.8f);
        ringImg.raycastTarget = false;

        var iconRt = NewRect("IconeEscudo", medal, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f), new Vector2(0.5f, 0.5f));
        bossShieldIcon = iconRt.gameObject.AddComponent<Image>();
        bossShieldIcon.sprite = HudSprites.Flame;
        bossShieldIcon.preserveAspect = true;
        bossShieldIcon.raycastTarget = false;

        // Faixa de dica elemental abaixo da barra
        var subBg = NewRect("FundoDicaEscudo", root, new Vector2(0.5f, 1f), new Vector2(18f, -80f), new Vector2(620f, 32f), new Vector2(0.5f, 1f));
        var subBgImg = subBg.gameObject.AddComponent<Image>();
        subBgImg.color = new Color(0.03f, 0.03f, 0.07f, 0.82f);
        subBgImg.raycastTarget = false;

        bossSubText = NewText("SubtituloBoss", subBg, "", 20, TextAnchor.MiddleCenter);
        bossSubText.supportRichText = true;
        Stretch(bossSubText.rectTransform, 2f);
    }

    private void BuildKeysPanel()
    {
        // Posicionado no canto superior direito ABAIXO do painel de Polaridade (y = -265),
        // evitando qualquer sobreposição com os slots de Fogo e Gelo.
        keysContainer = NewRect("PainelChaves", transform, new Vector2(1f, 1f), new Vector2(-40f, -265f), new Vector2(360f, 160f), new Vector2(1f, 1f));
    }

    private void BuildToastPanel()
    {
        var root = NewRect("PainelToast", transform, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(880f, 104f), new Vector2(0.5f, 0f));
        toastPanel = root;
        toastGroup = root.gameObject.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;
        toastGroup.interactable = false;
        toastGroup.blocksRaycasts = false;

        toastBorder = root.gameObject.AddComponent<Image>();
        toastBorder.color = new Color(1f, 0.84f, 0.28f, 0.85f);
        toastBorder.raycastTarget = false;

        var inner = NewRect("FundoToast", root, Vector2.zero, Vector2.zero, Vector2.zero);
        Stretch(inner, 3f);
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(0.04f, 0.04f, 0.08f, 0.94f);
        innerImg.raycastTarget = false;

        // Medalhão lateral com ícone contextual (Espada, Chave, Fogo/Gelo)
        var badge = NewRect("MedalhaoToast", inner, new Vector2(0f, 0.5f), new Vector2(52f, 0f), new Vector2(68f, 68f), new Vector2(0.5f, 0.5f));
        toastIconFrame = badge.gameObject.AddComponent<Image>();
        toastIconFrame.sprite = HudSprites.Disc;
        toastIconFrame.color = new Color(0.14f, 0.12f, 0.06f, 0.95f);
        toastIconFrame.raycastTarget = false;

        var iconRt = NewRect("IconeToast", badge, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f), new Vector2(0.5f, 0.5f));
        toastIcon = iconRt.gameObject.AddComponent<Image>();
        toastIcon.sprite = HudSprites.Sword;
        toastIcon.preserveAspect = true;
        toastIcon.raycastTarget = false;

        toastLabel = NewText("TextoToast", inner, "", 25, TextAnchor.MiddleLeft);
        toastLabel.supportRichText = true;
        toastLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
        toastLabel.verticalOverflow = VerticalWrapMode.Overflow;
        toastLabel.rectTransform.anchorMin = Vector2.zero;
        toastLabel.rectTransform.anchorMax = Vector2.one;
        toastLabel.rectTransform.offsetMin = new Vector2(100f, 10f);
        toastLabel.rectTransform.offsetMax = new Vector2(-24f, -10f);
    }

    // ------------------------------------------------------------------
    // Atualização Visual
    // ------------------------------------------------------------------
    private void Update()
    {
        if (rootCanvas == null) BuildCanvas();

        bool inMenu = SceneManager.GetActiveScene().name == "MainMenu";
        if (rootCanvas.enabled == inMenu) rootCanvas.enabled = !inMenu;
        if (inMenu) return;

        float dt = Time.unscaledDeltaTime;

        // --- Atualização da Barra do Boss ---
        if (bossGroup != null)
        {
            float targetAlpha = bossVisible ? 1f : 0f;
            bossGroup.alpha = Mathf.MoveTowards(bossGroup.alpha, targetAlpha, dt * 4f);

            if (bossVisible || bossGroup.alpha > 0.01f)
            {
                if (bossHealth01 > bossTrail01) bossTrail01 = bossHealth01; // regenerou
                else bossTrail01 = Mathf.MoveTowards(bossTrail01, bossHealth01, dt * 0.35f);

                if (bossFill != null) bossFill.anchorMax = new Vector2(bossHealth01, 1f);
                if (bossTrailFill != null) bossTrailFill.anchorMax = new Vector2(bossTrail01, 1f);
            }
        }

        // --- Atualização do Toast ---
        if (toastGroup != null)
        {
            bool active = !string.IsNullOrEmpty(toastText) && Time.unscaledTime < toastUntil;
            if (active)
            {
                float elapsed = Time.unscaledTime - toastShownAt;
                float remaining = toastUntil - Time.unscaledTime;
                float fadeIn = Mathf.Clamp01(elapsed / 0.18f);
                float fadeOut = Mathf.Clamp01(remaining / 0.35f);
                toastGroup.alpha = Mathf.Min(fadeIn, fadeOut);

                float pop = 1f + 0.06f * Mathf.Sin(Mathf.Clamp01(elapsed / 0.22f) * Mathf.PI);
                toastPanel.localScale = Vector3.one * pop;
                toastPanel.anchoredPosition = new Vector2(0f, Mathf.Lerp(95f, 130f, fadeIn));
            }
            else
            {
                toastGroup.alpha = Mathf.MoveTowards(toastGroup.alpha, 0f, dt * 5f);
            }
        }

        // Verifica se a contagem de chaves mudou
        int count = 0;
        foreach (var _ in GameProgress.Keys) count++;
        if (count != lastKeyCount) RefreshKeys();
    }

    private void RefreshBossImmediate()
    {
        if (rootCanvas == null) BuildCanvas();

        if (bossNameText != null) bossNameText.text = (bossName ?? "CHEFE").ToUpperInvariant();
        if (bossFillImage != null) bossFillImage.color = bossBarColor;
        if (bossFill != null) bossFill.anchorMax = new Vector2(bossHealth01, 1f);
        if (bossPercentText != null) bossPercentText.text = $"{Mathf.CeilToInt(bossHealth01 * 100f)}%";
        if (bossSubText != null) bossSubText.text = bossSubtitle ?? "";

        // Detecta se o escudo atual é Fogo ou Gelo pela cor/subtítulo
        bool fireShield = bossBarColor.r > bossBarColor.b;
        Color accent = fireShield ? FireColor : IceColor;
        if (bossBorder != null) bossBorder.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        if (bossShieldIcon != null)
        {
            bossShieldIcon.sprite = fireShield ? HudSprites.Flame : HudSprites.Ice;
            bossShieldIcon.color = Color.white;
        }
        if (bossShieldFrame != null)
        {
            bossShieldFrame.color = Color.Lerp(new Color(0.08f, 0.08f, 0.12f, 0.96f), accent, 0.3f);
        }
    }

    private void ConfigureToastStyle(string msg)
    {
        if (rootCanvas == null) BuildCanvas();
        if (toastLabel != null) toastLabel.text = msg;

        string lower = (msg ?? "").ToLowerInvariant();
        Color accent = GoldColor;
        Sprite icon = HudSprites.Sword;
        Color iconTint = Color.white;

        if (lower.Contains("chave") || lower.Contains("cadeado") || lower.Contains("trancado"))
        {
            accent = lower.Contains("prata") || lower.Contains("poço") ? SilverColor : GoldColor;
            icon = HudSprites.Key;
            iconTint = accent;
        }
        else if (lower.Contains("baú") || lower.Contains("combo") || lower.Contains("golpe"))
        {
            accent = GoldColor;
            icon = HudSprites.Sword;
            iconTint = Color.white;
        }
        else if (lower.Contains("checkpoint") || lower.Contains("gelo"))
        {
            accent = IceColor;
            icon = HudSprites.Ice;
            iconTint = Color.white;
        }
        else if (lower.Contains("despertou") || lower.Contains("ceifador") || lower.Contains("escudo"))
        {
            accent = FireColor;
            icon = HudSprites.Flame;
            iconTint = Color.white;
        }

        if (toastBorder != null) toastBorder.color = new Color(accent.r, accent.g, accent.b, 0.85f);
        if (toastIconFrame != null) toastIconFrame.color = Color.Lerp(new Color(0.08f, 0.08f, 0.12f, 0.96f), accent, 0.28f);
        if (toastIcon != null)
        {
            toastIcon.sprite = icon;
            toastIcon.color = iconTint;
        }
    }

    private void RefreshKeys()
    {
        if (keysContainer == null) return;

        foreach (var go in keyBadges)
            if (go != null) Destroy(go);
        keyBadges.Clear();

        int idx = 0;
        foreach (var keyId in GameProgress.Keys)
        {
            bool isSilver = keyId == GameProgress.KeyPoco;
            Color accent = isSilver ? SilverColor : GoldColor;

            var card = NewRect($"Chave_{keyId}", keysContainer, new Vector2(1f, 1f), new Vector2(0f, -idx * 50f), new Vector2(340f, 44f), new Vector2(1f, 1f));
            var borderImg = card.gameObject.AddComponent<Image>();
            borderImg.color = new Color(accent.r, accent.g, accent.b, 0.75f);
            borderImg.raycastTarget = false;

            var inner = NewRect("Fundo", card, Vector2.zero, Vector2.zero, Vector2.zero);
            Stretch(inner, 2f);
            var innerImg = inner.gameObject.AddComponent<Image>();
            innerImg.color = new Color(0.04f, 0.04f, 0.08f, 0.9f);
            innerImg.raycastTarget = false;

            var iconRt = NewRect("IconeChave", inner, new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(30f, 30f), new Vector2(0.5f, 0.5f));
            var iconImg = iconRt.gameObject.AddComponent<Image>();
            iconImg.sprite = HudSprites.Key;
            iconImg.color = accent;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var label = NewText("NomeChave", inner, GameProgress.KeyDisplayName(keyId), 19, TextAnchor.MiddleLeft);
            label.color = accent;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(50f, 0f);
            label.rectTransform.offsetMax = new Vector2(-12f, 0f);

            keyBadges.Add(card.gameObject);
            idx++;
        }
        lastKeyCount = idx;
    }

    // ------------------------------------------------------------------
    // Utilitários de UI
    // ------------------------------------------------------------------
    private static RectTransform NewRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        SetRect(rt, anchor, pos, size, pivot ?? anchor);
        return rt;
    }

    private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot ?? anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void Stretch(RectTransform rt, float inset)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(inset, inset);
        rt.offsetMax = new Vector2(-inset, -inset);
    }

    private Text NewText(string name, Transform parent, string content, int size, TextAnchor anchor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.fontSize = size;
        t.fontStyle = FontStyle.Bold;
        t.alignment = anchor;
        t.color = Color.white;
        t.raycastTarget = false;
        go.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        return t;
    }
}
