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

    // Lido no Awake (antes de qualquer Start): o ponto de chegada certo limpa o PendingSpawnId no Start dele,
    // e sem isto um ponto "padrão" que rodasse o Start depois achava que era Play direto e movia o jogador de novo.
    private string pendingAtLoad;

    private void Awake() => pendingAtLoad = GameProgress.PendingSpawnId;

    private IEnumerator Start()
    {
        bool arriving = !string.IsNullOrEmpty(spawnId) && pendingAtLoad == spawnId;
        bool directPlay = isDefaultSpawn && string.IsNullOrEmpty(pendingAtLoad);
        if (!arriving && !directPlay) yield break;
        if (arriving) GameProgress.PendingSpawnId = null;

        // Espera o PlayerBehavior.Start (que alinha o spawn padrão ao chão) rodar primeiro.
        yield return null;

        var player = FindFirstObjectByType<PlayerBehavior>();
        if (player == null) yield break;

        Vector3 target = GroundedPosition(player);
        player.transform.position = target;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) { rb.position = target; rb.linearVelocity = Vector2.zero; }
        player.SetSafeSpawnPosition(target);

        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 p = cam.transform.position;
            cam.transform.position = new Vector3(target.x, target.y + 1.2f, p.z);
        }
    }

    /// <summary>
    /// Posição do jogador com os PÉS apoiados no chão logo abaixo deste ponto (o pivô do cavaleiro fica acima dos pés;
    /// usar a posição crua deixava os pés dentro do chão e ele "afundava" no colisor).
    /// </summary>
    private Vector3 GroundedPosition(PlayerBehavior player)
    {
        var col = player.GetComponent<Collider2D>();
        if (col == null) return transform.position;
        float pivotToFeet = player.transform.position.y - col.bounds.min.y;
        int mask = LayerMask.GetMask("Ground");
        var hit = Physics2D.Raycast((Vector2)transform.position + Vector2.up * 3f, Vector2.down, 10f, mask);
        if (hit.collider == null || hit.collider.isTrigger) return transform.position + Vector3.up * pivotToFeet;
        return new Vector3(transform.position.x, hit.point.y + pivotToFeet + 0.05f, transform.position.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
