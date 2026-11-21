using System.Collections;
using UnityEngine;

/// <summary>
/// Plataforma mágica de impulso: ao ficar parado em cima por um instante, o cavaleiro é lançado para cima.
/// Usada para sair do fosso do Poço e voltar à bifurcação.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BouncePad : MonoBehaviour
{
    [Tooltip("Velocidade vertical do lançamento. 12.5 ≈ 8 blocos de altura com gravidade padrão.")]
    [SerializeField] private float launchSpeed = 12.5f;
    [Tooltip("Tempo em cima da plataforma antes de lançar (dá tempo de sair se não quiser subir).")]
    [SerializeField] private float chargeTime = 0.35f;
    [SerializeField] private float cooldown = 0.6f;
    [SerializeField] private Transform visual;

    private float charge;
    private float readyAt;
    private Vector3 visualBaseScale = Vector3.one;

    public void Configure(Transform visualTransform) => visual = visualTransform;

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (visual != null) visualBaseScale = visual.localScale;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (Time.time < readyAt) return;
        var player = other.GetComponentInParent<PlayerBehavior>();
        if (player == null || player.IsDead) return;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null || rb.linearVelocity.y > 0.5f) return;

        charge += Time.fixedDeltaTime;
        if (visual != null)
        {
            float k = Mathf.Clamp01(charge / chargeTime);
            visual.localScale = new Vector3(visualBaseScale.x * (1f + 0.15f * k), visualBaseScale.y * (1f - 0.3f * k), visualBaseScale.z);
        }

        if (charge >= chargeTime)
        {
            charge = 0f;
            readyAt = Time.time + cooldown;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, launchSpeed);
            IceVFX.SpawnFrostBurst(transform.position, 14);
            if (visual != null) StartCoroutine(Pop());
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerBehavior>() == null) return;
        charge = 0f;
        if (visual != null) visual.localScale = visualBaseScale;
    }

    private IEnumerator Pop()
    {
        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            float k = 1f - t / 0.25f;
            visual.localScale = new Vector3(visualBaseScale.x * (1f - 0.1f * k), visualBaseScale.y * (1f + 0.35f * k), visualBaseScale.z);
            yield return null;
        }
        visual.localScale = visualBaseScale;
    }
}
