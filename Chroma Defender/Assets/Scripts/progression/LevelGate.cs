using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Passagem para outra fase (portão do castelo, poço mágico, saída do castelo...).
/// Se exigir chave, mostra um cadeado animado que se abre quando o jogador chega com a chave.
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

    [Header("--- Visual do Cadeado ---")]
    [SerializeField] private SpriteRenderer padlockRenderer;
    [SerializeField] private Sprite[] padlockFrames;
    [SerializeField] private float padlockFps = 16f;
    [Tooltip("Objetos escondidos ao destrancar (ex.: portão fechado).")]
    [SerializeField] private GameObject[] hideOnUnlock;
    [Tooltip("Objetos exibidos ao destrancar (ex.: portão aberto, brilho mágico).")]
    [SerializeField] private GameObject[] showOnUnlock;

    [SerializeField] private float enterDelay = 0.6f;
    [Tooltip("Ignora o gatilho logo após a cena carregar (evita reentrar ao voltar de outra fase).")]
    [SerializeField] private float armDelay = 1.0f;

    private bool unlocked;
    private bool busy;
    private float nextLockedMsgTime;
    private float armedAt;

    private string UnlockFlag => $"portao_{requiredKeyId}_{targetScene}";

    public void Configure(string scene, string spawnId, string keyId, string locked, string unlockedMsg, string enterMsg)
    {
        targetScene = scene;
        targetSpawnId = spawnId;
        requiredKeyId = keyId;
        lockedMessage = locked;
        unlockMessage = unlockedMsg;
        enterMessage = enterMsg;
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

        bool open = string.IsNullOrEmpty(requiredKeyId) || GameProgress.HasFlag(UnlockFlag);
        unlocked = open;
        ApplyVisuals(open, instant: true);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryUse(other);
    private void OnTriggerStay2D(Collider2D other) => TryUse(other);

    private void TryUse(Collider2D other)
    {
        if (busy || Time.time < armedAt) return;
        var player = other.GetComponentInParent<PlayerBehavior>();
        if (player == null || player.IsDead) return;

        if (unlocked)
        {
            StartCoroutine(EnterRoutine());
        }
        else if (GameProgress.HasKey(requiredKeyId))
        {
            StartCoroutine(UnlockRoutine());
        }
        else if (Time.time >= nextLockedMsgTime)
        {
            nextLockedMsgTime = Time.time + 3f;
            ProgressHud.Toast(lockedMessage);
            StartCoroutine(ShakePadlock());
        }
    }

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
        ApplyVisuals(true, instant: false);
        if (!string.IsNullOrEmpty(unlockMessage)) ProgressHud.Toast(unlockMessage);

        yield return new WaitForSeconds(1.2f);
        busy = false;
        // O jogador ainda está no gatilho → OnTriggerStay2D fará a entrada.
    }

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
        yield return new WaitForSeconds(enterDelay);

        GameProgress.PendingSpawnId = targetSpawnId;
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetScene);
    }

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
}
