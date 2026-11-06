using UnityEngine;

/// <summary>
/// Câmera 2D suave com perseguição ao jogador e limites de cenário.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("--- Alvo & Offset ---")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
    [SerializeField] private float smoothTime = 0.22f;

    [Header("--- Limites do Mapa (Bounds) ---")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-5f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2(120f, 20f);

    private Vector3 currentVelocity = Vector3.zero;

    private void Start()
    {
        if (target == null)
        {
            PlayerBehavior player = FindFirstObjectByType<PlayerBehavior>();
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            Vector3 startPos = target.position + offset;
            if (useBounds)
            {
                startPos.x = Mathf.Clamp(startPos.x, minBounds.x, maxBounds.x);
                startPos.y = Mathf.Clamp(startPos.y, minBounds.y, maxBounds.y);
            }
            startPos.z = offset.z;
            transform.position = startPos;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }
        desiredPosition.z = offset.z;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
