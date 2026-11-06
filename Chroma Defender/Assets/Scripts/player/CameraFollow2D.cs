using UnityEngine;

/// <summary>
/// Câmera 2D suave com perseguição ao jogador e limites dinâmicos de cenário.
/// Acompanha fases compridas automaticamente e impede o jogador de andar para trás do início.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("--- Alvo & Offset ---")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);
    [SerializeField] private float smoothTime = 0.18f;

    [Header("--- Limites do Mapa (Bounds) ---")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 minBounds = new Vector2(-2f, -10f);
    [SerializeField] private Vector2 maxBounds = new Vector2(500f, 25f);

    private Vector3 currentVelocity = Vector3.zero;

    private void Awake()
    {
        // Se por engano o jogador foi arrastado para dentro da Câmera na Hierarchy, desanexa imediatamente
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.ToLower().Contains("player") || child.GetComponent<PlayerBehavior>() != null)
            {
                child.SetParent(null, true);
            }
        }
    }

    private void Start()
    {
        // Garante suporte a fases compridas (expande se estava com o limite antigo de 30m)
        if (maxBounds.x < 150f)
        {
            maxBounds = new Vector2(500f, Mathf.Max(maxBounds.y, 25f));
        }

        AutoFitBoundsToTilemaps();

        if (target == null)
        {
            PlayerBehavior player = FindFirstObjectByType<PlayerBehavior>();
            if (player != null) target = player.transform;
        }

        if (target != null)
        {
            if (target.IsChildOf(transform))
            {
                target.SetParent(null, true);
            }

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

        if (target.IsChildOf(transform))
        {
            target.SetParent(null, true);
        }

        Vector3 desiredPosition = target.position + offset;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }
        desiredPosition.z = offset.z;

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);
    }

    private void AutoFitBoundsToTilemaps()
    {
        var tilemaps = FindObjectsByType<UnityEngine.Tilemaps.Tilemap>(FindObjectsSortMode.None);
        foreach (var tm in tilemaps)
        {
            if (tm.cellBounds.size.x > 0)
            {
                Vector3 worldMin = tm.CellToWorld(tm.cellBounds.min);
                Vector3 worldMax = tm.CellToWorld(tm.cellBounds.max);

                if (worldMin.x < minBounds.x)
                {
                    minBounds.x = worldMin.x - 2f;
                }
                if (worldMax.x > maxBounds.x)
                {
                    maxBounds.x = worldMax.x + 12f;
                }
            }
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
