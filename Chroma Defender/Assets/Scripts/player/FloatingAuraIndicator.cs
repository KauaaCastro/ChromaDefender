using UnityEngine;

/// <summary>
/// Efeito de flutuação suave (bobbing) e leve pulsar para ícones e auras elementais.
/// </summary>
public class FloatingAuraIndicator : MonoBehaviour
{
    [SerializeField] private float bobSpeed = 2.8f;
    [SerializeField] private float bobHeight = 0.05f;
    [SerializeField] private float scalePulseSpeed = 2f;
    [SerializeField] private float scalePulseAmount = 0.04f;

    private Vector3 initialLocalPos;
    private Vector3 initialScale;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialScale = transform.localScale;
    }

    private void Update()
    {
        float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.localPosition = initialLocalPos + new Vector3(0f, yOffset, 0f);

        float scaleOffset = 1f + (Mathf.Sin(Time.time * scalePulseSpeed) * scalePulseAmount);
        transform.localScale = initialScale * scaleOffset;
    }
}
