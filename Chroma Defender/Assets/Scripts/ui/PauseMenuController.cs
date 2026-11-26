using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu de Pausa + Quadro de Controles do Chroma Defender.
/// Abre e fecha ao pressionar ESC ou P durante qualquer fase (exceto no MainMenu ou durante Game Over / Vitória).
/// Cria-se automaticamente em tempo de execução se não existir na cena, ou pode ser criado via
/// menu: Chroma Defender > Criar Menu de Pausa.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    private static readonly Color FireColor = new Color(1f, 0.38f, 0.12f);
    private static readonly Color IceColor = new Color(0.35f, 0.82f, 1f);
    private static readonly Color GoldColor = new Color(1f, 0.84f, 0.28f);

    [SerializeField] private string menuScene = "MainMenu";
    [SerializeField] private CanvasGroup rootGroup;
    [SerializeField] private RectTransform mainPanel;
    [SerializeField] private RectTransform controlsPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button controlsButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;
    [SerializeField] private Button backFromControlsButton;

    private Canvas rootCanvas;
    private Font uiFont;
    private bool isPaused;
    private bool showingControls;
    private float pausedAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        var existing = FindFirstObjectByType<PauseMenuController>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }
        var go = new GameObject("MenuPausa");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<PauseMenuController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureUI();
        WireButtons();
        SetPausedImmediate(false);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (isPaused) Time.timeScale = 1f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isPaused)
        {
            isPaused = false;
            showingControls = false;
            Time.timeScale = 1f;
            SetPausedImmediate(false);
        }
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
        {
            if (isPaused)
            {
                if (showingControls) ShowMainPanel();
                else Resume();
            }
            else if (CanPause())
            {
                Pause();
            }
        }

        if (rootGroup == null) return;

        float targetAlpha = isPaused ? 1f : 0f;
        rootGroup.alpha = Mathf.MoveTowards(rootGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 8f);

        if (!isPaused) return;

        float t = Time.unscaledTime - pausedAt;
        if (titleText != null)
        {
            float k = Mathf.Clamp01(t / 0.25f);
            float s = Mathf.Lerp(1.35f, 1f, 1f - (1f - k) * (1f - k));
            titleText.rectTransform.localScale = Vector3.one * s;
            titleText.color = Color.Lerp(FireColor, IceColor, (Mathf.Sin(t * 1.8f) + 1f) * 0.5f);
        }
    }

    private bool CanPause()
    {
        if (SceneManager.GetActiveScene().name == menuScene) return false;
        // Se o jogo já estiver pausado por outra tela (ex.: Vitória), não abre por cima
        if (Time.timeScale <= 0.01f) return false;

        var player = FindFirstObjectByType<PlayerBehavior>();
        if (player != null && player.IsDead) return false;

        return true;
    }

    public void Pause()
    {
        if (isPaused) return;
        EnsureUI();
        EnsureEventSystem();

        isPaused = true;
        showingControls = false;
        pausedAt = Time.unscaledTime;
        Time.timeScale = 0f;

        if (mainPanel != null) mainPanel.gameObject.SetActive(true);
        if (controlsPanel != null) controlsPanel.gameObject.SetActive(false);

        rootGroup.blocksRaycasts = true;
        rootGroup.interactable = true;

        if (resumeButton != null)
            EventSystem.current?.SetSelectedGameObject(resumeButton.gameObject);
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;
        showingControls = false;
        rootGroup.blocksRaycasts = false;
        rootGroup.interactable = false;
        EventSystem.current?.SetSelectedGameObject(null);

        StartCoroutine(UnpauseCleanRoutine());
    }

    private IEnumerator UnpauseCleanRoutine()
    {
        // Espera o fim do clique do botão para que o clique esquerdo não dispare um golpe da espada
        yield return null;
        ClearPlayerQueuedInputs();
        Time.timeScale = 1f;
        yield return null;
        ClearPlayerQueuedInputs();
    }

    private static void ClearPlayerQueuedInputs()
    {
        var player = FindFirstObjectByType<PlayerBehavior>();
        if (player == null) return;

        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var type = typeof(PlayerBehavior);
        type.GetField("lightQueued", flags)?.SetValue(player, false);
        type.GetField("heavyQueued", flags)?.SetValue(player, false);
        type.GetField("specialQueued", flags)?.SetValue(player, false);
        type.GetField("rollQueued", flags)?.SetValue(player, false);
    }

    public void ShowControlsPanel()
    {
        showingControls = true;
        if (mainPanel != null) mainPanel.gameObject.SetActive(false);
        if (controlsPanel != null) controlsPanel.gameObject.SetActive(true);
        if (backFromControlsButton != null)
            EventSystem.current?.SetSelectedGameObject(backFromControlsButton.gameObject);
    }

    public void ShowMainPanel()
    {
        showingControls = false;
        if (controlsPanel != null) controlsPanel.gameObject.SetActive(false);
        if (mainPanel != null) mainPanel.gameObject.SetActive(true);
        if (controlsButton != null)
            EventSystem.current?.SetSelectedGameObject(controlsButton.gameObject);
    }

    public void RestartLevel()
    {
        isPaused = false;
        showingControls = false;
        Time.timeScale = 1f;
        SetPausedImmediate(false);
        GameProgress.ResetProgress();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        isPaused = false;
        showingControls = false;
        Time.timeScale = 1f;
        SetPausedImmediate(false);
        SceneManager.LoadScene(menuScene);
    }

    private void SetPausedImmediate(bool v)
    {
        if (rootGroup == null) return;
        rootGroup.alpha = v ? 1f : 0f;
        rootGroup.blocksRaycasts = v;
        rootGroup.interactable = v;
    }

    private void WireButtons()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(Resume);
        }
        if (controlsButton != null)
        {
            controlsButton.onClick.RemoveAllListeners();
            controlsButton.onClick.AddListener(ShowControlsPanel);
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartLevel);
        }
        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoToMainMenu);
        }
        if (backFromControlsButton != null)
        {
            backFromControlsButton.onClick.RemoveAllListeners();
            backFromControlsButton.onClick.AddListener(ShowMainPanel);
        }
    }

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
        }
    }

    // ------------------------------------------------------------------
    // Construção automática da interface de Pausa + Controles
    // ------------------------------------------------------------------
    public void EnsureUI()
    {
        if (rootGroup != null && mainPanel != null && controlsPanel != null)
        {
            var firstKey = controlsPanel.Find("Linha_0/Tecla")?.GetComponent<Text>();
            if (mainPanel.sizeDelta.x >= 880f && firstKey != null && firstKey.text == "TECLAS  A  /  D") return;
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);
            mainPanel = null;
            controlsPanel = null;
        }

        uiFont = LoadBestFont();

        rootCanvas = GetComponent<Canvas>();
        if (rootCanvas == null) rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 180;

        var scaler = GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        rootGroup = GetComponent<CanvasGroup>();
        if (rootGroup == null) rootGroup = gameObject.AddComponent<CanvasGroup>();

        // Fundo escurecido
        var backdrop = NewRect("FundoEscuro", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        backdrop.offsetMin = backdrop.offsetMax = Vector2.zero;
        var backdropImg = backdrop.gameObject.AddComponent<Image>();
        backdropImg.color = new Color(0.03f, 0.04f, 0.09f, 0.88f);
        backdropImg.raycastTarget = true;

        BuildMainPausePanel();
        BuildControlsSubPanel();
        controlsPanel.gameObject.SetActive(false);
    }

    private void BuildMainPausePanel()
    {
        mainPanel = NewRect("PainelPrincipal", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 760f));
        var outerBorder = mainPanel.gameObject.AddComponent<Image>();
        outerBorder.color = new Color(0.35f, 0.82f, 1f, 0.45f);

        var inner = NewRect("FundoCartao", mainPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(4f, 4f);
        inner.offsetMax = new Vector2(-4f, -4f);
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(0.05f, 0.06f, 0.12f, 0.96f);

        // Ícones decorativos de Fogo e Gelo bem afastados do título para não encostar nas letras
        CreateElementMedal("MedalhaoFogo", mainPanel, new Vector2(-345f, 275f), HudSprites.Flame, FireColor);
        CreateElementMedal("MedalhaoGelo", mainPanel, new Vector2(345f, 275f), HudSprites.Ice, IceColor);

        titleText = NewText("TituloPausa", mainPanel, "JOGO PAUSADO", 64, FireColor, useOutline: false);
        SetRect(titleText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 275f), new Vector2(560f, 90f));
        AddShadow(titleText.gameObject, new Color(0f, 0.04f, 0.12f, 0.95f), new Vector2(5f, -5f));

        var sub = NewText("SubtituloPausa", mainPanel, "PRESSIONE ESC OU P PARA RETORNAR", 30, new Color(0.92f, 0.96f, 1f, 0.95f), useOutline: false);
        SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(780f, 44f));
        AddShadow(sub.gameObject, new Color(0f, 0f, 0f, 0.95f), new Vector2(2f, -2f));

        resumeButton = NewButton("BotaoContinuar", mainPanel, "CONTINUAR", new Vector2(0f, 95f), IceColor);
        controlsButton = NewButton("BotaoControles", mainPanel, "CONTROLES", new Vector2(0f, -20f), GoldColor);
        restartButton = NewButton("BotaoReiniciar", mainPanel, "REINICIAR FASE", new Vector2(0f, -135f), FireColor);
        menuButton = NewButton("BotaoMenu", mainPanel, "MENU INICIAL", new Vector2(0f, -250f), IceColor);

        SetupNavigation(resumeButton, controlsButton, restartButton, menuButton);
    }

    private void BuildControlsSubPanel()
    {
        controlsPanel = NewRect("PainelControles", transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180f, 890f));
        var outerBorder = controlsPanel.gameObject.AddComponent<Image>();
        outerBorder.color = new Color(1f, 0.84f, 0.28f, 0.55f);

        var inner = NewRect("FundoControles", controlsPanel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(4f, 4f);
        inner.offsetMax = new Vector2(-4f, -4f);
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(0.05f, 0.06f, 0.12f, 0.97f);

        var header = NewText("TituloControles", controlsPanel, "COMANDOS DO CAVALEIRO", 58, GoldColor, useOutline: false);
        SetRect(header.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 365f), new Vector2(1040f, 80f));
        AddShadow(header.gameObject, new Color(0f, 0f, 0f, 0.95f), new Vector2(5f, -5f));

        string[,] bindings =
        {
            { "MOVER CAVALEIRO", "TECLAS  A  /  D" },
            { "PULAR", "ESPACO" },
            { "ATAQUE LEVE (COMBO)", "CLIQUE ESQUERDO" },
            { "ATAQUE PESADO", "CLIQUE DIREITO" },
            { "ATAQUE ESPECIAL", "TECLA F   OU   E" },
            { "ROLAMENTO / ESQUIVA", "SHIFT   OU   TECLA K" },
            { "BLOQUEIO / PARRY", "TECLA Q   OU   MEIO DO MOUSE" },
            { "TROCAR FOGO / GELO", "TAB   OU   TECLA C" },
            { "ABRIR BAU / INTERAGIR", "TECLA W" },
            { "PAUSAR / RETOMAR JOGO", "ESC   OU   TECLA P" }
        };

        float startY = 265f;
        float rowStep = 54f;
        for (int i = 0; i < bindings.GetLength(0); i++)
        {
            float y = startY - i * rowStep;
            var row = NewRect($"Linha_{i}", controlsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(1060f, 46f));
            var rowBg = row.gameObject.AddComponent<Image>();
            rowBg.color = (i % 2 == 0)
                ? new Color(0.09f, 0.12f, 0.20f, 0.85f)
                : new Color(0.06f, 0.08f, 0.14f, 0.65f);
            rowBg.raycastTarget = false;

            var actionTxt = NewText("Acao", row, bindings[i, 0], 32, Color.white, useOutline: false);
            actionTxt.alignment = TextAnchor.MiddleLeft;
            actionTxt.rectTransform.anchorMin = new Vector2(0f, 0f);
            actionTxt.rectTransform.anchorMax = new Vector2(0.52f, 1f);
            actionTxt.rectTransform.offsetMin = new Vector2(26f, 0f);
            actionTxt.rectTransform.offsetMax = Vector2.zero;
            AddShadow(actionTxt.gameObject, new Color(0f, 0f, 0f, 0.95f), new Vector2(2f, -2f));

            Color keyColor = (i == 7) ? IceColor : ((i == 2 || i == 3 || i == 4) ? new Color(1f, 0.52f, 0.22f) : GoldColor);
            var keyTxt = NewText("Tecla", row, bindings[i, 1], 32, keyColor, useOutline: false);
            keyTxt.alignment = TextAnchor.MiddleRight;
            keyTxt.rectTransform.anchorMin = new Vector2(0.52f, 0f);
            keyTxt.rectTransform.anchorMax = new Vector2(1f, 1f);
            keyTxt.rectTransform.offsetMin = Vector2.zero;
            keyTxt.rectTransform.offsetMax = new Vector2(-26f, 0f);
            AddShadow(keyTxt.gameObject, new Color(0f, 0f, 0f, 0.95f), new Vector2(2f, -2f));
        }

        backFromControlsButton = NewButton("BotaoVoltarControles", controlsPanel, "VOLTAR", new Vector2(0f, -340f), IceColor);
    }

    private void CreateElementMedal(string name, Transform parent, Vector2 pos, Sprite iconSprite, Color accent)
    {
        var medal = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(68f, 68f));
        var frame = medal.gameObject.AddComponent<Image>();
        frame.sprite = HudSprites.Disc;
        frame.color = Color.Lerp(new Color(0.08f, 0.08f, 0.14f, 0.95f), accent, 0.35f);
        frame.raycastTarget = false;

        var ring = NewRect("Anel", medal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(68f, 68f));
        var ringImg = ring.gameObject.AddComponent<Image>();
        ringImg.sprite = HudSprites.Ring;
        ringImg.color = new Color(accent.r, accent.g, accent.b, 0.8f);
        ringImg.raycastTarget = false;

        var icon = NewRect("Icone", medal, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));
        var iconImg = icon.gameObject.AddComponent<Image>();
        iconImg.sprite = iconSprite;
        iconImg.preserveAspect = true;
        iconImg.raycastTarget = false;
    }

    private Button NewButton(string name, Transform parent, string label, Vector2 pos, Color accent)
    {
        var border = NewRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(560f, 88f));
        var borderImg = border.gameObject.AddComponent<Image>();
        borderImg.color = new Color(accent.r, accent.g, accent.b, 0.35f);

        var inner = NewRect("Fundo", border, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        inner.offsetMin = new Vector2(4f, 4f);
        inner.offsetMax = new Vector2(-4f, -4f);
        var innerImg = inner.gameObject.AddComponent<Image>();
        innerImg.color = new Color(accent.r * 0.12f, accent.g * 0.12f, accent.b * 0.12f, 0.94f);

        var text = NewText("Texto", border, label, 44, accent);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

        var btn = border.gameObject.AddComponent<Button>();
        btn.targetGraphic = innerImg;
        btn.transition = Selectable.Transition.ColorTint;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.45f, 1.45f, 1.45f, 1f);
        cb.selectedColor = new Color(1.45f, 1.45f, 1.45f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        btn.colors = cb;

        var fx = border.gameObject.AddComponent<MenuButtonFx>();
        fx.border = borderImg;
        fx.accent = accent;
        return btn;
    }

    private static void SetupNavigation(params Button[] buttons)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnDown = buttons[(i + 1) % buttons.Length],
                selectOnUp = buttons[(i + buttons.Length - 1) % buttons.Length]
            };
        }
    }

    private static Font LoadBestFont()
    {
#if UNITY_EDITOR
        var boldPixels = UnityEditor.AssetDatabase.LoadAssetAtPath<Font>("Assets/BoldPixels/Assets/font/BoldPixels.ttf");
        if (boldPixels != null) return boldPixels;
#endif
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private static void AddShadow(GameObject go, Color c, Vector2? distance = null)
    {
        var s = go.AddComponent<Shadow>();
        s.effectColor = c;
        s.effectDistance = distance ?? new Vector2(6f, -6f);
    }

    private static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        SetRect(rt, aMin, aMax, pos, size);
        return rt;
    }

    private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private Text NewText(string name, Transform parent, string content, int size, Color color, bool useOutline = true)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content;
        t.fontSize = size;
        // BoldPixels já é uma fonte negrito; aplicar FontStyle.Bold duplica os traços e deixa as letras turvas
        t.fontStyle = FontStyle.Normal;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        if (useOutline)
            go.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.9f);
        return t;
    }
}
