using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tela de Game Over. Aparece quando o jogador morre (PlayerBehavior.OnPlayerDeath),
/// mostra uma barra "Renascendo..." e some quando ele volta (OnPlayerRespawn).
/// A caveira e a cor mudam conforme a polaridade (Fogo = vermelho, Gelo = ciano).
/// </summary>
public class GameOverController : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Image tint;
    [SerializeField] private RawImage skull;
    [SerializeField] private Text title;
    [SerializeField] private Text subtitle;
    [SerializeField] private RectTransform progressFill;
    [SerializeField] private Texture2D fireSkull;
    [SerializeField] private Texture2D iceSkull;

    private static readonly Color FireColor = new Color(1f, 0.35f, 0.1f);
    private static readonly Color IceColor = new Color(0.35f, 0.82f, 1f);

    private PlayerBehavior player;
    private bool showing;
    private float shownAt;
    private float duration = 1.2f;

    private void Awake()
    {
        if (fireSkull != null) fireSkull.filterMode = FilterMode.Point;
        if (iceSkull != null) iceSkull.filterMode = FilterMode.Point;
        if (group != null)
        {
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    private void Start()
    {
        player = FindFirstObjectByType<PlayerBehavior>();
        if (player == null) return;

        // Lê o tempo de renascimento do jogador (campo privado), para a barra bater certinho.
        var field = typeof(PlayerBehavior).GetField("deathRespawnDelay", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null && field.GetValue(player) is float f && f > 0.05f) duration = f;

        player.OnPlayerDeath += Show;
        player.OnPlayerRespawn += Hide;
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.OnPlayerDeath -= Show;
        player.OnPlayerRespawn -= Hide;
    }

    private void Show()
    {
        bool fire = player == null || player.CurrentPolarity == ElementPolarity.Fire;
        Color c = fire ? FireColor : IceColor;

        if (skull != null) skull.texture = fire ? fireSkull : iceSkull;
        if (tint != null) tint.color = new Color(c.r * 0.18f, c.g * 0.18f, c.b * 0.18f, 0.88f);
        if (title != null) title.color = c;
        if (progressFill != null) progressFill.GetComponent<Image>().color = c;

        showing = true;
        shownAt = Time.unscaledTime;
    }

    private void Hide() => showing = false;

    private void Update()
    {
        if (group == null) return;

        float target = showing ? 1f : 0f;
        float speed = showing ? 5f : 4f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime * speed);

        if (!showing) return;

        float t = Time.unscaledTime - shownAt;

        // Título "bate" na tela e assenta
        if (title != null)
        {
            float k = Mathf.Clamp01(t / 0.3f);
            float s = Mathf.Lerp(1.6f, 1f, 1f - (1f - k) * (1f - k));
            title.rectTransform.localScale = Vector3.one * s;
        }
        // Caveira flutuando
        if (skull != null)
            skull.rectTransform.anchoredPosition = new Vector2(0f, 90f + Mathf.Sin(t * 5f) * 6f);

        // Barra "Renascendo..."
        if (progressFill != null)
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(t / duration), 1f);
        if (subtitle != null)
        {
            int dots = 1 + (int)(t * 4f) % 3;
            subtitle.text = "RENASCENDO" + new string('.', dots);
        }
    }
}
