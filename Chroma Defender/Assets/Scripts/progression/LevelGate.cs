using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Passagem para outra fase (portão do castelo, poço mágico, saída do castelo...).
///
/// Interação (padrão): o cavaleiro chega perto e aperta W / ↑ — igual aos baús.
///  1. Trancado e sem chave  → aviso "Está trancado" + cadeado balança (só de encostar).
///  2. Trancado e COM a chave → [W] Usar chave: cadeado abre, a grade do portão sobe.
///  3. Aberto                 → [W] Entrar: o cavaleiro entra, a tela escurece e a próxima fase carrega.
/// Desligando "requireInteract" volta ao modo antigo (abre/entra só de encostar).
///
/// Depois de aberto, fica aberto para sempre (flag salva em GameProgress).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class LevelGate : MonoBehaviour
{
    [Header("--- Destino ---")]
    [SerializeField] private string targetScene = GameProgress.SceneFase2;
    [SerializeField] private string targetSpawnId = GameProgress.SpawnFase2Entrada;

    [Header("--- Chave (vazio = sempre aberto) ---")]
    [SerializeField] private string requiredKeyId = GameProgress.KeyCastelo;

    [Header("--- Mensagens ---")]
    [SerializeField, TextArea] private string lockedMessage = "Está trancado. Você precisa de uma chave.";
    [SerializeField, TextArea] private string unlockMessage = "O cadeado se abriu!";
    [SerializeField, TextArea] private string enterMessage = "";

    [Header("--- Interação ---")]
    [Tooltip("Exige apertar W / ↑ para destrancar e para entrar. Desligado = só encostar.")]
    [SerializeField] private bool requireInteract = true;
    [Tooltip("Texto do aviso quando o portão está aberto. Vazio = \"[W] Entrar\".")]
    [SerializeField] private string enterPrompt = "";
    [Tooltip("Texto do aviso quando o jogador tem a chave. Vazio = \"[W] Usar <nome da chave>\".")]
    [SerializeField] private string unlockPrompt = "";
    [Tooltip("Altura do aviso acima do centro do portão.")]
    [SerializeField] private float promptHeight = 2.1f;

    [Header("--- Visual do Cadeado ---")]
    [SerializeField] private SpriteRenderer padlockRenderer;
    [SerializeField] private Sprite[] padlockFrames;
    [SerializeField] private float padlockFps = 16f;
    [Tooltip("Objetos escondidos ao destrancar (ex.: grade do portão fechado). Eles SOBEM como uma grade levadiça antes de sumir.")]
    [SerializeField] private GameObject[] hideOnUnlock;
    [Tooltip("Objetos exibidos ao destrancar (ex.: portão aberto, brilho mágico).")]
    [SerializeField] private GameObject[] showOnUnlock;

    [Header("--- Grade levadiça ---")]
    [SerializeField] private float gateRaiseHeight = 2.6f;
    [SerializeField] private float gateRaiseTime = 1.1f;

    [Header("--- Entrada ---")]
    [SerializeField] private float enterDelay = 0.35f;
    [SerializeField] private float fadeOutTime = 0.7f;
    [Tooltip("Ignora o gatilho logo após a cena carregar (evita reentrar ao voltar de outra fase).")]
    [SerializeField] private float armDelay = 1.0f;

    private bool unlocked;
    private bool busy;
    private bool playerInRange;
    private PlayerBehavior player;
    private float nextLockedMsgTime;
    private float armedAt;
    private GUIStyle promptStyle;

    private string UnlockFlag => $"portao_{requiredKeyId}_{targetScene}";
    private bool NeedsKey => !string.IsNullOrEmpty(requiredKeyId);

    public void Configure(string scene, string spawnId, string keyId, string locked, string unlockedMsg, string enterMsg, string prompt = "")
    {
        targetScene = scene;
        targetSpawnId = spawnId;
        requiredKeyId = keyId;
        lockedMessage = locked;
        unlockMessage = unlockedMsg;
        enterMessage = enterMsg;
        enterPrompt = prompt;
    }

    public void ConfigureVisuals(SpriteRenderer padlock, Sprite[] frames, GameObject[] hide, GameObject[] show)
    {
        padlockRenderer = padlock;
        padlockFrames = frames;
        hideOnUnlock = hide;
        showOnUnlock = show;
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        armedAt = Time.time + armDelay;

        bool open = !NeedsKey || GameProgress.HasFlag(UnlockFlag);
        unlocked = open;
        ApplyVisuals(open, instant: true);
    }

    // ---------------------------------------------------------------- Detecção do jogador

    private void OnTriggerEnter2D(Collider2D other) => OnPlayerTouch(other);
    private void OnTriggerStay2D(Collider2D other) => OnPlayerTouch(other);

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerBehavior>() != null) playerInRange = false;
    }

    private void OnPlayerTouch(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerBehavior>();
        if (p == null) return;
        player = p;
        playerInRange = true;

        if (busy || Time.time < armedAt || p.IsDead) return;

        if (!unlocked && !GameProgress.HasKey(requiredKeyId))
        {
            if (Time.time >= nextLockedMsgTime)
            {
                nextLockedMsgTime = Time.time + 3f;
                ProgressHud.Toast(lockedMessage);
                StartCoroutine(ShakePadlock());
            }
            return;
        }

        // Modo antigo: abre e entra só de encostar.
        if (!requireInteract) Use();
    }

    private void Update()
    {
        if (!requireInteract || !playerInRange || busy || Time.time < armedAt) return;
        if (player == null || player.IsDead) return;

        var kb = Keyboard.current;
        bool pressed = kb != null && (kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame);
        if (pressed) Use();
    }

    private void Use()
    {
        if (busy) return;
        if (unlocked) StartCoroutine(EnterRoutine());
        else if (GameProgress.HasKey(requiredKeyId)) StartCoroutine(UnlockRoutine());
    }

    // ---------------------------------------------------------------- Destrancar

    private IEnumerator UnlockRoutine()
    {
        busy = true;

        if (padlockRenderer != null && padlockFrames != null && padlockFrames.Length > 0)
        {
            float frameTime = 1f / Mathf.Max(1f, padlockFps);
            foreach (var f in padlockFrames)
            {
                padlockRenderer.sprite = f;
                yield return new WaitForSeconds(frameTime);
            }
        }

        FireVFX.SpawnFlameBurst(padlockRenderer != null ? padlockRenderer.transform.position : transform.position, 24);
        IceVFX.SpawnFrostBurst(transform.position, 16);

        unlocked = true;
        GameProgress.SetFlag(UnlockFlag);
        if (!string.IsNullOrEmpty(unlockMessage)) ProgressHud.Toast(unlockMessage);
        yield return ApplyVisualsAnimated();

        busy = false;
        // Modo antigo: o jogador ainda está no gatilho → OnTriggerStay2D fará a entrada.
        // Modo com tecla: o aviso muda para "[W] Entrar".
    }

    // ---------------------------------------------------------------- Entrar

    private IEnumerator EnterRoutine()
    {
        busy = true;

        if (!Application.CanStreamedLevelBeLoaded(targetScene))
        {
            ProgressHud.Toast($"A cena \"{targetScene}\" ainda não existe ou não está no Build Settings.\nUse o menu Chroma Defender → Fase 1 → Criar cenas da Fase 2 e 3.");
            yield return new WaitForSeconds(3f);
            busy = false;
            yield break;
        }

        if (!string.IsNullOrEmpty(enterMessage)) ProgressHud.Toast(enterMessage, 2f);

        // O cavaleiro para, caminha até o centro do portão e some na escuridão.
        if (player != null) StartCoroutine(PlayerWalkIn(player));
        yield return new WaitForSeconds(enterDelay);
        yield return ScreenFader.FadeOut(fadeOutTime);

        GameProgress.PendingSpawnId = targetSpawnId;
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetScene);
    }

    private IEnumerator PlayerWalkIn(PlayerBehavior p)
    {
        var rb = p.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        p.enabled = false; // trava os comandos durante a transição

        var renderers = p.GetComponentsInChildren<SpriteRenderer>();
        Vector3 start = p.transform.position;
        Vector3 end = new Vector3(transform.position.x, start.y, start.z);
        float t = 0f;
        const float duration = 0.7f;
        while (t < duration && p != null)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            p.transform.position = new Vector3(Mathf.Lerp(start.x, end.x, k), p.transform.position.y, start.z);
            foreach (var r in renderers)
            {
                if (r == null) continue;
                Color c = r.color;
                r.color = new Color(c.r, c.g, c.b, 1f - k * 0.85f);
            }
            yield return null;
        }
    }

    // ---------------------------------------------------------------- Visual

    private void ApplyVisuals(bool open, bool instant)
    {
        if (padlockRenderer != null)
        {
            if (open && instant) padlockRenderer.gameObject.SetActive(false);
            else if (open) StartCoroutine(FadeOut(padlockRenderer));
            else if (padlockFrames != null && padlockFrames.Length > 0) padlockRenderer.sprite = padlockFrames[0];
        }

        if (hideOnUnlock != null)
            foreach (var go in hideOnUnlock) if (go != null) go.SetActive(!open);
        if (showOnUnlock != null)
            foreach (var go in showOnUnlock) if (go != null) go.SetActive(open);
    }

    /// <summary>Cadeado cai, portão aberto aparece por trás e a grade fechada sobe como uma grade levadiça.</summary>
    private IEnumerator ApplyVisualsAnimated()
    {
        if (padlockRenderer != null) StartCoroutine(FadeOut(padlockRenderer));

        if (showOnUnlock != null)
            foreach (var go in showOnUnlock) if (go != null) go.SetActive(true);

        if (hideOnUnlock == null || hideOnUnlock.Length == 0)
        {
            yield return new WaitForSeconds(0.6f);
            yield break;
        }

        var starts = new Vector3[hideOnUnlock.Length];
        for (int i = 0; i < hideOnUnlock.Length; i++)
        {
            var go = hideOnUnlock[i];
            if (go == null) continue;
            starts[i] = go.transform.position;
            // A grade fica por cima do portão aberto enquanto sobe.
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>()) sr.sortingOrder += 1;
        }

        float t = 0f;
        float duration = Mathf.Max(0.1f, gateRaiseTime);
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = k * k * (3f - 2f * k);
            float shake = (1f - k) * 0.03f * Mathf.Sin(Time.time * 70f);
            for (int i = 0; i < hideOnUnlock.Length; i++)
            {
                var go = hideOnUnlock[i];
                if (go == null) continue;
                go.transform.position = starts[i] + new Vector3(shake, eased * gateRaiseHeight, 0f);
                float a = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                {
                    Color c = sr.color;
                    sr.color = new Color(c.r, c.g, c.b, a);
                }
            }
            yield return null;
        }

        for (int i = 0; i < hideOnUnlock.Length; i++)
        {
            var go = hideOnUnlock[i];
            if (go == null) continue;
            go.SetActive(false);
            go.transform.position = starts[i];
            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                sr.sortingOrder -= 1;
                Color c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
    }

    private IEnumerator FadeOut(SpriteRenderer sr)
    {
        float t = 0f;
        Vector3 start = sr.transform.position;
        Color c = sr.color;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            float k = t / 0.6f;
            sr.transform.position = start + Vector3.down * (k * 0.8f);
            sr.color = new Color(c.r, c.g, c.b, 1f - k);
            yield return null;
        }
        sr.gameObject.SetActive(false);
    }

    private IEnumerator ShakePadlock()
    {
        if (padlockRenderer == null) yield break;
        Transform t = padlockRenderer.transform;
        Vector3 origin = t.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            t.localPosition = origin + new Vector3(Random.Range(-0.06f, 0.06f), 0f, 0f);
            yield return null;
        }
        t.localPosition = origin;
    }

    // ---------------------------------------------------------------- Aviso na tela

    private string CurrentPrompt()
    {
        if (unlocked) return string.IsNullOrEmpty(enterPrompt) ? "[W] Entrar" : enterPrompt;
        if (GameProgress.HasKey(requiredKeyId))
            return string.IsNullOrEmpty(unlockPrompt) ? $"[W] Usar {GameProgress.KeyDisplayName(requiredKeyId)}" : unlockPrompt;
        return null; // trancado sem chave: só a mensagem de "trancado"
    }

    private void OnGUI()
    {
        if (!requireInteract || !playerInRange || busy || Time.time < armedAt) return;
        if (player == null || player.IsDead) return;

        string text = CurrentPrompt();
        if (string.IsNullOrEmpty(text)) return;

        var cam = Camera.main;
        if (cam == null) return;
        Vector3 screen = cam.WorldToScreenPoint(transform.position + Vector3.up * promptHeight);
        if (screen.z < 0f) return;

        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                richText = true
            };
        }

        float bob = Mathf.Sin(Time.time * 4f) * 3f;
        var rect = new Rect(screen.x - 160f, Screen.height - screen.y - 16f + bob, 320f, 32f);
        promptStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + 1.5f, rect.y + 1.5f, rect.width, rect.height), text, promptStyle);
        promptStyle.normal.textColor = new Color(1f, 0.84f, 0.3f);
        GUI.Label(rect, text, promptStyle);
    }
}
