using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Labirinto do Castelo — PORTA que teletransporta o cavaleiro dentro da mesma cena.
///
/// O jogador chega perto e aperta W / ↑. A porta abre, a tela escurece e ele aparece no destino.
///
/// Destinos:
///  - PortaLigada   → aparece na frente de outra porta (ligue as duas para ida e volta).
///  - InicioDoLabirinto → SEMPRE leva à Primeira Sala (CastleMazeStart). É a "Porta do Início".
///  - Aleatorio     → "Porta Traiçoeira": sorteia um destino da lista a cada uso.
///
/// Trancas:
///  - Nenhuma, Chave (GameProgress), SalaLimpa (abre quando não houver inimigos vivos na sala),
///    Alavanca (abre quando a flag da alavanca é ativada).
///
/// Depois de usada uma vez, a porta passa a mostrar para onde leva ("→ Corredor das Armaduras").
/// Para criar: menu Chroma Defender → Labirinto do Castelo → Presets.
/// </summary>
public class CastleDoor : MonoBehaviour
{
    public enum Destination { PortaLigada, InicioDoLabirinto, Aleatorio }
    public enum LockType { Nenhuma, Chave, SalaLimpa, Alavanca }

    [Header("--- Identificação ---")]
    [Tooltip("Nome exibido acima da porta (ex.: \"Porta da Esquerda\"). Vazio = sem título.")]
    [SerializeField] private string doorTitle = "";
    [Tooltip("Id único (usado para lembrar o destino descoberto e trancas abertas). Vazio = gerado pelo nome e posição.")]
    [SerializeField] private string doorId = "";

    [Header("--- Destino ---")]
    [SerializeField] private Destination destination = Destination.PortaLigada;
    [SerializeField] private CastleDoor linkedDoor;
    [Tooltip("Destinos possíveis da Porta Traiçoeira.")]
    [SerializeField] private CastleDoor[] randomDestinations;
    [Tooltip("Mostra o nome da sala de destino depois que o jogador usar a porta uma vez.")]
    [SerializeField] private bool rememberDestination = true;

    [Header("--- Tranca ---")]
    [SerializeField] private LockType lockType = LockType.Nenhuma;
    [SerializeField] private string requiredKeyId = GameProgress.KeyLabirinto;
    [Tooltip("Flag ativada pela alavanca (CastleLever) que destranca esta porta.")]
    [SerializeField] private string requiredFlag = "";
    [SerializeField, TextArea] private string lockedMessage = "";

    [Header("--- Chegada ---")]
    [Tooltip("Quanto à frente da porta o cavaleiro aparece ao chegar por ela (X).")]
    [SerializeField] private float arrivalOffsetX = 0f;
    [SerializeField] private float arrivalHeight = 0.9f;
    [Tooltip("Ao chegar por esta porta, vira o novo ponto de renascimento.")]
    [SerializeField] private bool arrivalIsCheckpoint = false;

    [Header("--- Interação ---")]
    [SerializeField] private float interactHalfWidth = 0.9f;
    [SerializeField] private float interactHeight = 2.6f;
    [SerializeField] private string promptText = "";
    [SerializeField] private float fadeTime = 0.3f;

    [Header("--- Visual ---")]
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Sprite closedSprite;
    [SerializeField] private Sprite ajarSprite;
    [SerializeField] private Sprite openSprite;
    [Tooltip("Sprite mostrado enquanto trancada por SalaLimpa/Alavanca (ex.: grade levadiça).")]
    [SerializeField] private Sprite lockedSprite;
    [Tooltip("Cadeado (tranca por chave). Some ao destrancar.")]
    [SerializeField] private SpriteRenderer padlockRenderer;
    [SerializeField] private Sprite[] padlockFrames;
    [Tooltip("Brilho pulsante atrás da porta (ex.: Porta do Início).")]
    [SerializeField] private SpriteRenderer glowRenderer;
    [SerializeField] private Color glowColor = new Color(0.4f, 0.85f, 1f, 0.6f);
    [Tooltip("Altura do título/aviso acima da base da porta.")]
    [SerializeField] private float labelHeight = 3.5f;

    private static readonly List<CastleDoor> all = new List<CastleDoor>();
    private static float globalCooldownUntil;
    private static bool globalBusy;

    private PlayerBehavior player;
    private bool busy;
    private bool playerNear;
    private bool wasLocked;
    private bool keyUnlocked;
    private float nextLockedMsg;

