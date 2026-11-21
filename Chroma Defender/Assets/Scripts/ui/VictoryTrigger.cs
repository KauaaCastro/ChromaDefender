using UnityEngine;

/// <summary>
/// Zona de fim de fase. Coloque num objeto com um Collider2D marcado como "Is Trigger".
/// Quando o jogador entra, a tela de vitória aparece.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class VictoryTrigger : MonoBehaviour
{
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Awake()
    {
        // Se o gatilho ainda estiver na posição inicial onde o jogador nasce (x ≈ 0.48),
        // reposiciona automaticamente para o final da fase (x = 108, y = 1).
        if (Mathf.Abs(transform.position.x - 0.47903723f) < 0.05f)
        {
            transform.position = new Vector3(108f, 1f, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerBehavior>() != null)
            VictoryController.Trigger();
    }
}
