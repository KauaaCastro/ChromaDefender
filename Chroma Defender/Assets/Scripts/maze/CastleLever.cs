using UnityEngine;

/// <summary>
/// Labirinto do Castelo — ALAVANCA.
/// O jogador aperta W / ↑ perto dela: ativa a flag em GameProgress e todas as portas com
/// tranca "Alavanca" e a mesma flag abrem (mesmo em outra sala — ótimo para atalhos e enigmas).
/// Fica puxada para sempre na sessão.
/// </summary>
public class CastleLever : MonoBehaviour
{
    [SerializeField] private string flagId = "alavanca_labirinto_1";
    [SerializeField] private SpriteRenderer leverRenderer;
    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField, TextArea] private string pulledMessage = "Você puxou a alavanca... ao longe, uma grade de ferro se ergue.";
    [SerializeField] private string promptText = "[W] Puxar alavanca";
    [SerializeField] private float range = 1.2f;

    private PlayerBehavior player;
    private bool pulled;
    private bool near;

    public void Configure(string flag, string message = "")
    {
        flagId = flag;
        if (!string.IsNullOrEmpty(message)) pulledMessage = message;
    }

    private void Start()
    {
        pulled = GameProgress.HasFlag(flagId);
        if (leverRenderer != null) leverRenderer.sprite = pulled ? downSprite : upSprite;
    }

    private void Update()
    {
        if (pulled) return;
        if (player == null) player = FindAnyObjectByType<PlayerBehavior>();
        if (player == null || player.IsDead) { near = false; return; }

        Vector3 d = player.transform.position - transform.position;
        near = Mathf.Abs(d.x) <= range && d.y > -0.6f && d.y < 2.5f;
        if (near && MazeGui.InteractPressed()) Pull();
    }

    private void Pull()
    {
        pulled = true;
        GameProgress.SetFlag(flagId);
        if (leverRenderer != null) leverRenderer.sprite = downSprite;
        FireVFX.SpawnFlameBurst(transform.position + Vector3.up * 0.8f, 14);
        if (!string.IsNullOrEmpty(pulledMessage)) ProgressHud.Toast(pulledMessage, 3.5f);
    }

    private void OnGUI()
    {
        if (pulled || !near) return;
        MazeGui.WorldLabel(transform.position + Vector3.up * 2f, promptText, 17, new Color(1f, 0.84f, 0.3f));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.2f);
        // Liga a alavanca às portas que ela abre.
        foreach (var door in FindObjectsByType<CastleDoor>(FindObjectsInactive.Include))
        {
            if (door != null && door.RequiresFlag(flagId)) Gizmos.DrawLine(transform.position + Vector3.up, door.transform.position + Vector3.up * 1.5f);
        }
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.8f, "Alavanca: " + flagId);
#endif
    }
}
