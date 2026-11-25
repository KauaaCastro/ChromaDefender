using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HUD do Chroma Defender: vida e polaridade (Fogo/Gelo).
/// Liga-se sozinho ao PlayerBehavior da cena. Também pode ser chamado por outros scripts:
///   HudController.Instance.SetCrystalHealth(80, 100);
///   HudController.Instance.SetElement(ElementPolarity.Ice);
/// </summary>
public class HudController : MonoBehaviour
{
    public static HudController Instance { get; private set; }

    [Header("Vida")]
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private Text healthText;

    [Header("Polaridade: slot de Fogo")]
    [SerializeField] private RectTransform fireSlot;
    [SerializeField] private Image fireFrame;
    [SerializeField] private Image fireIcon;

    [Header("Polaridade: slot de Gelo")]
    [SerializeField] private RectTransform iceSlot;
    [SerializeField] private Image iceFrame;
    [SerializeField] private Image iceIcon;

    [Header("Polaridade: brilho e legenda")]
    [SerializeField] private Image glow;
    [SerializeField] private Text elementText;

    [Header("Técnica / Combo Leve")]
    [SerializeField] private RectTransform comboPanel;
    [SerializeField] private Image comboBorder;
    [SerializeField] private Image comboIcon;
    [SerializeField] private Text comboText;
    [SerializeField] private Image[] comboPips;

    private static readonly Color FireColor = new Color(1f, 0.45f, 0.1f);
    private static readonly Color IceColor = new Color(0.35f, 0.82f, 1f);
    private static readonly Color GoldColor = new Color(1f, 0.84f, 0.28f);

    private PlayerBehavior player;
    private ElementPolarity current = ElementPolarity.Fire;
    private float pop; // 1 -> 0 logo após trocar, para o efeito de "pulo"
    private float comboPop;
    private int lastComboHits = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Sprites gerados por código (pixel art)
        if (fireIcon != null) fireIcon.sprite = HudSprites.Flame;
        if (iceIcon != null) iceIcon.sprite = HudSprites.Ice;
        if (fireFrame != null) fireFrame.sprite = HudSprites.Disc;
        if (iceFrame != null) iceFrame.sprite = HudSprites.Disc;
        if (glow != null) glow.sprite = HudSprites.Ring;