    public Destination DestinationMode => destination;
    public CastleDoor LinkedDoor => linkedDoor;
    public CastleMazeRoom Room => CastleMazeRoom.At(transform.position + Vector3.up);
    public string DoorKey => string.IsNullOrEmpty(doorId)
        ? $"{name}_{Mathf.RoundToInt(transform.position.x)}_{Mathf.RoundToInt(transform.position.y)}" : doorId;
    private string DiscoveredFlag => "labirinto_porta_vista_" + DoorKey;
    private string KeyOpenFlag => "labirinto_porta_aberta_" + DoorKey;
    public Vector3 ArrivalPosition => transform.position + new Vector3(arrivalOffsetX, arrivalHeight, 0f);
    public static IReadOnlyList<CastleDoor> All => all;

    /// <summary>Sala para onde esta porta leva (para as placas calcularem o caminho). Nulo se aleatória.</summary>
    public CastleMazeRoom DestinationRoom
    {
        get
        {
            switch (destination)
            {
                case Destination.PortaLigada: return linkedDoor != null ? linkedDoor.Room : null;
                case Destination.InicioDoLabirinto:
                    var s = CastleMazeStart.Instance;
                    return s != null ? CastleMazeRoom.At(s.transform.position + Vector3.up) : CastleMazeRoom.StartRoom;
                default: return null;
            }
        }
    }

    // ================================================================== configuração (editor/builders)

    public void SetLink(CastleDoor target) { destination = Destination.PortaLigada; linkedDoor = target; }
    public void SetTitle(string title) => doorTitle = title;
    public bool RequiresFlag(string flag) => lockType == LockType.Alavanca && !string.IsNullOrEmpty(flag) && requiredFlag == flag;
    public void SetId(string id) => doorId = id;

    // Selo temporário controlado por eventos (ex.: arena de ondas trancando a saída durante a luta).
    private bool eventSealed;
    private string eventSealMessage;

    /// <summary>Tranca/destranca a porta por código, independentemente do tipo de tranca configurado.</summary>
    public void SetSealed(bool value, string message = null)
    {
        eventSealed = value;
        eventSealMessage = message;
    }

    // ================================================================== ciclo de vida

