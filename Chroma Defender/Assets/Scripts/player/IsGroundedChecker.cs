using UnityEngine;

/// <summary>
/// Verificador de chao confiavel, imune a deslizes, desincronia de escala e rampas.
/// Analisa a base real do colisor do jogador com OverlapBox e multi-raycasts redundantes.
/// </summary>
public class IsGroundedChecker : MonoBehaviour
{
    [SerializeField] private Transform checkerPosition;
    [SerializeField] private Vector2 checkerSize = new Vector2(0.4f, 0.12f);
    [SerializeField] private LayerMask groundLayer;

    private Collider2D playerCollider;

    private void Awake()
    {
        playerCollider = GetComponent<Collider2D>();
        if (groundLayer.value == 0)
        {
            int groundMask = LayerMask.GetMask("Ground");
            if (groundMask != 0) groundLayer = groundMask;
            else groundLayer = LayerMask.GetMask("Default");
        }
    }

    public bool IsGrounded()
    {
        if (playerCollider == null) playerCollider = GetComponent<Collider2D>();

        int mask = groundLayer.value != 0 ? groundLayer.value : (LayerMask.GetMask("Ground") | LayerMask.GetMask("Default"));

        if (playerCollider != null)
        {
            Bounds b = playerCollider.bounds;
            float footY = b.min.y;

            // 1. Verificacao por OverlapBox na sola dos pes
            Vector2 boxCenter = new Vector2(b.center.x, footY - 0.03f);
            Vector2 boxSize = new Vector2(b.size.x * 0.8f, Mathf.Max(0.1f, checkerSize.y));
            Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f, mask);
            foreach (var hit in hits)
            {
                if (IsValidGround(hit)) return true;
            }

            // 2. Verificacao redundante por 3 Raycasts verticais (Centro, Esquerda, Direita)
            float rayLen = 0.16f;
            Vector2 originCenter = new Vector2(b.center.x, footY + 0.05f);
            Vector2 originLeft = new Vector2(b.min.x + 0.05f, footY + 0.05f);
            Vector2 originRight = new Vector2(b.max.x - 0.05f, footY + 0.05f);

            RaycastHit2D hitC = Physics2D.Raycast(originCenter, Vector2.down, rayLen, mask);
            if (IsValidGround(hitC.collider)) return true;

            RaycastHit2D hitL = Physics2D.Raycast(originLeft, Vector2.down, rayLen, mask);
            if (IsValidGround(hitL.collider)) return true;

            RaycastHit2D hitR = Physics2D.Raycast(originRight, Vector2.down, rayLen, mask);
            if (IsValidGround(hitR.collider)) return true;
        }
        else if (checkerPosition != null)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(checkerPosition.position, 0.2f, mask);
            foreach (var hit in hits)
            {
                if (IsValidGround(hit)) return true;
            }
        }
        else
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.3f, mask);
            foreach (var hit in hits)
            {
                if (IsValidGround(hit)) return true;
            }
        }

        return false;
    }

    private bool IsValidGround(Collider2D col)
    {
        if (col == null) return false;
        if (col.isTrigger) return false;
        if (col == playerCollider) return false;
        if (col.transform.IsChildOf(transform)) return false;

        // Inimigos ou pickups nao sao chao
        string nameLower = col.gameObject.name.ToLower();
        if (nameLower.Contains("enemy") || nameLower.Contains("slime") || nameLower.Contains("rat") || nameLower.Contains("pickup"))
        {
            return false;
        }

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = transform.position;
        Vector2 size = checkerSize;

        if (TryGetComponent<Collider2D>(out var col))
        {
            Bounds b = col.bounds;
            pos = new Vector3(b.center.x, b.min.y - 0.03f, b.center.z);
            size = new Vector2(b.size.x * 0.8f, Mathf.Max(0.1f, checkerSize.y));
        }
        Gizmos.DrawWireCube(pos, size);
    }
}
