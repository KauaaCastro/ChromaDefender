using UnityEngine;

/// <summary>
/// Fundo animado (Animator com clipe de frames) que acompanha a câmera, com parallax e
/// escala para cobrir a altura da tela. Usado pelos prefabs de Assets/Prefabs/Fundos/Animados.
///
///   parallax = 0 → fica parado na tela (como um céu)
///   parallax = 1 → anda junto com o mundo
/// </summary>
[DefaultExecutionOrder(1000)] // depois do CameraFollow2D
[RequireComponent(typeof(SpriteRenderer))]
public class FundoAnimado : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [Range(0f, 1f)] [SerializeField] private float parallax = 0.1f;
    [Tooltip("Quanto a imagem fica maior que a altura da tela (folga para o movimento).")]
    [SerializeField] private float overscan = 1.2f;

    [Header("--- Limitar a uma área ---")]
    [Tooltip("Liga: o fundo só aparece quando a câmera está dentro da área abaixo.")]
    [SerializeField] private bool limitToArea = false;
    [Tooltip("Centro da área (objeto vazio na cena).")]
    [SerializeField] private Transform areaCenter;
    [Tooltip("Tamanho da área em unidades (largura x altura).")]
    [SerializeField] private Vector2 areaSize = new Vector2(20f, 10f);
    [Tooltip("Inverte: o fundo aparece FORA da área (use para esconder um fundo dentro da zona).")]
    [SerializeField] private bool invertArea = false;

    private Vector3 origin;
    private bool hasOrigin;
    private bool shown = true;

    private void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        FitToView();
    }

    private void FitToView()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;
        float viewHeight = (targetCamera != null && targetCamera.orthographic) ? targetCamera.orthographicSize * 2f : 10f;
        float k = viewHeight * overscan / sr.sprite.bounds.size.y;
        transform.localScale = new Vector3(k, k, 1f);
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null) return;
        }
        if (!hasOrigin)
        {
            origin = targetCamera.transform.position;
            hasOrigin = true;
        }
        Vector3 cam = targetCamera.transform.position;

        if (limitToArea && areaCenter != null)
        {
            Vector3 c = areaCenter.position;
            bool inside = Mathf.Abs(cam.x - c.x) <= areaSize.x * 0.5f && Mathf.Abs(cam.y - c.y) <= areaSize.y * 0.5f;
            bool show = inside != invertArea;
            if (show != shown)
            {
                shown = show;
                var sr = GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = show;
            }
        }

        Vector3 p = origin + (cam - origin) * (1f - parallax);
        transform.position = new Vector3(p.x, p.y, 0f);
    }
}
