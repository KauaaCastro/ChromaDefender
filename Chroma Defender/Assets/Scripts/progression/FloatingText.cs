using UnityEngine;

/// <summary>
/// Texto curto que sobe e some (ex.: "+12" de vida ao derrotar um mob).
/// Uso: FloatingText.Spawn(posição, "+12", cor);
/// </summary>
public class FloatingText : MonoBehaviour
{
    private const float Duration = 0.9f;
    private const float RiseSpeed = 1.4f;

    private TextMesh text;
    private Color baseColor;
    private float elapsed;

    public static void Spawn(Vector3 position, string message, Color color)
    {
        var go = new GameObject("FloatingText");
        go.transform.position = position;

        var tm = go.AddComponent<TextMesh>();
        tm.text = message;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.07f;
        tm.fontSize = 36;
        tm.fontStyle = FontStyle.Bold;
        tm.color = color;

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = 60;

        var ft = go.AddComponent<FloatingText>();
        ft.text = tm;
        ft.baseColor = color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        transform.position += Vector3.up * (RiseSpeed * Time.deltaTime);

        float t = elapsed / Duration;
        if (text != null)
        {
            Color c = baseColor;
            c.a = 1f - Mathf.Clamp01((t - 0.5f) * 2f); // fica opaco na primeira metade e some na segunda
            text.color = c;
        }

        if (t >= 1f) Destroy(gameObject);
    }
}
