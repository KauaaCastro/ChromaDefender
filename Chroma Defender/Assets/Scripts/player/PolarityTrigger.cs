using UnityEngine;

public class PolarityTrigger : MonoBehaviour, IDamageableTarget
{
    [Header("Configuração de Polaridade")]
    [SerializeField] private ElementPolarity requiredPolarity = ElementPolarity.Fire;
    [SerializeField] private bool allowPassThroughWhenMatched = true;

    private Collider2D col;
    private SpriteRenderer sr;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (sr != null)
        {
            sr.color = (requiredPolarity == ElementPolarity.Fire) ? new Color(1f, 0.4f, 0.2f, 0.85f) : new Color(0.2f, 0.7f, 1f, 0.85f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckInteraction(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        CheckInteraction(collision.gameObject);
    }

    private void CheckInteraction(GameObject target)
    {
        PlayerBehavior player = target.GetComponent<PlayerBehavior>();
        if (player == null) player = target.GetComponentInParent<PlayerBehavior>();

        if (player != null)
        {
            if (player.CurrentPolarity == requiredPolarity)
            {
                Debug.Log($"<color=green>[Chroma]</color> Polaridade compatível ({requiredPolarity})! Interação ativada.");
                if (allowPassThroughWhenMatched && col != null && !col.isTrigger)
                {
                    // Deixa o jogador atravessar a barreira se estiver com a polaridade certa
                    Physics2D.IgnoreCollision(col, player.GetComponent<Collider2D>(), true);
                }
            }
            else
            {
                Debug.Log($"<color=red>[Chroma]</color> Polaridade incompatível! Mude para {requiredPolarity}.");
            }
        }
    }

    // Quebra ao ser golpeado com o elemento correto
    public void TakeDamage(int damage, Vector2 knockback, ElementPolarity attackPolarity)
    {
        if (attackPolarity == requiredPolarity)
        {
            Debug.Log($"<color=green>[Chroma]</color> Cristal elemental de {requiredPolarity} destruído! Passagem liberada!");
            if (col != null) col.enabled = false;

            if (attackPolarity == ElementPolarity.Fire)
            {
                FireVFX.SpawnFlameBurst(transform.position, 22);
            }
            else
            {
                IceVFX.SpawnFrostBurst(transform.position, 22);
            }

            Destroy(gameObject, 0.08f);
        }
        else
        {
            Debug.Log($"<color=yellow>[Chroma]</color> O cristal resistiu! Ele exige um golpe de <color=orange>{requiredPolarity}</color>!");
        }
    }
}
