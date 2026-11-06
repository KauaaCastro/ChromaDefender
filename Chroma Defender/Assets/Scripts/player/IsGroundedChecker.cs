using UnityEngine;

public class IsGroundedChecker : MonoBehaviour
{
    [SerializeField] private Transform checkerPosition;
    [SerializeField] private Vector2 checkerSize = new Vector2(0.35f, 0.08f);
    [SerializeField] private LayerMask groundLayer;

    private Collider2D playerCollider;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
    }

    public bool IsGrounded()
    {
        Vector3 pos = checkerPosition != null ? checkerPosition.position : transform.position;

        // Se o checkerPosition não foi configurado, usa a base do collider
        if (checkerPosition == null && playerCollider != null)
        {
            pos = new Vector3(playerCollider.bounds.center.x, playerCollider.bounds.min.y, playerCollider.bounds.center.z);
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, checkerSize, 0f, groundLayer);
        foreach (var hit in hits)
        {
            // Ignora colisores que sejam parte do próprio jogador ou seus filhos
            if (hit != null && hit != playerCollider && !hit.transform.IsChildOf(transform))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = checkerPosition != null ? checkerPosition.position : transform.position;
        if (checkerPosition == null && TryGetComponent<Collider2D>(out var col))
        {
            pos = new Vector3(col.bounds.center.x, col.bounds.min.y, col.bounds.center.z);
        }
        Gizmos.DrawWireCube(pos, checkerSize);
    }
}
