using UnityEngine;

/// <summary>
/// Cristais de gelo ou chamas orbitando EM VOLTA do personagem enquanto a polaridade está ativa.
/// Elipse com profundidade falsa: quando o item passa "atrás" do corpo vai para trás do sprite.
/// </summary>
public class ElementOrbitAura : MonoBehaviour
{
    public enum Kind { Ice, Fire }

    private Kind kind;
    private SpriteRenderer[] items;
    private float[] phases;
    private float[] scales;
    private int frontOrder;
    private int behindOrder;

    [SerializeField] private float heightOffset = 0.05f; // Nível da cintura/cinto
    [SerializeField] private float radiusX = 0.62f;
    [SerializeField] private float radiusY = 0.14f; // Anel horizontal rente à cintura (nunca sobe para a cabeça)
    [SerializeField] private float speed = 1.6f;

    public bool IsSetup => items != null;

    public void Setup(Kind k, int knightSortingOrder, int count = 5)
    {
        if (items != null) return;

        kind = k;
        frontOrder = Mathf.Max(IceVFX.SortingOrder, knightSortingOrder + 3);
        behindOrder = knightSortingOrder - 1;

        if (kind == Kind.Fire) { speed = 1.9f; }

        items = new SpriteRenderer[count];
        phases = new float[count];
        scales = new float[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject(kind == Kind.Ice ? "OrbitCrystal" : "OrbitFlame");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = kind == Kind.Ice ? IceVFX.ShardSprite : IceVFX.SoftSprite;
            sr.sharedMaterial = kind == Kind.Ice ? IceVFX.ShardMaterial : IceVFX.SoftMaterial;
            items[i] = sr;
            phases[i] = (Mathf.PI * 2f / count) * i;
            scales[i] = kind == Kind.Ice ? Random.Range(0.2f, 0.3f) : Random.Range(0.14f, 0.22f);
        }
    }

    private void Update()
    {
        if (items == null) return;

        float t = Time.time;
        for (int i = 0; i < items.Length; i++)
        {
            float ang = t * speed + phases[i];
            float cos = Mathf.Cos(ang);
            float sin = Mathf.Sin(ang);

            var tr = items[i].transform;
            // Órbita estritamente horizontal na cintura: não sobe para a parte de cima do boneco
            tr.localPosition = new Vector3(cos * radiusX, heightOffset + (sin * radiusY), 0f);
            items[i].sortingOrder = sin > 0f ? behindOrder : frontOrder;

            float depth = sin > 0f ? 0.75f : 1f; // itens "atrás" ficam menores/mais escuros

            if (kind == Kind.Ice)
            {
                // Cristal alinhado horizontalmente na órbita da cintura
                float tangent = Mathf.Atan2(cos * radiusY, -sin * radiusX) * Mathf.Rad2Deg;
                tr.localRotation = Quaternion.Euler(0f, 0f, tangent + 90f);
                tr.localScale = Vector3.one * scales[i] * depth;
                float pulse = 0.75f + 0.25f * Mathf.Sin(t * 5f + phases[i]);
                var c = Color.Lerp(IceVFX.IceMid, IceVFX.IceCore, pulse);
                c.a = sin > 0f ? 0.7f : 1f;
                items[i].color = c;
            }
            else
            {
                // Chama tremula (flicker) alternando entre amarelo e laranja
                float flicker = 0.75f + 0.25f * Mathf.Sin(t * 14f + phases[i] * 3f) + Random.Range(-0.08f, 0.08f);
                tr.localRotation = Quaternion.identity;
                tr.localScale = Vector3.one * scales[i] * depth * flicker * 1.3f;
                var c = Color.Lerp(FireVFX.FireMid, FireVFX.FireCore, flicker - 0.4f);
                c.a = sin > 0f ? 0.65f : 0.95f;
                items[i].color = c;
            }
        }
    }
}
