using UnityEngine;

/// <summary>
/// Zona de morte invisível no fundo de um buraco/abismo: o cavaleiro que cair nela morre na hora
/// (mesmo caminho da queda para fora do mapa — Game Over e renascimento no checkpoint).
/// Precisa de um BoxCollider2D (vira trigger automaticamente).
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DeathZone : MonoBehaviour
{
    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
    private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other) => Check(other);
    private void OnTriggerStay2D(Collider2D other) => Check(other);

    private static void Check(Collider2D other)
    {
        var p = other.GetComponentInParent<PlayerBehavior>();
        if (p != null && !p.IsDead) p.KillByFall();
    }

    private void OnDrawGizmos()
    {
        var b = GetComponent<BoxCollider2D>();
        if (b == null) return;
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.35f);
        Gizmos.DrawCube(transform.TransformPoint(b.offset), Vector3.Scale(b.size, transform.lossyScale));
    }
}
