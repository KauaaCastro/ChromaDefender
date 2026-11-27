using System.Collections;
using UnityEngine;

/// <summary>
/// Caixote empurrável que volta ao lugar de origem se cair num buraco/poço
/// (ou for empurrado para longe demais), para o jogador nunca ficar sem apoio.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class CrateRespawn : MonoBehaviour
{
    [Tooltip("Se o caixote ficar mais baixo que a posição inicial por esta distância, ele volta.")]
    [SerializeField] private float fallResetDistance = 2.5f;
    [Tooltip("Distância horizontal máxima da origem (0 = sem limite).")]
    [SerializeField] private float maxHorizontalDistance = 0f;
    [SerializeField] private float fadeTime = 0.35f;

    private Vector3 home;
    private Rigidbody2D rb;
    private SpriteRenderer[] renderers;
    private bool resetting;

    private void Awake()
    {
        home = transform.position;
        rb = GetComponent<Rigidbody2D>();
        renderers = GetComponentsInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (resetting) return;
        bool fell = transform.position.y < home.y - fallResetDistance;
        bool strayed = maxHorizontalDistance > 0f && Mathf.Abs(transform.position.x - home.x) > maxHorizontalDistance;
        if (fell || strayed) StartCoroutine(ResetRoutine());
    }

    private IEnumerator ResetRoutine()
    {
        resetting = true;
        yield return Fade(1f, 0f);
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.position = home;
        transform.position = home;
        yield return Fade(0f, 1f);
        resetting = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeTime; t += Time.deltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / fadeTime));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        foreach (var sr in renderers)
        {
            if (sr == null) continue;
            var c = sr.color; c.a = a; sr.color = c;
        }
    }
}
