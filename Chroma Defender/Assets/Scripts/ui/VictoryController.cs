using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Tela de Vitória. Fica invisível até alguém chamar VictoryController.Trigger()
/// (o VictoryTrigger faz isso quando o jogador toca nele). Pausa o jogo e mostra
/// os botões Jogar de novo / Menu.
/// Montada por Chroma Defender > Criar Tela de Vitória.
/// </summary>
public class VictoryController : MonoBehaviour
{
    [SerializeField] private string menuScene = "MainMenu";
    [Tooltip("Deixe vazio para ir para a próxima cena da lista do Build.")]
    [SerializeField] private string nextScene = "";
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Text title;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button menuButton;

    private static VictoryController instance;
    private bool showing;
    private float shownAt;

    /// <summary>Mostra a tela de vitória (se ela existir na cena).</summary>
    public static void Trigger()
    {
        if (instance != null) instance.Show();
        else Debug.LogWarning("[Vitória] Nenhuma tela de vitória na cena. Use Chroma Defender > Criar Tela de Vitória.");
    }

    private void Awake()
    {
        instance = this;
        SetVisible(false);
    }

    private void Start()
    {
        if (nextButton != null)
        {
            if (HasNextScene()) nextButton.onClick.AddListener(NextLevel);
            else
            {
                // Ainda não existe a próxima fase: botão fica desativado
                nextButton.interactable = false;
                var label = nextButton.GetComponentInChildren<Text>();
                if (label != null) label.text = "EM BREVE";
            }
        }
        if (replayButton != null) replayButton.onClick.AddListener(Replay);
        if (menuButton != null) menuButton.onClick.AddListener(GoMenu);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        Time.timeScale = 1f;
    }

    private void SetVisible(bool v)
    {
        if (group == null) return;
        group.blocksRaycasts = v;
        group.interactable = v;
        if (!v) group.alpha = 0f;
    }

    private void Show()
    {
        if (showing) return;
        showing = true;
        shownAt = Time.unscaledTime;
        SetVisible(true);
        Time.timeScale = 0f;
        GameAudioManager.EnsureInstance()?.PlayVictory();
        var first = (nextButton != null && nextButton.interactable) ? nextButton : replayButton;
        if (first != null) EventSystem.current?.SetSelectedGameObject(first.gameObject);
    }

    private bool HasNextScene()
    {
        if (!string.IsNullOrEmpty(nextScene)) return Application.CanStreamedLevelBeLoaded(nextScene);
        return SceneManager.GetActiveScene().buildIndex + 1 < SceneManager.sceneCountInBuildSettings;
    }

    private void NextLevel()
    {
        Time.timeScale = 1f;
        if (!string.IsNullOrEmpty(nextScene)) SceneManager.LoadScene(nextScene);
        else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void Replay()
    {
        Time.timeScale = 1f;
        GameProgress.ResetProgress();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }

    private void Update()
    {
        if (!showing || group == null) return;

        float t = Time.unscaledTime - shownAt;
        group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.unscaledDeltaTime * 3f);

        if (title != null)
        {
            float k = Mathf.Clamp01(t / 0.4f);
            float s = Mathf.Lerp(1.6f, 1f, 1f - (1f - k) * (1f - k));
            title.rectTransform.localScale = Vector3.one * s;
            // Alterna Fogo/Gelo devagar no título
            title.color = Color.Lerp(new Color(1f, 0.35f, 0.1f), new Color(0.35f, 0.82f, 1f), (Mathf.Sin(t * 1.5f) + 1f) * 0.5f);
        }
    }
}