        EnsureComboPanel();
        if (comboIcon != null) comboIcon.sprite = HudSprites.Sword;
    }

    private void Start()
    {
        SetCrystalHealth(100, 100);
        SetElement(ElementPolarity.Fire, false);

        player = FindFirstObjectByType<PlayerBehavior>();
        if (player != null)
        {
            player.OnHealthChanged += SetCrystalHealth;
            player.OnPolarityChanged += OnPolarity;
            SetCrystalHealth(player.CurrentHealth, player.MaxHealth);
            SetElement(player.CurrentPolarity, false);
        }

        GameProgress.OnChanged += RefreshCombo;
        RefreshCombo();
    }

    private void OnDestroy()
    {
        GameProgress.OnChanged -= RefreshCombo;
        if (player != null)
        {
            player.OnHealthChanged -= SetCrystalHealth;
            player.OnPolarityChanged -= OnPolarity;
        }
    }

    private void OnPolarity(ElementPolarity p) => SetElement(p, true);

    public void SetCrystalHealth(int current, int max)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);
        if (healthFill != null)
            healthFill.anchorMax = new Vector2((float)current / max, 1f);
        if (healthText != null)
            healthText.text = $"{current}/{max}";
    }

    public void SetElement(ElementPolarity element) => SetElement(element, true);

    private void SetElement(ElementPolarity element, bool animate)
    {
        current = element;
        if (animate) pop = 1f;
        bool fire = element == ElementPolarity.Fire;
        if (elementText != null)
        {
            elementText.text = fire ? "FOGO" : "GELO";
            elementText.color = fire ? FireColor : IceColor;
        }
        if (!animate) ApplyVisuals(1f);
    }

    private void Update()
    {
        pop = Mathf.MoveTowards(pop, 0f, Time.unscaledDeltaTime * 3.5f);
        comboPop = Mathf.MoveTowards(comboPop, 0f, Time.unscaledDeltaTime * 2.8f);

        int currentHits = GetUnlockedComboHits();
        if (currentHits != lastComboHits) RefreshCombo();

        ApplyVisuals(Time.unscaledDeltaTime * 14f);
    }

    private int GetUnlockedComboHits()
    {
        int hits = 2;
        if (player != null) hits = Mathf.Max(hits, player.UnlockedLightHits);
        if (GameProgress.LightComboHits > hits) hits = GameProgress.LightComboHits;
        return Mathf.Clamp(hits, 2, 4);
    }

    public void RefreshCombo()
    {
        int hits = GetUnlockedComboHits();
        bool upgraded = lastComboHits >= 2 && hits > lastComboHits;
        lastComboHits = hits;
        if (upgraded) comboPop = 1f;

        bool isLevel3Plus = hits >= 3;
        if (comboText != null)
        {
            comboText.text = isLevel3Plus
                ? $"TÉCNICA: COMBO NV. {hits} ★"
                : $"ATAQUE LEVE: {hits} GOLPES";
            comboText.color = isLevel3Plus ? GoldColor : new Color(0.92f, 0.94f, 1f);
        }

        if (comboBorder != null)
        {
            comboBorder.color = isLevel3Plus
                ? new Color(1f, 0.82f, 0.25f, 0.75f)
                : new Color(0.75f, 0.82f, 0.95f, 0.32f);
        }

        if (comboPips != null)
        {
            for (int i = 0; i < comboPips.Length; i++)
            {
                if (comboPips[i] == null) continue;
                bool active = (i + 1) <= hits;
                bool bonusPip = i >= 2 && active;
                comboPips[i].color = !active
                    ? new Color(0.18f, 0.20f, 0.26f, 0.75f)
                    : (bonusPip ? GoldColor : (current == ElementPolarity.Fire ? FireColor : IceColor));
                comboPips[i].rectTransform.localScale = Vector3.one * (active ? (bonusPip ? 1.15f : 1f) : 0.82f);
            }
        }
    }

    private void ApplyVisuals(float lerp)
    {
        bool fire = current == ElementPolarity.Fire;
        float overshoot = 1f + 0.3f * Mathf.Sin(pop * Mathf.PI);   // pulinho ao trocar
        UpdateSlot(fireSlot, fireFrame, fireIcon, fire, FireColor, overshoot, lerp);
        UpdateSlot(iceSlot, iceFrame, iceIcon, !fire, IceColor, overshoot, lerp);

        if (comboPanel != null)
        {
            float s = 1f + 0.22f * Mathf.Sin(comboPop * Mathf.PI);
            comboPanel.localScale = Vector3.Lerp(comboPanel.localScale, Vector3.one * s, Mathf.Clamp01(lerp));
        }

        if (comboPips != null)
        {
            Color elemCol = fire ? FireColor : IceColor;
            for (int i = 0; i < comboPips.Length && i < 2; i++)
            {
                if (comboPips[i] != null && (i + 1) <= lastComboHits)
                    comboPips[i].color = Color.Lerp(comboPips[i].color, elemCol, Mathf.Clamp01(lerp));
            }
        }

        if (glow != null)
        {
            var target = fire ? FireColor : IceColor;
            float pulse = 0.55f + 0.25f * Mathf.Sin(Time.unscaledTime * 4f);
            target.a = pulse;
            glow.color = Color.Lerp(glow.color, target, lerp);
            var rt = glow.rectTransform;
            var slot = fire ? fireSlot : iceSlot;
            if (slot != null)
            {
                rt.position = Vector3.Lerp(rt.position, slot.position, lerp);
                rt.localScale = Vector3.one * (1.35f + 0.05f * Mathf.Sin(Time.unscaledTime * 4f));
            }
        }
    }

    private void EnsureComboPanel()
    {
        if (comboPanel != null) return;

        Font font = (healthText != null && healthText.font != null)
            ? healthText.font
            : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Cria o painel de Técnica/Combo abaixo do rótulo VIDA no canto superior esquerdo
        var borderGo = new GameObject("PainelTecnica", typeof(RectTransform), typeof(Image));
        borderGo.transform.SetParent(transform, false);
        comboPanel = borderGo.GetComponent<RectTransform>();
        comboPanel.anchorMin = comboPanel.anchorMax = new Vector2(0f, 1f);
        comboPanel.pivot = new Vector2(0f, 1f);
        comboPanel.anchoredPosition = new Vector2(30f, -116f);
        comboPanel.sizeDelta = new Vector2(390f, 44f);
        comboBorder = borderGo.GetComponent<Image>();
        comboBorder.color = new Color(0.75f, 0.82f, 0.95f, 0.32f);
        comboBorder.raycastTarget = false;

        var innerGo = new GameObject("Fundo", typeof(RectTransform), typeof(Image));
        innerGo.transform.SetParent(comboPanel, false);
        var innerRt = innerGo.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(2f, 2f);
        innerRt.offsetMax = new Vector2(-2f, -2f);
        var innerImg = innerGo.GetComponent<Image>();
        innerImg.color = new Color(0.04f, 0.04f, 0.08f, 0.88f);
        innerImg.raycastTarget = false;

        // Ícone de espada à esquerda
        var iconGo = new GameObject("IconeEspada", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(comboPanel, false);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(24f, 0f);
        iconRt.sizeDelta = new Vector2(28f, 28f);
        comboIcon = iconGo.GetComponent<Image>();
        comboIcon.sprite = HudSprites.Sword;
        comboIcon.preserveAspect = true;
        comboIcon.raycastTarget = false;

        // Texto da técnica
        var textGo = new GameObject("TextoTecnica", typeof(RectTransform), typeof(Text), typeof(Outline));
        textGo.transform.SetParent(comboPanel, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(46f, 0f);
        textRt.offsetMax = new Vector2(-96f, 0f);
        comboText = textGo.GetComponent<Text>();
        comboText.font = font;
        comboText.fontSize = 18;
        comboText.fontStyle = FontStyle.Bold;
        comboText.alignment = TextAnchor.MiddleLeft;
        comboText.color = new Color(0.92f, 0.94f, 1f);
        comboText.raycastTarget = false;
        textGo.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.85f);

        // 3 marcadores visuais de golpes do combo (Pips I, II, III)
        comboPips = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            var pipGo = new GameObject($"Golpe_{i + 1}", typeof(RectTransform), typeof(Image));
            pipGo.transform.SetParent(comboPanel, false);
            var pipRt = pipGo.GetComponent<RectTransform>();
            pipRt.anchorMin = pipRt.anchorMax = new Vector2(1f, 0.5f);
            pipRt.pivot = new Vector2(0.5f, 0.5f);
            pipRt.anchoredPosition = new Vector2(-72f + i * 26f, 0f);
            pipRt.sizeDelta = new Vector2(18f, 18f);
            var img = pipGo.GetComponent<Image>();
            img.sprite = HudSprites.Disc;
            img.raycastTarget = false;
            comboPips[i] = img;
        }
    }

    private static void UpdateSlot(RectTransform slot, Image frame, Image icon, bool active, Color color, float overshoot, float lerp)
    {
        if (slot == null) return;
        float s = active ? 1.15f * overshoot : 0.7f;
        slot.localScale = Vector3.Lerp(slot.localScale, Vector3.one * s, Mathf.Clamp01(lerp));
        if (frame != null)
        {
            var f = active ? Color.Lerp(new Color(0.08f, 0.08f, 0.12f), color, 0.35f) : new Color(0.08f, 0.08f, 0.12f);
            f.a = active ? 0.95f : 0.7f;
            frame.color = Color.Lerp(frame.color, f, Mathf.Clamp01(lerp));
        }
        if (icon != null)
        {
            var c = active ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.55f);
            icon.color = Color.Lerp(icon.color, c, Mathf.Clamp01(lerp));
        }
    }
}
