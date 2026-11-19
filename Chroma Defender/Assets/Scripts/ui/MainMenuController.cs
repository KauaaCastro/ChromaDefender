using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menu inicial. Fundo de floresta com parallax, tÃ­tulo Fogo/Gelo, botÃµes Jogar e Sair.
/// Montado pelo menu Chroma Defender > Criar Menu Inicial.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameScene = "SampleScene";
    [SerializeField] private RawImage[] layers;
    [SerializeField] private float[] layerSpeeds;
    [SerializeField] private float[] layerMouseShift;
    [SerializeField] private RectTransform fireIcon;
    [SerializeField] private RectTransform iceIcon;
    [SerializeField] private RectTransform title;
    [SerializeField] private CanvasGroup fade;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private bool leaving;
    private float startTime;
    private Vector2 fireBase, iceBase;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (layers != null)
            foreach (var l in layers)
                if (l != null && l.texture != null) l.texture.wrapMode = TextureWrapMode.Repeat;
        if (fireIcon != null) fireBase = fireIcon.anchoredPosition;
        if (iceIcon != null) iceBase = iceIcon.anchoredPosition;
    }

    private void Start()
    {
        startTime = Time.unscaledTime;
        if (playButton != null)
        {
            playButton.onClick.AddListener(Play);
            EventSystem.current?.SetSelectedGameObject(playButton.gameObject);
        }
        if (quitButton != null) quitButton.onClick.AddListener(Quit);
#if UNITY_WEBGL
        if (quitButton != null) quitButton.gameObject.SetActive(false);
#endif
        if (fade != null) { fade.alpha = 1f; fade.blocksRaycasts = false; }
    }

    private void Update()
    {
        float t = Time.unscaledTime - startTime;

        // Mouse (-1..1) para um leve parallax
        Vector2 m = Vector2.zero;
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse != null)
            m = new Vector2(mouse.position.ReadValue().x / Screen.width, mouse.position.ReadValue().y / Screen.height) * 2f - Vector2.one;

        if (layers != null)
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i] == null) continue;
                float speed = i < layerSpeeds.Length ? layerSpeeds[i] : 0f;
                float shift = i < layerMouseShift.Length ? layerMouseShift[i] : 0f;
                var r = layers[i].uvRect;
                r.x = t * speed + m.x * shift;
                r.y = m.y * shift * 0.4f;
                layers[i].uvRect = r;
            }

        // TÃ­tulo entra descendo e depois respira
        if (title != null)
        {
            float k = Mathf.Clamp01(t / 0.8f);
            float ease = 1f - (1f - k) * (1f - k) * (1f - k);
            float s = 1f + Mathf.Sin(t * 1.6f) * 0.015f;
            title.localScale = Vector3.one * s * Mathf.Lerp(0.6f, 1f, ease);
        }

        // Ãcones de Fogo e Gelo flutuando em contratempo
        if (fireIcon != null) fireIcon.anchoredPosition = fireBase + new Vector2(0f, Mathf.Sin(t * 2.2f) * 10f);
        if (iceIcon != null) iceIcon.anchoredPosition = iceBase + new Vector2(0f, Mathf.Sin(t * 2.2f + Mathf.PI) * 10f);

        // Fade in / out
        if (fade != null)
        {
            float target = leaving ? 1f : 0f;
            fade.alpha = Mathf.MoveTowards(fade.alpha, target, Time.unscaledDeltaTime * (leaving ? 2.5f : 1.5f));
            if (leaving && fade.alpha >= 1f) SceneManager.LoadScene(gameScene);
        }
    }

    private void Play()
    {
        if (leaving) return;
        leaving = true;
        if (fade != null) fade.blocksRaycasts = true;
        else SceneManager.LoadScene(gameScene);
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

