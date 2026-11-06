using UnityEngine;

/// <summary>
/// Efeitos de Gelo 100% procedurais (sem depender de materiais/shaders dos packs de VFX, que não
/// renderizam bem no URP 2D). Gera o arco de corte gélido, estilhaços de gelo e o burst de impacto.
/// Tudo é desenhado acima dos sprites (sorting order alto) para sempre aparecer na frente do cenário.
/// </summary>
public static class IceVFX
{
    public const int SortingOrder = 30;

    public static readonly Color IceCore = new Color(0.88f, 0.98f, 1f, 1f);
    public static readonly Color IceMid = new Color(0.35f, 0.82f, 1f, 1f);
    public static readonly Color IceDeep = new Color(0.12f, 0.45f, 0.95f, 1f);

    private static Material shardMaterial;
    private static Material softMaterial;
    private static Texture2D shardTexture;
    private static Texture2D softTexture;
    private static Sprite arcSprite;

    // ------------------------------------------------------------------
    // Texturas e materiais gerados em código
    // ------------------------------------------------------------------
    private static Texture2D ShardTexture
    {
        get
        {
            if (shardTexture != null) return shardTexture;
            const int size = 32;
            shardTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c;
                    float ny = (y - c) / c;
                    float d = Mathf.Abs(nx) * 1.7f + Mathf.Abs(ny) * 0.75f; // losango alongado (cristal)
                    float a = Mathf.Clamp01(1f - d);
                    a = Mathf.Sqrt(a);
                    shardTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            shardTexture.Apply();
            return shardTexture;
        }
    }

    private static Texture2D SoftTexture
    {
        get
        {
            if (softTexture != null) return softTexture;
            const int size = 32;
            softTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            float c = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = (x - c) / c;
                    float ny = (y - c) / c;
                    float d = Mathf.Sqrt(nx * nx + ny * ny);
                    float a = Mathf.Clamp01(1f - d);
                    a *= a;
                    softTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            softTexture.Apply();
            return softTexture;
        }
    }

    private static Material CreateMaterial(Texture2D tex, string name)
    {
        Shader sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        var mat = new Material(sh) { name = name };
        mat.mainTexture = tex;
        return mat;
    }

    private static Sprite shardSprite;
    private static Sprite softSprite;

    /// <summary>Sprite de cristal (losango alongado) para uso em SpriteRenderers.</summary>
    public static Sprite ShardSprite
    {
        get
        {
            if (shardSprite == null)
                shardSprite = Sprite.Create(ShardTexture, new Rect(0, 0, ShardTexture.width, ShardTexture.height), new Vector2(0.5f, 0.5f), 32f);
            return shardSprite;
        }
    }

    /// <summary>Sprite de bolha suave (brilho) para uso em SpriteRenderers.</summary>
    public static Sprite SoftSprite
    {
        get
        {
            if (softSprite == null)
                softSprite = Sprite.Create(SoftTexture, new Rect(0, 0, SoftTexture.width, SoftTexture.height), new Vector2(0.5f, 0.5f), 32f);
            return softSprite;
        }
    }

    public static Material ShardMaterial
    {
        get
        {
            if (shardMaterial == null) shardMaterial = CreateMaterial(ShardTexture, "IceVFX_Shard");
            return shardMaterial;
        }
    }

    public static Material SoftMaterial
    {
        get
        {
            if (softMaterial == null) softMaterial = CreateMaterial(SoftTexture, "IceVFX_Soft");
            return softMaterial;
        }
    }

    /// <summary>Sprite de meia-lua (corte) apontando para a direita.</summary>
    private static Sprite ArcSprite => GetArcSprite(ref arcSprite, IceMid, IceCore);

