using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

/// <summary>
/// Baú de melhoria. O jogador chega perto e aperta W / ↑ (ou só encosta, se "openOnTouch" estiver ligado).
/// O baú toca a animação de abrir e, no fim dela, melhora uma habilidade do cavaleiro.
///
/// ESCALÁVEL — cada tipo de baú cuida de uma habilidade e dá o PRÓXIMO nível dela:
///   Madeira → Combo Leve      (2 → 3 → 4 golpes)
///   Prata   → Ataque Pesado   (bloqueado → 1 → 2 → 3 golpes)
///   Ouro    → Ataque Especial (bloqueado → liberado)
/// Com "Nível" = 0 o baú dá sempre o próximo nível (a ordem dos baús na fase não importa).
/// Com "Nível" > 0 ele dá exatamente aquele nível. Se a habilidade já estiver nesse nível, o baú cura a vida.
/// Prefabs prontos em Assets/Prefabs/Baus (menu Chroma Defender > Baús).
///
/// Usa o Animator dos baús do pacote Cainos (parâmetro bool "IsOpened", estados Closed → Open → Opened).
/// Fica registrado em GameProgress: depois de aberto, continua aberto ao morrer, ao trocar de cena e ao voltar.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class UpgradeChest : MonoBehaviour
{
    public enum Melhoria { ComboLeve, AtaquePesado, Especial }

    [Header("--- Identificação ---")]
    [Tooltip("Id único deste baú. Vazio = gerado pela cena + posição (cada baú colocado conta como um baú diferente).")]
    [SerializeField] private string chestId = "";

    [Header("--- Recompensa ---")]
    [SerializeField] private Melhoria melhoria = Melhoria.ComboLeve;
    [Tooltip("0 = próximo nível (escalável). Combo Leve: 3 ou 4 golpes. Ataque Pesado: 1 a 3 golpes. Especial: 1.")]
    [FormerlySerializedAs("lightComboHits")]
    [Range(0, 4)]
    [SerializeField] private int nivel = 0;
    [Tooltip("Vazio = texto automático conforme a melhoria e o nível.")]
    [SerializeField] private string rewardTitle = "";
    [SerializeField] private string rewardDescription = "";

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

    private string FlagId => "bau_aberto_" + (string.IsNullOrEmpty(chestId) ? AutoId() : chestId);

    private string AutoId()
    {
        Vector3 p = transform.position;
        return $"{gameObject.scene.name}_{Mathf.RoundToInt(p.x * 10f)}_{Mathf.RoundToInt(p.y * 10f)}";
    }

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

        int current = CurrentLevel();
        int max = MaxLevel();
        int target = nivel > 0 ? Mathf.Min(nivel, max) : current + 1;

        Vector3 top = transform.position + Vector3.up * 0.7f;
        FireVFX.SpawnFlameBurst(top, 22);

        if (target <= current || target > max)
        {
            // Habilidade já no nível deste baú (ou no máximo): vira um baú de cura.
            if (player != null) player.Heal(player.MaxHealth);
            ProgressHud.Toast($"Baú aberto! <color=#FFD54A>Elixir Restaurador</color>\nVocê já domina {SkillName()}. Vida restaurada!", 4.5f);
            return;
        }

        switch (melhoria)
        {
            case Melhoria.ComboLeve:
                GameProgress.UnlockLightCombo(target);
                if (player != null) player.UpgradeLightCombo(target);
                break;
            case Melhoria.AtaquePesado:
                GameProgress.UnlockHeavy(target);
                if (player != null) player.UnlockHeavyAttack(target);
                break;
            case Melhoria.Especial:
                GameProgress.UnlockWeaponArt();
                if (player != null) player.UnlockWeaponArt();
                break;
        }

        string title = string.IsNullOrEmpty(rewardTitle) ? AutoTitle(target) : rewardTitle;
        string desc = string.IsNullOrEmpty(rewardDescription) ? AutoDescription(target) : rewardDescription;
        ProgressHud.Toast($"Baú aberto! <color=#FFD54A>{title}</color>\n{desc}", 4.5f);
    }

    private int CurrentLevel()
    {
        switch (melhoria)
        {
            case Melhoria.ComboLeve:
                return Mathf.Max(2, GameProgress.LightComboHits, player != null ? player.UnlockedLightHits : 0);
            case Melhoria.AtaquePesado:
                return Mathf.Max(GameProgress.HeavyHits, player != null ? player.UnlockedHeavyHits : 0);
            default:
                return GameProgress.WeaponArt || (player != null && player.WeaponArtUnlocked) ? 1 : 0;
        }
    }

    private int MaxLevel() => melhoria == Melhoria.ComboLeve ? 4 : melhoria == Melhoria.AtaquePesado ? 3 : 1;

    private string SkillName() =>
        melhoria == Melhoria.ComboLeve ? "o Combo Leve" : melhoria == Melhoria.AtaquePesado ? "o Ataque Pesado" : "o Ataque Especial";

    private string AutoTitle(int level)
    {
        switch (melhoria)
        {
            case Melhoria.ComboLeve: return $"Combo Leve — {level}º golpe";
            case Melhoria.AtaquePesado: return level == 1 ? "Ataque Pesado" : $"Ataque Pesado — {level}º golpe";
            default: return "Ataque Especial";
        }
    }

    private string AutoDescription(int level)
    {
        switch (melhoria)
        {
            case Melhoria.ComboLeve: return $"Seu ataque rápido agora encadeia {level} golpes!";
            case Melhoria.AtaquePesado:
                return level == 1 ? "Botão direito do mouse: golpe pesado liberado!" : $"Seu ataque pesado agora encadeia {level} golpes!";
            default: return "Tecla F ou E: tempestade de cortes liberada!";
        }
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

    private void OnGUI()
    {
        if (opened || opening || openOnTouch || !playerInRange) return;
        var cam = Camera.main;
        if (cam == null) return;

        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * promptHeight);
        if (screen.z < 0f) return;

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };
        }

        var rect = new Rect(screen.x - 80f, Screen.height - screen.y - 14f, 160f, 28f);
        // Sombra para ler em qualquer fundo
        promptStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), promptText, promptStyle);
        promptStyle.normal.textColor = new Color(1f, 0.84f, 0.3f);
        GUI.Label(rect, promptText, promptStyle);
    }
}
