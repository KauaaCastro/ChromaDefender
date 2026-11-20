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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerBehavior>() != null)
            VictoryController.Trigger();
    }
}
