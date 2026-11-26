using System.Collections;
using UnityEngine;

/// <summary>
/// Ponto de chegada ao entrar na cena vindo de outra fase.
/// Se GameProgress.PendingSpawnId bater com o id, o cavaleiro é posicionado aqui
/// (e este passa a ser o ponto de renascimento).
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnId;
    [Tooltip("Ponto inicial da cena quando ela é aberta direto (Play no editor), sem vir de outra fase.")]
    [SerializeField] private bool isDefaultSpawn = false;

    public void Configure(string id, bool isDefault = false)
    {
        spawnId = id;
        isDefaultSpawn = isDefault;
    }

    private IEnumerator Start()
    {
        bool arriving = !string.IsNullOrEmpty(spawnId) && GameProgress.PendingSpawnId == spawnId;
        bool directPlay = isDefaultSpawn && string.IsNullOrEmpty(GameProgress.PendingSpawnId);
        if (!arriving && !directPlay) yield break;
        if (arriving) GameProgress.PendingSpawnId = null;

        // Espera o PlayerBehavior.Start (que alinha o spawn padrão ao chão) rodar primeiro.
        yield return null;

        var player = FindFirstObjectByType<PlayerBehavior>();
        if (player == null) yield break;

        player.transform.position = transform.position;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        player.SetSafeSpawnPosition(transform.position);

        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 p = cam.transform.position;
            cam.transform.position = new Vector3(transform.position.x, transform.position.y + 1.2f, p.z);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
