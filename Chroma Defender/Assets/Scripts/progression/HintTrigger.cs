using UnityEngine;

/// <summary>
/// Mostra uma dica na tela quando o cavaleiro entra na área (placas, bifurcação, avisos).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class HintTrigger : MonoBehaviour
{
    [SerializeField, TextArea] private string message;
    [SerializeField] private float duration = 4f;
    [SerializeField] private bool onlyOnce = false;
    [SerializeField] private float repeatCooldown = 8f;

    private bool shown;
    private float nextTime;

    public void Configure(string msg, float dur = 4f, bool once = false)
    {
        message = msg;
        duration = dur;
        onlyOnce = once;
    }

    private void Start()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerBehavior>() == null) return;
        if (onlyOnce && shown) return;
        if (Time.time < nextTime) return;

        shown = true;
        nextTime = Time.time + repeatCooldown;
        ProgressHud.Toast(message, duration);
    }
}