    private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    private void OnDisable() => all.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        all.Clear();
        globalCooldownUntil = 0f;
        globalBusy = false;
    }

    private void Start()
    {
        keyUnlocked = GameProgress.HasFlag(KeyOpenFlag);
        wasLocked = IsLocked(out _);
        if (padlockRenderer != null)
        {
            bool showPad = lockType == LockType.Chave && !keyUnlocked;
            padlockRenderer.gameObject.SetActive(showPad);
            if (showPad && padlockFrames != null && padlockFrames.Length > 0) padlockRenderer.sprite = padlockFrames[0];
        }
        RefreshSprite();
    }

    private void Update()
    {
        if (player == null) player = FindAnyObjectByType<PlayerBehavior>();

        if (glowRenderer != null)
        {
            float a = glowColor.a * (0.6f + 0.4f * Mathf.Sin(Time.time * 3f));
            glowRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, a);
        }

        // Grade que abre sozinha (sala limpa / alavanca)
        bool locked = IsLocked(out _);
        if (wasLocked && !locked && lockType != LockType.Chave) StartCoroutine(UnlockEffect());
        else if (!wasLocked && locked) RefreshSprite(); // ex.: inimigos voltaram após o jogador morrer
        wasLocked = locked;

        if (player == null || player.IsDead) { playerNear = false; return; }
        Vector3 d = player.transform.position - transform.position;
        playerNear = Mathf.Abs(d.x) <= interactHalfWidth && d.y > -0.6f && d.y < interactHeight;

        if (!playerNear || busy || globalBusy || Time.time < globalCooldownUntil) return;
        if (!MazeGui.InteractPressed()) return;

        if (locked)
        {
            if (lockType == LockType.Chave && GameProgress.HasKey(requiredKeyId)) StartCoroutine(KeyUnlockRoutine());
            else ShowLockedMessage();
            return;
        }
        StartCoroutine(TravelRoutine());
    }

    // ================================================================== trancas

    public bool IsLocked(out string reason)
    {
        reason = null;
        if (eventSealed)
        {
            reason = !string.IsNullOrEmpty(eventSealMessage) ? eventSealMessage : "A porta está selada por magia.";
            return true;
        }
        switch (lockType)
        {
            case LockType.Chave:
                if (keyUnlocked) return false;
                reason = !string.IsNullOrEmpty(lockedMessage) ? lockedMessage
                    : $"Trancada. Você precisa da <color=#FFD54A>{GameProgress.KeyDisplayName(requiredKeyId)}</color>.";
                return true;

            case LockType.SalaLimpa:
                var room = Room;
                int alive = room != null ? room.AliveEnemies() : 0;
                if (alive <= 0) return false;
                reason = !string.IsNullOrEmpty(lockedMessage) ? lockedMessage
                    : $"A grade está selada por magia sombria.\nDerrote todos os inimigos desta sala (restam {alive}).";
                return true;

            case LockType.Alavanca:
                if (string.IsNullOrEmpty(requiredFlag) || GameProgress.HasFlag(requiredFlag)) return false;
                reason = !string.IsNullOrEmpty(lockedMessage) ? lockedMessage
                    : "A grade não se move. Deve haver uma alavanca em algum lugar...";
                return true;
        }
        return false;
    }

    private void ShowLockedMessage()
    {
        if (Time.time < nextLockedMsg) return;
        nextLockedMsg = Time.time + 1.5f;
        IsLocked(out string reason);
        ProgressHud.Toast(reason, 3f);
        StartCoroutine(Shake(padlockRenderer != null && padlockRenderer.gameObject.activeSelf ? padlockRenderer.transform : doorRenderer != null ? doorRenderer.transform : null));
    }

    private IEnumerator KeyUnlockRoutine()
    {
        busy = true;
        if (padlockRenderer != null && padlockFrames != null)
        {
            foreach (var f in padlockFrames)
            {
                padlockRenderer.sprite = f;
                yield return new WaitForSeconds(1f / 16f);
            }
        }
        keyUnlocked = true;
        GameProgress.SetFlag(KeyOpenFlag);
        FireVFX.SpawnFlameBurst(transform.position + Vector3.up * 1.5f, 20);
        ProgressHud.Toast($"A {GameProgress.KeyDisplayName(requiredKeyId)} destrancou a porta!", 2.5f);
        if (padlockRenderer != null)
        {
            float t = 0f;
            Vector3 start = padlockRenderer.transform.position;
            while (t < 0.5f)
            {
                t += Time.deltaTime;
                padlockRenderer.transform.position = start + Vector3.down * t * 1.6f;
                padlockRenderer.color = new Color(1f, 1f, 1f, 1f - t * 2f);
                yield return null;
            }
            padlockRenderer.gameObject.SetActive(false);
        }
        wasLocked = false;
        RefreshSprite();
        busy = false;
    }

    private IEnumerator UnlockEffect()
    {
        Vector3 c = transform.position + Vector3.up * 1.5f;
        IceVFX.SpawnFrostBurst(c, 18);
        FireVFX.SpawnFlameBurst(c, 12);
        var room = Room;
        if (room != null && room.Contains(player != null ? player.transform.position : Vector3.one * 99999f))
            ProgressHud.Toast(lockType == LockType.SalaLimpa ? "A sala foi purificada — a grade se abriu!" : "A grade se abriu!", 2.5f);

        // A grade sobe e a porta comum aparece.
        if (doorRenderer != null && lockedSprite != null)
        {
            Transform t = doorRenderer.transform;
            Vector3 start = t.localPosition;
            float e = 0f;
            while (e < 0.6f)
            {
                e += Time.deltaTime;
                t.localPosition = start + Vector3.up * (e / 0.6f) * 1.2f;
                yield return null;
            }
            t.localPosition = start;
        }
        RefreshSprite();
    }

    // ================================================================== viagem

    private IEnumerator TravelRoutine()
    {
        Vector3? target = ResolveTarget(out CastleDoor arrivalDoor);
        if (!target.HasValue)
        {
            ProgressHud.Toast("Esta porta não leva a lugar nenhum.\n(Configure o destino no Inspector da CastleDoor.)", 3f);
            yield break;
        }

        busy = true;
        globalBusy = true;

        if (doorRenderer != null)
        {
            if (ajarSprite != null) { doorRenderer.sprite = ajarSprite; yield return new WaitForSeconds(0.1f); }
            if (openSprite != null) { doorRenderer.sprite = openSprite; yield return new WaitForSeconds(0.12f); }
        }

        yield return ScreenFader.FadeOut(fadeTime);

        if (player != null)
        {
            Vector3 p = target.Value;
            player.transform.position = p;
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.position = p; rb.linearVelocity = Vector2.zero; }
            if (arrivalDoor != null && arrivalDoor.arrivalIsCheckpoint) player.SetSafeSpawnPosition(p);
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(p.x, p.y + 1.2f, cam.transform.position.z);
        }

        // Lembra para onde a porta leva (e a volta, se as duas estiverem ligadas entre si).
        if (rememberDestination && destination == Destination.PortaLigada)
        {
            GameProgress.SetFlag(DiscoveredFlag);
            if (linkedDoor != null && linkedDoor.linkedDoor == this) GameProgress.SetFlag(linkedDoor.DiscoveredFlag);
        }

        RefreshSprite();
        CastleMaze.NotifyTeleported();
        globalCooldownUntil = Time.time + 0.6f;
        yield return null;
        yield return ScreenFader.FadeIn(fadeTime);

        busy = false;
        globalBusy = false;
    }

    private Vector3? ResolveTarget(out CastleDoor arrivalDoor)
    {
        arrivalDoor = null;
        switch (destination)
        {
            case Destination.PortaLigada:
                if (linkedDoor == null) return null;
                arrivalDoor = linkedDoor;
                return linkedDoor.ArrivalPosition;

            case Destination.InicioDoLabirinto:
                var start = CastleMazeStart.Instance;
                if (start != null) return start.transform.position + Vector3.up * arrivalHeight;
                var sr = CastleMazeRoom.StartRoom;
                return sr != null ? sr.Center : (Vector3?)null;

            case Destination.Aleatorio:
                var options = new List<CastleDoor>();
                if (randomDestinations != null)
                    foreach (var d in randomDestinations) if (d != null && d != this) options.Add(d);
                if (options.Count == 0) return null;
                arrivalDoor = options[Random.Range(0, options.Count)];
                return arrivalDoor.ArrivalPosition;
        }
        return null;
    }

    // ================================================================== visual

    private void RefreshSprite()
    {
        if (doorRenderer == null) return;
        bool barred = (lockType == LockType.SalaLimpa || lockType == LockType.Alavanca) && IsLocked(out _);
        if (barred && lockedSprite != null) doorRenderer.sprite = lockedSprite;
        else if (closedSprite != null) doorRenderer.sprite = closedSprite;
    }

    private IEnumerator Shake(Transform t)
    {
        if (t == null) yield break;
        Vector3 o = t.localPosition;
        float e = 0f;
        while (e < 0.3f)
        {
            e += Time.deltaTime;
            t.localPosition = o + new Vector3(Random.Range(-0.05f, 0.05f), 0f, 0f);
            yield return null;
        }
        t.localPosition = o;
    }

    private string DestinationLabel()
    {
        switch (destination)
        {
            case Destination.InicioDoLabirinto:
                var sr = CastleMazeRoom.StartRoom;
                return $"<color=#7FE7FF>→ {(sr != null ? sr.RoomName : "Início")}</color>";
            case Destination.Aleatorio:
                return "<color=#C77DFF>→ ???</color>";
            default:
                if (!rememberDestination || !GameProgress.HasFlag(DiscoveredFlag) || linkedDoor == null) return "<color=#AAAAAA>→ ?</color>";
                var r = linkedDoor.Room;
                return $"→ {(r != null ? r.RoomName : "?")}";
        }
    }

    private void OnGUI()
    {
        if (player == null || player.IsDead || globalBusy) return;
        float dist = Vector2.Distance(player.transform.position, transform.position);
        if (dist > 4.5f) return;

        // Título + destino conhecido
        string title = string.IsNullOrEmpty(doorTitle) ? "" : $"<b>{doorTitle}</b>\n";
        MazeGui.WorldLabel(transform.position + Vector3.up * labelHeight, title + DestinationLabel(), 14, new Color(0.95f, 0.9f, 0.8f));

        if (!playerNear || busy) return;
        string prompt;
        if (IsLocked(out _))
            prompt = lockType == LockType.Chave && GameProgress.HasKey(requiredKeyId)
                ? $"[W] Usar {GameProgress.KeyDisplayName(requiredKeyId)}" : "[W] Trancada";
        else
            prompt = !string.IsNullOrEmpty(promptText) ? promptText
                : destination == Destination.InicioDoLabirinto ? "[W] Voltar ao Início" : "[W] Abrir porta";
        float bob = Mathf.Sin(Time.time * 4f) * 0.05f;
        MazeGui.WorldLabel(transform.position + Vector3.up * (labelHeight + 0.75f + bob), prompt, 17, new Color(1f, 0.84f, 0.3f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * interactHeight * 0.5f, new Vector3(interactHalfWidth * 2f, interactHeight, 0f));

        switch (destination)
        {
            case Destination.PortaLigada:
                if (linkedDoor == null) break;
                bool twoWay = linkedDoor.linkedDoor == this;
                Gizmos.color = twoWay ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.5f, 0.2f);
                Vector3 a = transform.position + Vector3.up * 1.5f, b = linkedDoor.transform.position + Vector3.up * 1.5f;
                Gizmos.DrawLine(a, b);
                if (!twoWay) Gizmos.DrawWireSphere(b, 0.35f); // ponta da seta = destino de mão única
                break;
            case Destination.Aleatorio:
                Gizmos.color = new Color(0.8f, 0.4f, 1f, 0.5f);
                if (randomDestinations != null)
                    foreach (var d in randomDestinations)
                        if (d != null) Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, d.transform.position + Vector3.up * 1.5f);
                break;
        }
#if UNITY_EDITOR
        string label = string.IsNullOrEmpty(doorTitle) ? name : doorTitle;
        if (lockType != LockType.Nenhuma) label += $" [{lockType}]";
        UnityEditor.Handles.Label(transform.position + Vector3.up * (labelHeight + 0.4f), label);
#endif
    }
}
