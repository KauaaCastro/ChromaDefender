using System.Collections;
using UnityEngine;

public enum EnemyPolarityType
{
    Neutral, // Toma dano de qualquer elemento
    Ice,     // Inimigo de Gelo (Azul): imune a Gelo, fraco contra FOGO
    Fire     // Inimigo de Fogo (Vermelho): imune a Fogo, fraco contra GELO
}

/// <summary>
/// Inimigo com patrulha e vulnerabilidade de polaridade elemental.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ElementalEnemy : MonoBehaviour, IDamageableTarget
{
    [Header("--- Polaridade Elemental ---")]
    [SerializeField] private EnemyPolarityType polarity = EnemyPolarityType.Neutral;

    [Header("--- Vida & Dano ---")]
    [SerializeField] private int maxHealth = 60;
    [SerializeField] private int currentHealth;
    [SerializeField] private int contactDamage = 15;
    [SerializeField] private float contactKnockback = 4f;

    [Header("--- Patrulha & Movimento ---")]
    [SerializeField] private float moveSpeed = 1.6f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private bool faceMovingDirection = true;

    [Header("--- Feedback Visual ---")]
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private Color iceEnemyColor = new Color(0.4f, 0.85f, 1f, 1f);
    [SerializeField] private Color fireEnemyColor = new Color(1f, 0.5f, 0.2f, 1f);

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private float startX;
    private int direction = 1; // 1 = direita, -1 = esquerda
    private bool isDead = false;
    private bool isHitStunned = false;

    public EnemyPolarityType Polarity => polarity;
    public bool IsDead => isDead;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.linearDamping = 8f; // Amortece o knockback para não voar indefinidamente
        }

        currentHealth = maxHealth;
        startX = transform.position.x;
        ApplyElementalVisuals();
    }

    private void ApplyElementalVisuals()
    {
        if (sr == null) return;

        switch (polarity)
        {
            case EnemyPolarityType.Ice:
                sr.color = iceEnemyColor;
                break;
            case EnemyPolarityType.Fire:
                sr.color = fireEnemyColor;
                break;
            default:
                sr.color = neutralColor;
                break;
        }
    }

    private void FixedUpdate()
    {
        if (isDead || isHitStunned) return;

        // Movimento de patrulha horizontal
        float currentX = transform.position.x;
        if (direction > 0 && currentX >= startX + patrolDistance)
        {
            direction = -1;
        }
        else if (direction < 0 && currentX <= startX - patrolDistance)
        {
            direction = 1;
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
        }

        if (faceMovingDirection)
        {
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (direction > 0 ? -1 : 1); // Orientação do sprite
            transform.localScale = scale;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamagePlayer(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamagePlayer(other.gameObject);
    }

    private void TryDamagePlayer(GameObject target)
    {
        if (isDead) return;

        PlayerBehavior player = target.GetComponent<PlayerBehavior>();
        if (player == null) player = target.GetComponentInParent<PlayerBehavior>();

        if (player != null && !player.IsDead && !player.IsInvulnerable)
        {
            float pushDir = Mathf.Sign(player.transform.position.x - transform.position.x);
            if (pushDir == 0f) pushDir = 1f;

            Vector2 knockback = new Vector2(pushDir * contactKnockback, 3f);
            player.TakeDamage(contactDamage, knockback);
        }
    }

    // ------------------------------------------------------------------
    // Interface IDamageableTarget (Ataques do Jogador)
    // ------------------------------------------------------------------
    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (isDead) return;

        // Regra de Polaridade:
        // Inimigo de Gelo só toma dano de FOGO
        if (polarity == EnemyPolarityType.Ice && attackPolarity != ElementPolarity.Fire)
        {
            Debug.Log("<color=cyan>[IMUNE]</color> Inimigo de Gelo é imune a ataques de Gelo! Use <color=orange>FOGO</color>!");
            StartCoroutine(FlashRoutine(Color.gray));
            return;
        }

        // Inimigo de Fogo só toma dano de GELO
        if (polarity == EnemyPolarityType.Fire && attackPolarity != ElementPolarity.Ice)
        {
            Debug.Log("<color=orange>[IMUNE]</color> Inimigo de Fogo é imune a ataques de Fogo! Use <color=cyan>GELO</color>!");
            StartCoroutine(FlashRoutine(Color.gray));
            return;
        }

        currentHealth -= damage;
        string elemStr = (attackPolarity == ElementPolarity.Fire) ? "<color=orange>FOGO</color>" : "<color=cyan>GELO</color>";
        Debug.Log($"<color=white>[DANO NO INIMIGO]</color> -{damage} de {elemStr}! Vida: {Mathf.Max(0, currentHealth)}/{maxHealth}");

        // Aplica Knockback
        if (rb != null)
        {
            rb.linearVelocity = knockback;
        }

        // Efeitos de Partícula de Impacto
        Vector3 center = col != null ? (Vector3)col.bounds.center : transform.position;
        if (attackPolarity == ElementPolarity.Ice)
        {
            IceVFX.SpawnFrostBurst(center, 14);
        }
        else
        {
            FireVFX.SpawnFlameBurst(center, 16);
        }

        StartCoroutine(FlashRoutine(Color.white));

        if (currentHealth <= 0)
        {
            Die(attackPolarity);
        }
    }

    private void Die(ElementPolarity deathElement)
    {
        if (isDead) return;
        isDead = true;

        if (col != null) col.enabled = false;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Vector3 center = col != null ? (Vector3)col.bounds.center : transform.position;
        if (deathElement == ElementPolarity.Ice)
        {
            IceVFX.SpawnFrostBurst(center, 28);
        }
        else
        {
            FireVFX.SpawnFlameBurst(center, 30);
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator FlashRoutine(Color flashColor)
    {
        isHitStunned = true;
        if (sr != null) sr.color = flashColor;
        yield return new WaitForSeconds(0.12f);
        ApplyElementalVisuals();
        isHitStunned = false;
    }

    private IEnumerator DeathRoutine()
    {
        float elapsed = 0f;
        float duration = 0.3f;
        Vector3 startScale = transform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        Destroy(gameObject);
    }
}
