using System.Collections;
using UnityEngine;

/// <summary>
/// Plataforma quebradiça / Chão falso.
/// Ao ser pisada ou quando o jogador se aproxima, ela treme e desaba,
/// derrubando o jogador no andar inferior.
/// </summary>
public class CrumblingPlatform : MonoBehaviour
{
    [Header("--- Tempos da Armadilha ---")]
    [Tooltip("Tempo que a plataforma treme antes de cair.")]
    [SerializeField] private float shakeDuration = 0.35f;
    [Tooltip("Tempo para a plataforma reaparecer/recompor-se (0 para nunca reaparecer).")]
    [SerializeField] private float respawnTime = 6f;
    [Tooltip("Velocidade com que ela cai após ceder.")]
    [SerializeField] private float fallSpeed = 9f;

    [Header("--- Visual & Feedback ---")]
    [SerializeField] private float shakeIntensity = 0.07f;
    [SerializeField] private Color warningTint = new Color(0.7f, 0.7f, 0.7f, 1f);

    private Vector3 initialPosition;
    private Color initialColor;
    private Collider2D col;
    private SpriteRenderer sr;
    private bool isCrumbling = false;

    private void Awake()
    {
        initialPosition = transform.position;
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) initialColor = sr.color;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isCrumbling) return;

        // Verifica se quem pisou foi o jogador por cima da plataforma
        PlayerBehavior player = collision.gameObject.GetComponent<PlayerBehavior>();
        if (player == null) player = collision.gameObject.GetComponentInParent<PlayerBehavior>();

        if (player != null)
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                if (contact.normal.y < -0.5f) // O jogador está acima da plataforma
                {
                    TriggerCrumble();
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCrumbling) return;

        // Também pode ser acionado por trigger de aproximação
        PlayerBehavior player = other.GetComponent<PlayerBehavior>();
        if (player == null) player = other.GetComponentInParent<PlayerBehavior>();

        if (player != null)
        {
            TriggerCrumble();
        }
    }

    public void TriggerCrumble()
    {
        if (isCrumbling) return;
        StartCoroutine(CrumbleRoutine());
    }

    private IEnumerator CrumbleRoutine()
    {
        isCrumbling = true;

        // Fase 1: Tremor de alerta (rachando)
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float offsetX = Random.Range(-shakeIntensity, shakeIntensity);
            float offsetY = Random.Range(-shakeIntensity * 0.5f, shakeIntensity * 0.5f);
            transform.position = initialPosition + new Vector3(offsetX, offsetY, 0f);

            if (sr != null)
            {
                sr.color = Color.Lerp(initialColor, warningTint, elapsed / shakeDuration);
            }
            yield return null;
        }

        // Fase 2: Desativa colisão (jogador cai através)
        if (col != null) col.enabled = false;

        // Fase 3: Queda rápida e desaparecimento
        float fallElapsed = 0f;
        Vector3 fallStart = transform.position;
        while (fallElapsed < 0.6f)
        {
            fallElapsed += Time.deltaTime;
            transform.position += Vector3.down * (fallSpeed * Time.deltaTime);

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, fallElapsed / 0.6f);
                sr.color = c;
            }
            yield return null;
        }

        if (sr != null) sr.enabled = false;

        // Fase 4: Respawn (se configurado)
        if (respawnTime > 0f)
        {
            yield return new WaitForSeconds(respawnTime);
            transform.position = initialPosition;
            if (sr != null)
            {
                sr.color = initialColor;
                sr.enabled = true;
            }
            if (col != null) col.enabled = true;
            isCrumbling = false;
        }
    }
}
