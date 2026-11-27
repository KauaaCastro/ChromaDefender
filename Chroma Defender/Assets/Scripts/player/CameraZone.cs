using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Área retangular que limita a câmera. Enquanto o jogador estiver dentro dela, a câmera (CameraFollow2D)
/// nunca mostra nada fora do retângulo — assim o "void" além das bordas do mapa e abaixo do chão
/// (quedas) fica invisível. O retângulo é centrado neste objeto, com o tamanho dado em <see cref="size"/>.
/// </summary>
public class CameraZone : MonoBehaviour
{
    [Tooltip("Largura x altura da área visível (unidades do mundo), centrada neste objeto.")]
    [SerializeField] private Vector2 size = new Vector2(40f, 20f);
    [Tooltip("Zoom da câmera (meia-altura da vista) dentro desta área. 0 = mantém o zoom normal. Ex.: 7.5 numa sala de chefe gigante.")]
    [SerializeField] private float orthoSize = 0f;

    public float OrthoSize => orthoSize;

    private static readonly List<CameraZone> zones = new List<CameraZone>();

    public Rect Bounds => new Rect((Vector2)transform.position - size * 0.5f, size);

    private void OnEnable() { if (!zones.Contains(this)) zones.Add(this); }
    private void OnDisable() => zones.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => zones.Clear();

    /// <summary>Menor zona que contém o ponto (horizontalmente; na vertical aceita estar abaixo, caindo).</summary>
    public static CameraZone Find(Vector2 point)
    {
        CameraZone best = null;
        float bestArea = float.MaxValue;
        foreach (var z in zones)
        {
            if (z == null) continue;
            var r = z.Bounds;
            // Abaixo do retângulo ainda conta (jogador caindo num buraco): a câmera fica presa no fundo da área.
            bool inside = point.x >= r.xMin && point.x <= r.xMax && point.y <= r.yMax && point.y >= r.yMin - 30f;
            if (!inside) continue;
            float area = r.width * r.height;
            if (area < bestArea) { bestArea = area; best = z; }
        }
        return best;
    }

    /// <summary>Limita a posição da câmera para que a vista (meia-largura/meia-altura) fique dentro da zona.</summary>
    public Vector3 Clamp(Vector3 camPos, float halfWidth, float halfHeight)
    {
        var r = Bounds;
        camPos.x = r.width <= halfWidth * 2f ? r.center.x : Mathf.Clamp(camPos.x, r.xMin + halfWidth, r.xMax - halfWidth);
        camPos.y = r.height <= halfHeight * 2f ? r.center.y : Mathf.Clamp(camPos.y, r.yMin + halfHeight, r.yMax - halfHeight);
        return camPos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
        var r = Bounds;
        Gizmos.DrawWireCube(r.center, r.size);
    }
}
