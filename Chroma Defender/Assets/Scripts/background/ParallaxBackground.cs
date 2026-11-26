using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fundo em camadas com parallax infinito + animação ambiente (vento, partículas flutuando, névoa pulsando).
///
/// Como funciona:
///   - O objeto raiz acompanha a câmera a cada frame (depois do CameraFollow2D).
///   - Cada camada tem 3 cópias lado a lado e "dá a volta" quando sai da tela, então o fundo nunca acaba.
///   - parallax = 0 → a camada fica parada na tela (bem longe, ex.: céu).
///     parallax = 1 → a camada anda junto com o mundo (bem perto).
///   - autoScrollSpeed faz a camada andar sozinha (vento/névoa), mesmo com o jogador parado.
///   - bobAmplitude / alphaPulse dão vida às partículas e à névoa.
///
/// Monte pelo menu: Chroma Defender > Fundo > Adicionar Parallax Floresta na cena aberta.
/// </summary>
[DefaultExecutionOrder(1000)] // roda depois do LateUpdate do CameraFollow2D
public class ParallaxBackground : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "Camada";
        public Sprite sprite;

        [Tooltip("0 = parada na tela (longe), 1 = anda junto com o mundo (perto).")]
        [Range(0f, 1f)] public float parallax = 0.2f;

        [Tooltip("Velocidade de rolagem automática em unidades/seg (positivo = para a direita).")]
        public float autoScrollSpeed = 0f;

        [Tooltip("Flutuação vertical (unidades).")]
        public float bobAmplitude = 0f;
        public float bobSpeed = 0.5f;

        [Tooltip("Quanto a transparência oscila (0 = nada, 1 = some totalmente no mínimo).")]
        [Range(0f, 1f)] public float alphaPulse = 0f;
        public float alphaPulseSpeed = 0.4f;

        public Color tint = Color.white;
    }

    [Header("--- Câmera ---")]
    [SerializeField] private Camera targetCamera;
    [Tooltip("Quanto a imagem fica maior que a altura da tela (folga para o movimento vertical).")]
    [SerializeField] private float overscan = 1.2f;
    [Tooltip("Quanto da movimentação vertical da câmera vira parallax (0 = nenhum).")]
    [SerializeField] private float verticalFactor = 0.15f;
    [SerializeField] private float zPosition = 0f;

    [Header("--- Ordenação ---")]
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("Ordem da camada mais ao fundo; as próximas recebem +1, +2...")]
    [SerializeField] private int baseSortingOrder = -200;

    [Header("--- Camadas (do fundo para a frente) ---")]
    [SerializeField] private List<Layer> layers = new List<Layer>();

    private class Runtime
    {
        public Transform root;
        public SpriteRenderer[] tiles;
        public float width;
        public float phase;
    }

    private readonly List<Runtime> runtime = new List<Runtime>();
    private Vector2 origin;
    private float maxVerticalShift;

    public List<Layer> Layers => layers;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        Build();
        if (targetCamera != null) origin = targetCamera.transform.position;
    }

    /// <summary>(Re)cria os sprites de todas as camadas. Também é usado pelo editor para pré-visualizar.</summary>
    public void Build()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
        }
        runtime.Clear();

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        float viewHeight = (cam != null && cam.orthographic) ? cam.orthographicSize * 2f : 10f;
        maxVerticalShift = viewHeight * Mathf.Max(0f, overscan - 1f) * 0.5f;

        for (int i = 0; i < layers.Count; i++)
        {
            Layer layer = layers[i];
            if (layer == null || layer.sprite == null) continue;

            float scale = viewHeight * overscan / layer.sprite.bounds.size.y;
            var layerRoot = new GameObject(string.IsNullOrEmpty(layer.name) ? layer.sprite.name : layer.name).transform;
            layerRoot.SetParent(transform, false);
            layerRoot.localScale = new Vector3(scale, scale, 1f);

            var rt = new Runtime
            {
                root = layerRoot,
                tiles = new SpriteRenderer[3],
                width = layer.sprite.bounds.size.x * scale,
                phase = i * 1.37f // dessincroniza a animação entre camadas
            };

            for (int k = 0; k < 3; k++)
            {
                var tile = new GameObject("Tile_" + k).AddComponent<SpriteRenderer>();
                tile.transform.SetParent(layerRoot, false);
                tile.transform.localPosition = new Vector3((k - 1) * layer.sprite.bounds.size.x, 0f, 0f);
                tile.sprite = layer.sprite;
                tile.color = layer.tint;
                tile.sortingLayerName = sortingLayerName;
                tile.sortingOrder = baseSortingOrder + i;
                rt.tiles[k] = tile;
            }
            runtime.Add(rt);
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null) { targetCamera = Camera.main; if (targetCamera == null) return; }

        Vector3 camPos = targetCamera.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, zPosition);

        float t = Time.time;
        int r = 0;
        for (int i = 0; i < layers.Count && r < runtime.Count; i++)
        {
            Layer layer = layers[i];
            if (layer == null || layer.sprite == null) continue;
            Runtime rt = runtime[r++];

            // Horizontal: parallax + vento, com "volta" infinita
            float x = -(camPos.x - origin.x) * layer.parallax + layer.autoScrollSpeed * t;
            x = Mathf.Repeat(x + rt.width * 0.5f, rt.width) - rt.width * 0.5f;

            // Vertical: parallax suave limitado à folga + flutuação
            float y = -(camPos.y - origin.y) * layer.parallax * verticalFactor;
            y = Mathf.Clamp(y, -maxVerticalShift, maxVerticalShift);
            if (layer.bobAmplitude > 0f) y += Mathf.Sin((t + rt.phase) * layer.bobSpeed * Mathf.PI * 2f) * layer.bobAmplitude;

            rt.root.localPosition = new Vector3(x, y, 0f);

            if (layer.alphaPulse > 0f)
            {
                float k = 0.5f + 0.5f * Mathf.Sin((t + rt.phase) * layer.alphaPulseSpeed * Mathf.PI * 2f);
                Color c = layer.tint;
                c.a *= 1f - layer.alphaPulse * k;
                for (int j = 0; j < rt.tiles.Length; j++) rt.tiles[j].color = c;
            }
        }
    }
}