    internal static Sprite GetArcSprite(ref Sprite cache, Color mid, Color core)
    {
        if (cache != null) return cache;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        float c = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / c;
                float ny = (y - c) / c;
                float dOuter = Mathf.Sqrt(nx * nx + ny * ny);
                float dInner = Mathf.Sqrt((nx + 0.42f) * (nx + 0.42f) + ny * ny);
                float outer = Mathf.Clamp01((0.96f - dOuter) / 0.07f);
                float inner = Mathf.Clamp01((dInner - 0.86f) / 0.10f);
                float a = outer * inner;
                float edge = Mathf.Clamp01((dOuter - 0.7f) / 0.26f);
                Color col = Color.Lerp(mid, core, edge);
                col.a = a;
                tex.SetPixel(x, y, col);
            }
        }
        tex.Apply();
        cache = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
        return cache;
    }

    // ------------------------------------------------------------------
    // Utilidades
    // ------------------------------------------------------------------
    /// <summary>
    /// Empurra todos os renderers de um VFX de pack para a frente (sorting order padrão 0 fica atrás dos sprites).
    /// </summary>
    public static void BringToFront(GameObject vfx, int minOrder = SortingOrder)
    {
        if (vfx == null) return;
        foreach (var r in vfx.GetComponentsInChildren<Renderer>(true))
        {
            if (r.sortingOrder < minOrder) r.sortingOrder += minOrder;
        }
    }

    internal static ParticleSystem NewParticleSystem(string name, Vector3 pos, Quaternion rot, Material mat, int order)
    {
        var go = new GameObject(name);
        go.transform.SetPositionAndRotation(pos, rot);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.sortingOrder = order;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        return ps;
    }

    internal static Gradient FadeGradient(Color a, Color b)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(a, 0f), new GradientColorKey(b, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    // ------------------------------------------------------------------
    // Efeitos
    // ------------------------------------------------------------------
    /// <summary>Arco de corte gélido (meia-lua) que cresce e some rapidamente.</summary>
    public static void SpawnSlashArc(Vector3 pos, float facing, float angleDeg, float scale = 1.4f)
    {
        var go = new GameObject("IceSlashArc");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg * facing);
        go.transform.localScale = new Vector3(scale * facing, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = ArcSprite;
        sr.sortingOrder = SortingOrder + 1;
        go.AddComponent<IceSlashArc>().Init(scale);
    }

    /// <summary>Estilhaços de gelo disparados em cone na direção do golpe.</summary>
    public static void SpawnSlashShards(Vector3 pos, float facing, int count = 14)
    {
        var rot = Quaternion.LookRotation(new Vector3(facing, 0f, 0f), Vector3.up);
        var ps = NewParticleSystem("IceSlashShards", pos, rot, ShardMaterial, SortingOrder + 2);

        var main = ps.main;
        main.loop = false;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(IceCore, IceMid);
        main.gravityModifier = 0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.12f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGradient(Color.white, IceDeep);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ps.Play();
    }

    /// <summary>Burst de cristais + flash congelante no ponto de impacto contra o alvo.</summary>
    public static void SpawnFrostBurst(Vector3 pos, int count = 20)
    {
        // Cristais radiais
        var ps = NewParticleSystem("IceHitShards", pos, Quaternion.identity, ShardMaterial, SortingOrder + 3);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.32f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(IceCore, IceMid);
        main.gravityModifier = 0.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.15f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FadeGradient(Color.white, IceDeep);

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));
        ps.Play();

        // Flash/halo congelante
        var flash = NewParticleSystem("IceHitFlash", pos, Quaternion.identity, SoftMaterial, SortingOrder + 2);
        var fm = flash.main;
        fm.loop = false;
        fm.duration = 0.3f;
        fm.startLifetime = 0.22f;
        fm.startSpeed = 0f;
        fm.startSize = 1.5f;
        fm.startColor = new Color(0.55f, 0.9f, 1f, 0.85f);
        fm.simulationSpace = ParticleSystemSimulationSpace.World;
        fm.stopAction = ParticleSystemStopAction.Destroy;
        fm.playOnAwake = false;

        var fe = flash.emission;
        fe.rateOverTime = 0f;
        fe.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)1) });

        var fs = flash.shape;
        fs.enabled = false;

        var fSize = flash.sizeOverLifetime;
        fSize.enabled = true;
        fSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.2f));

        var fCol = flash.colorOverLifetime;
        fCol.enabled = true;
        fCol.color = FadeGradient(Color.white, IceMid);
        flash.Play();
    }
}

/// <summary>Anima o arco de corte gélido: cresce, brilha e desaparece.</summary>
public class IceSlashArc : MonoBehaviour
{
    private const float Life = 0.26f;
    private float t;
    private float baseScale = 1.4f;
    private SpriteRenderer sr;

    public void Init(float scale)
    {
        baseScale = scale;
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        t += Time.deltaTime;
        float k = Mathf.Clamp01(t / Life);

        float s = baseScale * Mathf.Lerp(0.7f, 1.25f, 1f - (1f - k) * (1f - k));
        float sign = Mathf.Sign(transform.localScale.x);
        transform.localScale = new Vector3(s * sign, s, 1f);

        if (sr != null)
        {
            var c = Color.white;
            c.a = 1f - k * k;
            sr.color = c;
        }

        if (t >= Life) Destroy(gameObject);
    }
}
