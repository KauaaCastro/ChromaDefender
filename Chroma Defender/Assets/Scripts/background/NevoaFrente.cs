using UnityEngine;

/// <summary>
/// Névoa da frente (sobreposição). Três cópias do sprite lado a lado que rolam
/// horizontalmente em loop e têm a opacidade oscilando suavemente.
///
/// Independente do ParallaxBackground: funciona só onde o objeto estiver na cena.
/// Estrutura (criada pelo menu Chroma Defender → Fundos → Criar Prefab Névoa do Labirinto):
///   Nevoa_Labirinto (este script)
///    ├─ Tile_0 (SpriteRenderer)
///    ├─ Tile_1 (SpriteRenderer)
///    └─ Tile_2 (SpriteRenderer)
/// </summary>
[ExecuteAlways]
public class NevoaFrente : MonoBehaviour
{
    [Tooltip("Largura de um tile em unidades (o sprite 10_Mist tem 19,2).")]
    [SerializeField] private float tileWidth = 19.2f;

    [Tooltip("Velocidade de rolagem em unidades/seg (positivo = para a direita).")]
    [SerializeField] private float scrollSpeed = 0.45f;

    [Tooltip("Opacidade mínima e máxima da oscilação.")]
    [SerializeField, Range(0f, 1f)] private float alphaMin = 0.55f;
    [SerializeField, Range(0f, 1f)] private float alphaMax = 1f;
    [SerializeField] private float alphaSpeed = 0.1f;

    [Tooltip("Flutuação vertical (unidades). 0 = parada.")]
    [SerializeField] private float bobAmplitude = 0.06f;
    [SerializeField] private float bobSpeed = 0.05f;

    [Tooltip("Cor base dos tiles (multiplica o sprite).")]
    [SerializeField] private Color tint = Color.white;

    private SpriteRenderer[] tiles;

    public float TileWidth => tileWidth;

    private void OnEnable() => CacheTiles();

    private void CacheTiles()
    {
        tiles = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void Update()
    {
        if (tiles == null || tiles.Length == 0) CacheTiles();
        if (tiles == null) return;

        float t = Application.isPlaying ? Time.time : 0f;

        // Rolagem: 3 tiles centrados em -W, 0 e +W, deslocados por "off" (0..W) → sempre cobrem o centro.
        float off = Mathf.Repeat(t * scrollSpeed, tileWidth);
        float bob = bobAmplitude > 0f ? Mathf.Sin(t * bobSpeed * Mathf.PI * 2f) * bobAmplitude : 0f;

        float alpha = Mathf.Lerp(alphaMin, alphaMax, 0.5f + 0.5f * Mathf.Sin(t * alphaSpeed * Mathf.PI * 2f));
        Color c = new Color(tint.r, tint.g, tint.b, tint.a * alpha);

        for (int i = 0; i < tiles.Length && i < 3; i++)
        {
            var sr = tiles[i];
            if (sr == null) continue;
            if (sr.transform == transform) continue;
            sr.transform.localPosition = new Vector3((i - 1) * tileWidth - off, bob, 0f);
            sr.color = c;
        }
    }
}
