using UnityEngine;
using System.Collections;

/// <summary>
/// Boneco de treino ou objeto destruível para testar ataques e polaridades (Fogo e Gelo).
/// </summary>
public class TrainingDummy : MonoBehaviour, IDamageableTarget
{
    [Header("--- Configurações de Vida ---")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private bool respawnOnDeath = true;
    [SerializeField] private float respawnTime = 2f;

    [Header("--- Fraqueza / Polaridade ---")]
    [Tooltip("Se marcado, só recebe dano se a polaridade do golpe bater com esta.")]
    [SerializeField] private bool requiresSpecificPolarity = false;
    [SerializeField] private ElementPolarity weakAgainst = ElementPolarity.Fire;

    [Header("--- Feedback Visual ---")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float flashDuration = 0.12f;

    [Header("--- Física & Reação a Dano ---")]
    [Tooltip("Resistência a empurrão (0 = empurrão total, 1 = inamovível).")]
    [SerializeField, Range(0f, 1f)] private float knockbackResistance = 0.15f;
    [Tooltip("Arrasto linear para desacelerar o recuo rapidamente sem deslizar no chão.")]
    [SerializeField] private float linearDrag = 14f;

    private SpriteRenderer sr;
    private Color originalColor;
    private Rigidbody2D rb;
    private Collider2D col;
    private bool isDead;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.linearDamping = linearDrag;
        }

        if (sr != null) originalColor = sr.color;
        currentHealth = maxHealth;
    }

    private void FixedUpdate()
    {
        // Freia suavemente o knockback para que o boneco não deslize para longe do combo
        if (rb != null && rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, linearDrag * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Recebe dano, reage com knockback, flash de cor e números de dano no console.
    /// </summary>
    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (isDead) return;

        // Se exige polaridade específica
        if (requiresSpecificPolarity && attackPolarity != weakAgainst)
        {
            Debug.Log($"<color=gray>[IMUNE]</color> O alvo resistiu ao ataque! É necessário atacar com: <color=yellow>{weakAgainst}</color>");
            StartCoroutine(FlashColorRoutine(Color.gray));
            return;
        }

        currentHealth -= damage;
        string elemento = (attackPolarity == ElementPolarity.Fire) ? "<color=orange>FOGO</color>" : "<color=cyan>GELO</color>";
        Debug.Log($"<color=white>[DANO CAUSADO]</color> -{damage} de {elemento}! Vida do alvo: {Mathf.Max(0, currentHealth)}/{maxHealth}");

        // Knockback físico amortecido para manter o fluxo do combo
        if (rb != null)
        {
            Vector2 finalKnockback = knockback * (1f - Mathf.Clamp01(knockbackResistance));
            rb.linearVelocity = finalKnockback;
        }

        // Efeito de impacto elemental no alvo
        Vector3 center = col != null ? (Vector3)col.bounds.center : transform.position;
        if (attackPolarity == ElementPolarity.Ice) IceVFX.SpawnFrostBurst(center);
        else FireVFX.SpawnFlameBurst(center);

        // Flash de cor
        Color flash = (attackPolarity == ElementPolarity.Fire) ? new Color(1f, 0.4f, 0.1f) : new Color(0.2f, 0.7f, 1f);
        StartCoroutine(FlashColorRoutine(flash));

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashColorRoutine(Color flash)
    {
        if (sr != null)
        {
            sr.color = flash;
            yield return new WaitForSeconds(flashDuration);
            sr.color = originalColor;
        }
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("<color=red>[ALVO DERROTADO]</color> O alvo foi destruído!");

        if (respawnOnDeath)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            Destroy(gameObject, 0.2f);
        }
    }

    private IEnumerator RespawnRoutine()
    {
        if (sr != null) sr.enabled = false;
        if (col != null) col.enabled = false;
        if (rb != null) rb.simulated = false;

        yield return new WaitForSeconds(respawnTime);

        currentHealth = maxHealth;
        isDead = false;
        if (sr != null)
        {
            sr.color = originalColor;
            sr.enabled = true;
        }
        if (col != null) col.enabled = true;
        if (rb != null)
        {
            rb.simulated = true;
            rb.linearVelocity = Vector2.zero;
        }

        Debug.Log("<color=green>[ALVO RENASCEU]</color> Pronto para novos testes!");
    }
}
