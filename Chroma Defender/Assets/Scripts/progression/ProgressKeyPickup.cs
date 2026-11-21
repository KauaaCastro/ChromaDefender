using UnityEngine;

/// <summary>
/// Chave de progressão (Castelo, Poço...). Ao ser tocada, fica registrada em GameProgress
/// e continua valendo nas outras cenas. Se o jogador já tem a chave, o objeto some sozinho.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ProgressKeyPickup : MonoBehaviour
{
    [SerializeField] private string keyId = GameProgress.KeyCastelo;
    [SerializeField] private float bobSpeed = 3f;
    [SerializeField] private float bobHeight = 0.18f;

    private Vector3 startPos;
    private bool collected;

    public void Configure(string id)
    {
        keyId = id;
    }

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        if (GameProgress.HasKey(keyId))
        {
            Destroy(gameObject);
            return;
        }
        GetComponent<Collider2D>().isTrigger = true;
        startPos = transform.position;
    }

    private void Update()
    {
        if (collected) return;
        transform.position = startPos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (other.GetComponentInParent<PlayerBehavior>() == null) return;

        collected = true;
        GameProgress.AddKey(keyId);
        ProgressHud.Toast($"Você obteve: <color=#FFD54A>{GameProgress.KeyDisplayName(keyId)}</color>!");
        FireVFX.SpawnFlameBurst(transform.position, 18);
        Destroy(gameObject, 0.05f);
    }
}
