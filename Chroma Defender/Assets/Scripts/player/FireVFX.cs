using UnityEngine;

/// <summary>
/// Efeitos de Fogo procedurais (mesma base do IceVFX): arco de corte flamejante, brasas e burst de impacto.
/// </summary>
public static class FireVFX
{
    public static readonly Color FireCore = new Color(1f, 0.96f, 0.7f, 1f);
    public static readonly Color FireMid = new Color(1f, 0.55f, 0.1f, 1f);
    public static readonly Color FireDeep = new Color(0.85f, 0.12f, 0.02f, 1f);

    private static Sprite arcSprite;

    private static Gradient FlameGradient()
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(FireCore, 0f), new GradientColorKey(FireMid, 0.4f), new GradientColorKey(FireDeep, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.5f), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    /// <summary>Arco de corte flamejante.</summary>
    public static void SpawnSlashArc(Vector3 pos, float facing, float angleDeg, float scale = 1.4f)
    {
        var go = new GameObject("FireSlashArc");
        go.transform.position = pos;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg * facing);
        go.transform.localScale = new Vector3(scale * facing, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = IceVFX.GetArcSprite(ref arcSprite, FireMid, FireCore);
        sr.sortingOrder = IceVFX.SortingOrder + 1;
        go.AddComponent<IceSlashArc>().Init(scale);
    }

    /// <summary>Brasas e faíscas disparadas em cone na direção do golpe, subindo com o calor.</summary>
    public static void SpawnSlashEmbers(Vector3 pos, float facing, int count = 16)
    {
        var rot = Quaternion.LookRotation(new Vector3(facing, 0f, 0f), Vector3.up);
        var ps = IceVFX.NewParticleSystem("FireSlashEmbers", pos, rot, IceVFX.SoftMaterial, IceVFX.SortingOrder + 2);

        var main = ps.main;
        main.loop = false;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(FireCore, FireMid);
        main.gravityModifier = -0.25f; // brasas sobem
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        main.playOnAwake = false;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 32f;
        shape.radius = 0.12f;

        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = FlameGradient();

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));

        ps.Play();
    }

    /// <summary>Explosão de chamas no ponto de impacto contra o alvo.</summary>
    public static void SpawnFlameBurst(Vector3 pos, int count = 22)
    {
        // Brasas radiais
        var ps = IceVFX.NewParticleSystem("FireHitEmbers", pos, Quaternion.identity, IceVFX.SoftMaterial, IceVFX.SortingOrder + 3);
        var main = ps.main;
        main.loop = false;
        main.duration = 0.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.26f);
        main.startColor = new ParticleSystem.MinMaxGradient(FireCore, FireMid);
        main.gravityModifier = -0.35f;
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
        col.color = FlameGradient();

        var size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
        ps.Play();

        // Bola de fogo / flash de calor
        var flash = IceVFX.NewParticleSystem("FireHitFlash", pos, Quaternion.identity, IceVFX.SoftMaterial, IceVFX.SortingOrder + 2);
        var fm = flash.main;
        fm.loop = false;
        fm.duration = 0.3f;
        fm.startLifetime = 0.28f;
        fm.startSpeed = 0f;
        fm.startSize = 1.6f;
        fm.startColor = new Color(1f, 0.6f, 0.15f, 0.9f);
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
        fSize.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.3f));

        var fCol = flash.colorOverLifetime;
        fCol.enabled = true;
        fCol.color = FlameGradient();
        flash.Play();
    }
}
