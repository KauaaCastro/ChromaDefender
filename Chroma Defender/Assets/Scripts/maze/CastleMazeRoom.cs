using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Labirinto do Castelo — uma SALA (área) do labirinto.
///
/// Não precisa de colisor: a sala é só um retângulo (tamanho + centro), desenhado em amarelo na Scene.
/// Portas, placas e o HUD descobrem em que sala estão pela posição.
///  - Marque "Is Start Room" na Primeira Sala: é para lá que as placas apontam
///    e para onde a Porta do Início sempre leva (junto com o CastleMazeStart).
///  - Portas "Seladas" contam os inimigos vivos dentro deste retângulo.
/// </summary>
public class CastleMazeRoom : MonoBehaviour
{
    [SerializeField] private string roomName = "Sala do Labirinto";
    [Tooltip("Largura e altura da área da sala (em unidades).")]
    [SerializeField] private Vector2 size = new Vector2(24f, 10f);
    [SerializeField] private Vector2 centerOffset = Vector2.zero;
    [Tooltip("Primeira Sala do labirinto (início da Fase 2).")]
    [SerializeField] private bool isStartRoom = false;
    [Tooltip("Mensagem extra mostrada na PRIMEIRA vez que o jogador entra (vazio = só o nome).")]
    [SerializeField, TextArea] private string firstVisitMessage = "";

    private static readonly List<CastleMazeRoom> all = new List<CastleMazeRoom>();
    public static IReadOnlyList<CastleMazeRoom> All => all;

    private int cachedAlive;
    private float nextAliveCheck;

    public string RoomName => roomName;
    public bool IsStartRoom => isStartRoom;
    public string FirstVisitMessage => firstVisitMessage;
    public Vector3 Center => transform.position + (Vector3)centerOffset;
    public string VisitedFlag => "labirinto_sala_" + roomName;
    public bool Visited => GameProgress.HasFlag(VisitedFlag);

    public void Configure(string name, Vector2 roomSize, bool start, string message = "")
    {
        roomName = name;
        size = roomSize;
        isStartRoom = start;
        firstVisitMessage = message;
    }

    private void OnEnable() { if (!all.Contains(this)) all.Add(this); }
    private void OnDisable() => all.Remove(this);

    public bool Contains(Vector3 p)
    {
        Vector3 c = Center;
        return Mathf.Abs(p.x - c.x) <= size.x * 0.5f && Mathf.Abs(p.y - c.y) <= size.y * 0.5f;
    }

    /// <summary>Inimigos vivos dentro da sala (atualiza no máximo 4x por segundo).</summary>
    public int AliveEnemies()
    {
        if (Time.time < nextAliveCheck) return cachedAlive;
        nextAliveCheck = Time.time + 0.25f;
        int n = 0;
        foreach (var e in FindObjectsByType<EnemyBehavior>(FindObjectsInactive.Exclude))
            if (e != null && !e.IsDead && Contains(e.transform.position)) n++;
        cachedAlive = n;
        return n;
    }

    /// <summary>A menor sala que contém o ponto (salas podem ficar uma dentro da outra).</summary>
    public static CastleMazeRoom At(Vector3 p)
    {
        CastleMazeRoom best = null;
        float bestArea = float.MaxValue;
        foreach (var r in all)
        {
            if (r == null || !r.Contains(p)) continue;
            float area = r.size.x * r.size.y;
            if (area < bestArea) { best = r; bestArea = area; }
        }
        return best;
    }

    public static CastleMazeRoom StartRoom
    {
        get
        {
            foreach (var r in all) if (r != null && r.isStartRoom) return r;
            return null;
        }
    }

    public static CastleMazeRoom Find(string name)
    {
        foreach (var r in all) if (r != null && r.roomName == name) return r;
        return null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => all.Clear();

    private void OnDrawGizmos()
    {
        Gizmos.color = isStartRoom ? new Color(0.3f, 1f, 0.4f, 0.8f) : new Color(1f, 0.85f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(Center, new Vector3(size.x, size.y, 0f));
#if UNITY_EDITOR
        UnityEditor.Handles.Label(Center + new Vector3(-size.x * 0.5f + 0.3f, size.y * 0.5f - 0.3f, 0f),
            (isStartRoom ? "★ " : "") + roomName);
#endif
    }
}
