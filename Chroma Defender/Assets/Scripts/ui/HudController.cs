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

    private static readonly Color FireColor = new Color(1f, 0.45f, 0.1f);
    private static readonly Color IceColor = new Color(0.35f, 0.82f, 1f);

    private PlayerBehavior player;
    private ElementPolarity current = ElementPolarity.Fire;
    private float pop; // 1 -> 0 logo após trocar, para o efeito de "pulo"

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
    }

    private void OnDestroy()
    {
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
        ApplyVisuals(Time.unscaledDeltaTime * 14f);
    }

    private void ApplyVisuals(float lerp)
    {
        bool fire = current == ElementPolarity.Fire;
        float overshoot = 1f + 0.3f * Mathf.Sin(pop * Mathf.PI);   // pulinho ao trocar
        UpdateSlot(fireSlot, fireFrame, fireIcon, fire, FireColor, overshoot, lerp);
        UpdateSlot(iceSlot, iceFrame, iceIcon, !fire, IceColor, overshoot, lerp);

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
