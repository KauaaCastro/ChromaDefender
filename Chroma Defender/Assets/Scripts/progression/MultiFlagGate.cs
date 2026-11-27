using UnityEngine;

/// <summary>
/// Junta várias flags (ex.: três alavancas) numa só: quando todas estiverem ativas, ativa <see cref="resultFlag"/>.
/// Use a resultFlag numa CastleDoor com tranca "Alavanca" para abrir a porta só depois de todas as alavancas.
/// Mostra no canto da tela quantas já foram ativadas enquanto o jogador está na área.
/// </summary>
public class MultiFlagGate : MonoBehaviour
{
    [SerializeField] private string[] requiredFlags;
    [SerializeField] private string resultFlag = "portao_multiplo";
    [SerializeField, TextArea] private string completeMessage = "Todas as alavancas foram puxadas! O portão se abriu.";
    [Tooltip("Rótulo do contador (vazio = sem contador). Ex.: \"Alavancas\".")]
    [SerializeField] private string counterLabel = "Alavancas";
    [Tooltip("Mostra o contador só com a câmera dentro desta faixa X (mín, máx).")]
    [SerializeField] private Vector2 counterAreaX = new Vector2(-9999f, 9999f);

    private int lastCount = -1;
    private GUIStyle style;

    private int Count()
    {
        int n = 0;
        if (requiredFlags != null) foreach (var f in requiredFlags) if (GameProgress.HasFlag(f)) n++;
        return n;
    }

    private void Update()
    {
        if (GameProgress.HasFlag(resultFlag) || requiredFlags == null || requiredFlags.Length == 0) return;
        int n = Count();
        if (n != lastCount && lastCount >= 0 && n > lastCount && n < requiredFlags.Length)
            ProgressHud.Toast($"{counterLabel}: {n}/{requiredFlags.Length}", 2.5f);
        lastCount = n;
        if (n >= requiredFlags.Length)
        {
            GameProgress.SetFlag(resultFlag);
            ProgressHud.Toast(completeMessage, 4f);
        }
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(counterLabel) || requiredFlags == null || requiredFlags.Length == 0) return;
        var cam = Camera.main;
        if (cam == null || cam.transform.position.x < counterAreaX.x || cam.transform.position.x > counterAreaX.y) return;
        if (style == null) style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
        float scale = Mathf.Max(0.75f, Screen.height / 720f);
        style.fontSize = Mathf.RoundToInt(18 * scale);
        int n = Count();
        string text = $"{counterLabel}: {n}/{requiredFlags.Length}";
        var rect = new Rect(0f, 60f * scale, Screen.width - 24f * scale, 30f * scale);
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        style.normal.textColor = n >= requiredFlags.Length ? new Color(0.55f, 1f, 0.65f) : new Color(1f, 0.85f, 0.35f);
        GUI.Label(rect, text, style);
    }
}
