using System.Collections;
using UnityEngine;

/// <summary>
/// Grande Porta Trancada do Castelo.
/// Exige que o jogador possua a Chave Dourada para abrir e liberar a passagem.
/// </summary>
public class LockedDoor : MonoBehaviour, IDamageableTarget
{
    [Header("--- Configuração da Porta ---")]
    [Tooltip("Distância vertical que a porta se move para cima ao abrir.")]
    [SerializeField] private float openSlideDistance = 3.5f;
    [SerializeField] private float openDuration = 1.2f;
    [SerializeField] private bool consumeKeyOnOpen = false;

    [Header("--- Bloqueio & Feedback ---")]
    [SerializeField] private Collider2D solidCollider;
    [SerializeField] private SpriteRenderer doorRenderer;

    private bool isOpened = false;
    private bool isShaking = false;
    private Vector3 initialPosition;

    private void Awake()
    {
        initialPosition = transform.position;
        if (solidCollider == null) solidCollider = GetComponent<Collider2D>();
        if (doorRenderer == null) doorRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryInteractWithDoor(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryInteractWithDoor(collision.gameObject);
    }

    public void TryInteractWithDoor(GameObject source)
    {
        if (isOpened) return;

        PlayerBehavior player = source.GetComponent<PlayerBehavior>();
        if (player == null) player = source.GetComponentInParent<PlayerBehavior>();

        if (player == null) return;

        if (player.HasCastleKey)
        {
            OpenDoor(player);
        }
        else
        {
            DenyAccess();
        }
    }

    private void OpenDoor(PlayerBehavior player)
    {
        if (isOpened) return;
        isOpened = true;

        if (consumeKeyOnOpen)
        {
            player.ConsumeCastleKey();
        }
        else
        {
            Debug.Log("<color=green>[PORTA DESTRANCADA]</color> Você usou a Chave Dourada! A passagem para a Câmara Secreta foi aberta!");
        }

        if (solidCollider != null) solidCollider.enabled = false;

        StartCoroutine(OpenSlideRoutine());
    }

    private void DenyAccess()
    {
        if (isShaking) return;
        Debug.Log("<color=yellow>[PORTA TRANCADA]</color> Esta imponente porta está trancada por uma fechadura dourada. Encontre a Chave no castelo!");
        StartCoroutine(ShakeRoutine());
    }

    private IEnumerator OpenSlideRoutine()
    {
        Vector3 start = transform.position;
        Vector3 target = start + Vector3.up * openSlideDistance;
        float elapsed = 0f;

        while (elapsed < openDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / openDuration);
            transform.position = Vector3.Lerp(start, target, t);

            // Suave desvanecimento no final
            if (doorRenderer != null && t > 0.5f)
            {
                Color c = doorRenderer.color;
                c.a = Mathf.Lerp(1f, 0.2f, (t - 0.5f) * 2f);
                doorRenderer.color = c;
            }
            yield return null;
        }

        transform.position = target;
        if (doorRenderer != null)
        {
            Color c = doorRenderer.color;
            c.a = 0f;
            doorRenderer.color = c;
        }
    }

    private IEnumerator ShakeRoutine()
    {
        isShaking = true;
        float elapsed = 0f;
        float duration = 0.25f;
        float intensity = 0.08f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float xOffset = Random.Range(-intensity, intensity);
            transform.position = initialPosition + new Vector3(xOffset, 0f, 0f);
            yield return null;
        }

        transform.position = initialPosition;
        isShaking = false;
    }

    // Se o jogador atacar a porta trancada, dá o mesmo feedback físico
    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (!isOpened)
        {
            DenyAccess();
        }
    }
}
