using UnityEngine;

/// <summary>
/// Ao passar por aqui, o ponto de renascimento do cavaleiro (queda no abismo / morte) é atualizado.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Vector2 respawnOffset = new Vector2(0f, 1.5f);
    [SerializeField] private bool showMessage = true;

    private bool activated;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerBehavior>();
        if (player == null || player.IsDead) return;

        player.SetSafeSpawnPosition(transform.position + (Vector3)respawnOffset);

        if (!activated)
        {
            activated = true;
            IceVFX.SpawnFrostBurst(transform.position + Vector3.up, 14);
            if (showMessage) ProgressHud.Toast("Checkpoint salvo", 1.8f);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1f, 2f, 0f));
    }
}
