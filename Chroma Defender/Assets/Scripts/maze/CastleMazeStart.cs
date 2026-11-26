using UnityEngine;

/// <summary>
/// Labirinto do Castelo — ponto de INÍCIO.
/// A "Porta do Início" sempre leva o jogador para cá, não importa onde ele esteja.
/// Coloque UM na Primeira Sala (no chão, onde o cavaleiro deve aparecer).
/// </summary>
public class CastleMazeStart : MonoBehaviour
{
    private static CastleMazeStart instance;

    public static CastleMazeStart Instance
    {
        get
        {
            if (instance == null) instance = FindAnyObjectByType<CastleMazeStart>();
            return instance;
        }
    }

    private void OnEnable() => instance = this;
    private void OnDisable() { if (instance == this) instance = null; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 2f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2.3f, "INÍCIO DO LABIRINTO");
#endif
    }
}
