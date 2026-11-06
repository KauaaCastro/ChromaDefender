using UnityEngine;

/// <summary>
/// Chave Dourada colecionável da demo.
/// Ao ser tocada pelo jogador, adiciona a flag de posse da chave em PlayerBehavior.
/// </summary>
public class KeyPickup : MonoBehaviour
{
    [Header("--- Animação Flutuante ---")]
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float rotateSpeed = 0f;

    [Header("--- Visual & Brilho ---")]
    [SerializeField] private Color keyGlowColor = new Color(1f, 0.85f, 0.2f, 1f);

    private Vector3 initialPos;
    private bool collected = false;

    private void Start()
    {
        initialPos = transform.position;

        // Garante que tenha colisor configurado como trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // SpriteRenderer com cor dourada vibrante
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.color = keyGlowColor;
    }

    private void Update()
    {
        if (collected) return;

        // Movimento de sobe e desce suave (bobbing)
        float newY = initialPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(initialPos.x, newY, initialPos.z);

        if (rotateSpeed != 0f)
        {
            transform.Rotate(Vector3.forward, rotateSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        PlayerBehavior player = other.GetComponent<PlayerBehavior>();
        if (player == null) player = other.GetComponentInParent<PlayerBehavior>();

        if (player != null)
        {
            collected = true;
            player.CollectCastleKey();

            // Efeito visual rápido de coleta (partículas douradas de fogo/luz)
            FireVFX.SpawnFlameBurst(transform.position, 16);

            Destroy(gameObject, 0.05f);
        }
    }
}
