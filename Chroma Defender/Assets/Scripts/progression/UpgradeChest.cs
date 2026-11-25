using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Baú de melhoria. O jogador chega perto e aperta W / ↑ (ou só encosta, se "openOnTouch" estiver ligado).
/// O baú toca a animação de abrir e, no fim dela, melhora o combo de ataque leve do cavaleiro.
///
/// Usa o Animator dos baús do pacote Cainos (parâmetro bool "IsOpened", estados Closed → Open → Opened).
/// Fica registrado em GameProgress: depois de aberto, continua aberto ao morrer, ao trocar de cena e ao voltar.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class UpgradeChest : MonoBehaviour
{
    public enum UpgradeKind
    {
        LightCombo = 0,
        HeavyAttack = 1,
        WeaponArt = 2
    }

    [Header("--- Identificação ---")]
    [Tooltip("Id único deste baú. Dois baús com o mesmo id contam como o mesmo baú.")]
    [SerializeField] private string chestId = "bau_combo_leve_3";

    [Header("--- Recompensa ---")]
    [Tooltip("Tipo de técnica liberada por este baú (Combo Leve, Ataque Pesado ou Ataque Especial/Supremo).")]
    [SerializeField] private UpgradeKind upgradeKind = UpgradeKind.LightCombo;
    [Tooltip("Quantidade de golpes do combo de ataque leve liberada (3 = terceiro golpe, 4 = quarto golpe).")]
    [Range(2, 4)]
    [SerializeField] private int lightComboHits = 3;
    [Tooltip("Quantidade de golpes do ataque pesado liberada (1 a 3) quando Upgrade Kind = HeavyAttack.")]
    [Range(1, 3)]
    [SerializeField] private int heavyComboHits = 1;
    [SerializeField] private string rewardTitle = "Combo Leve — 3º golpe";
    [SerializeField] private string rewardDescription = "Seu ataque rápido agora encadeia 3 golpes!";

    [Header("--- Interação ---")]
    [Tooltip("Abre sozinho ao encostar, sem apertar nada.")]
    [SerializeField] private bool openOnTouch = false;
    [SerializeField] private string promptText = "[W] Abrir baú";
    [Tooltip("Altura do aviso \"Abrir baú\" acima da base do baú.")]
    [SerializeField] private float promptHeight = 1.6f;

    [Header("--- Referências ---")]
    [SerializeField] private Animator animator;

    private const string OpenedParam = "IsOpened";
    private const string OpenedState = "Opened";

    private bool playerInRange;
    private bool opening;
    private bool opened;
    private PlayerBehavior player;
    private GUIStyle promptStyle;

    private string FlagId => "bau_aberto_" + chestId;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (GameProgress.HasFlag(FlagId))
        {
            // Já aberto antes (outra vida ou outra visita à cena): fica aberto e sem interação.
            opened = true;
            if (animator != null)
            {
                animator.SetBool(OpenedParam, true);
                animator.Play(OpenedState, 0, 1f);
            }
        }
    }

    private void Update()
    {
        if (opened || opening || !playerInRange) return;

        var kb = Keyboard.current;
        bool pressed = kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame);
        if (openOnTouch || pressed) StartCoroutine(OpenRoutine());
    }

    private IEnumerator OpenRoutine()
    {
        opening = true;
        GameProgress.SetFlag(FlagId);

        if (animator != null) animator.SetBool(OpenedParam, true);
        yield return new WaitForSeconds(OpenClipLength());

        GiveReward();
        opening = false;
        opened = true;
    }

    private void GiveReward()
    {
        if (player == null) player = FindAnyObjectByType<PlayerBehavior>();

        switch (upgradeKind)
        {
            case UpgradeKind.HeavyAttack:
                GameProgress.UnlockHeavyCombo(heavyComboHits);
                if (player != null) player.UnlockHeavyAttack(heavyComboHits);
                break;

            case UpgradeKind.WeaponArt:
                GameProgress.UnlockWeaponArt();
                if (player != null) player.UnlockWeaponArt();
                break;

            default:
                GameProgress.UnlockLightCombo(lightComboHits);
                if (player != null && player.UnlockedLightHits < lightComboHits)
                    player.UpgradeLightCombo(lightComboHits);
                break;
        }

        Vector3 top = transform.position + Vector3.up * 0.7f;
        FireVFX.SpawnFlameBurst(top, 22);
        ProgressHud.Toast($"Baú aberto! <color=#FFD54A>{rewardTitle}</color>\n{rewardDescription}", 4.5f);
    }

    private float OpenClipLength()
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip != null && clip.name.EndsWith("- Open")) return clip.length;
        }
        return 0.5f;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerBehavior>();
        if (p == null) return;
        player = p;
        playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerBehavior>() != null) playerInRange = false;
    }

    private Texture2D promptBgTex;

    private void OnGUI()
    {
        if (opened || opening || openOnTouch || !playerInRange) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * promptHeight);
        if (screen.z < 0f) return;

        float scale = Mathf.Max(0.75f, Screen.height / 720f);
        if (promptBgTex == null)
        {
            promptBgTex = new Texture2D(1, 1);
            promptBgTex.SetPixel(0, 0, Color.white);
            promptBgTex.Apply();
        }

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
        }
        promptStyle.fontSize = Mathf.RoundToInt(17 * scale);

        float w = 176f * scale;
        float h = 32f * scale;
        var rect = new Rect(screen.x - w * 0.5f, Screen.height - screen.y - h * 0.5f, w, h);

        // Moldura dourada + fundo escuro
        Color prev = GUI.color;
        GUI.color = new Color(1f, 0.82f, 0.28f, 0.85f);
        GUI.DrawTexture(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f), promptBgTex);
        GUI.color = new Color(0.05f, 0.05f, 0.09f, 0.92f);
        GUI.DrawTexture(rect, promptBgTex);
        GUI.color = prev;

        // Sombra + texto dourado
        promptStyle.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), promptText, promptStyle);
        promptStyle.normal.textColor = new Color(1f, 0.86f, 0.32f);
        GUI.Label(rect, promptText, promptStyle);
    }
}
