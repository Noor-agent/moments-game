using UnityEngine;

/// <summary>
/// Procedural VFX factory. Creates all 9 VFX particle systems at runtime.
/// Called by VFXManager.Awake() when no prefabs are assigned in the inspector —
/// ensures the game always has working VFX even without art assets.
///
/// Effect catalogue:
///   HitSpark     — sharp burst of lit shards, player-color tinted
///   DashTrail    — ribbon stream along movement direction
///   ScoreBurst   — golden ring pulse + pop particles
///   Confetti     — multi-color falling celebration ribbons
///   Explosion    — fiery shockwave + debris chunks
///   PaintSplat   — radial flat splatter, player-color
///   IceCrack     — ice shard burst, cold blue
///   ElimFlash    — single-frame flash + ring shock
///   JoinPing     — soft glow ring expand (lobby)
/// </summary>
public static class VFXBuilder
{
    // ── Public factory ─────────────────────────────────────────────────────

    public static GameObject Build(VFXManager.VFXType type, Transform parent)
    {
        var go = new GameObject($"VFX_{type}");
        go.transform.SetParent(parent);
        go.SetActive(false);

        switch (type)
        {
            case VFXManager.VFXType.HitSpark:    BuildHitSpark(go);    break;
            case VFXManager.VFXType.DashTrail:   BuildDashTrail(go);   break;
            case VFXManager.VFXType.ScoreBurst:  BuildScoreBurst(go);  break;
            case VFXManager.VFXType.Confetti:    BuildConfetti(go);    break;
            case VFXManager.VFXType.Explosion:   BuildExplosion(go);   break;
            case VFXManager.VFXType.PaintSplat:  BuildPaintSplat(go);  break;
            case VFXManager.VFXType.IceCrack:    BuildIceCrack(go);    break;
            case VFXManager.VFXType.ElimFlash:   BuildElimFlash(go);   break;
            case VFXManager.VFXType.JoinPing:    BuildJoinPing(go);    break;
        }
        return go;
    }

    // ── HitSpark ──────────────────────────────────────────────────────────

    private static void BuildHitSpark(GameObject go)
    {
        // Main burst
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.04f, ParticleSystemRenderMode.Stretch);

        WithMain(ps, duration:0.15f, looping:false, lifetime:(0.2f,0.6f),
            speed:(4f,14f), size:(0.04f,0.12f), gravity:0.5f, count:24,
            shape: ParticleSystemShapeType.Sphere, shapeRadius:0.05f);
        WithColorOverLife(ps, new Color(1f,0.9f,0.5f,1f), new Color(1f,0.3f,0.1f,0f));

        // Tiny sparkle child
        var spark = AddChild("Sparkle", go.transform);
        var sps   = spark.AddComponent<ParticleSystem>();
        WithMain(sps, duration:0.1f, looping:false, lifetime:(0.1f,0.3f),
            speed:(8f,20f), size:(0.01f,0.03f), gravity:0f, count:10,
            shape: ParticleSystemShapeType.Cone, shapeRadius:0.02f);
        WithColorOverLife(sps, Color.white, new Color(1f,1f,0.8f,0f));
        SetAdditiveRenderer(spark.GetComponent<ParticleSystemRenderer>(), 0.02f, ParticleSystemRenderMode.Stretch);
    }

    // ── DashTrail ─────────────────────────────────────────────────────────

    private static void BuildDashTrail(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.08f, ParticleSystemRenderMode.Stretch);

        WithMain(ps, duration:0.25f, looping:false, lifetime:(0.15f,0.4f),
            speed:(0f, 1f), size:(0.05f, 0.15f), gravity:-0.5f, count:0,
            shape: ParticleSystemShapeType.Cone, shapeRadius:0.25f);

        // Emission over time (spawns while dashing, not burst)
        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 40f;
        em.SetBursts(new ParticleSystem.Burst[0]);

        WithColorOverLife(ps, new Color(0.6f,0.9f,1f,0.8f), new Color(0.2f,0.6f,1f,0f));

        // Stretch along velocity
        r.velocityScale  = 0.3f;
        r.lengthScale    = 2f;
    }

    // ── ScoreBurst ────────────────────────────────────────────────────────

    private static void BuildScoreBurst(GameObject go)
    {
        // Ring expand
        var ring = AddChild("Ring", go.transform);
        var rps  = ring.AddComponent<ParticleSystem>();
        SetAdditiveRenderer(ring.GetComponent<ParticleSystemRenderer>(), 0.1f,
            ParticleSystemRenderMode.HorizontalBillboard);
        WithMain(rps, duration:0.1f, looping:false, lifetime:(0.4f,0.6f),
            speed:(2f,5f), size:(0.06f,0.1f), gravity:0f, count:32,
            shape: ParticleSystemShapeType.Circle, shapeRadius:0.3f);
        WithColorOverLife(rps, new Color(1f,0.85f,0.1f,1f), new Color(1f,0.6f,0.05f,0f));

        // Stars pop up
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.05f, ParticleSystemRenderMode.Billboard);
        WithMain(ps, duration:0.15f, looping:false, lifetime:(0.5f,0.9f),
            speed:(3f,8f), size:(0.08f,0.18f), gravity:-1.5f, count:12,
            shape: ParticleSystemShapeType.Sphere, shapeRadius:0.1f);
        WithColorOverLife(ps, new Color(1f,1f,0.5f,1f), new Color(1f,0.8f,0f,0f));
    }

    // ── Confetti ──────────────────────────────────────────────────────────

    private static void BuildConfetti(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Mesh;
        // Use a small quad as mesh if available, otherwise billboard
        r.renderMode = ParticleSystemRenderMode.Billboard;
        SetAdditiveRenderer(r, 0.12f, ParticleSystemRenderMode.Billboard);

        WithMain(ps, duration:0.3f, looping:false, lifetime:(1.8f,3.0f),
            speed:(2f, 9f), size:(0.06f, 0.14f), gravity:1.2f, count:80,
            shape: ParticleSystemShapeType.Cone, shapeRadius:1.0f);

        // Random hue gradient
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[]
            {
                new(new Color(1f,0.2f,0.5f), 0f),
                new(new Color(0.2f,1f,0.4f), 0.33f),
                new(new Color(0.3f,0.5f,1f), 0.66f),
                new(new Color(1f,0.9f,0f),   1f),
            },
            new GradientAlphaKey[] { new(1f,0f), new(1f,0.7f), new(0f,1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);

        // Rotation
        var rot = ps.rotationOverLifetime;
        rot.enabled   = true;
        rot.z         = new ParticleSystem.MinMaxCurve(-360f, 360f);
        rot.separateAxes = false;

        // Turbulence
        var noise = ps.noise;
        noise.enabled   = true;
        noise.strength  = 0.6f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.2f;
    }

    // ── Explosion ─────────────────────────────────────────────────────────

    private static void BuildExplosion(GameObject go)
    {
        // Flash
        var flash = AddChild("Flash", go.transform);
        var fps   = flash.AddComponent<ParticleSystem>();
        SetAdditiveRenderer(flash.GetComponent<ParticleSystemRenderer>(), 1.2f,
            ParticleSystemRenderMode.Billboard);
        WithMain(fps, duration:0.05f, looping:false, lifetime:(0.08f,0.12f),
            speed:(0f,0f), size:(1.5f,2.5f), gravity:0f, count:1,
            shape: ParticleSystemShapeType.Sphere, shapeRadius:0.01f);
        WithColorOverLife(fps, new Color(1f,1f,0.8f,0.9f), new Color(1f,0.6f,0f,0f));

        // Fireball core
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.4f, ParticleSystemRenderMode.Billboard);
        WithMain(ps, duration:0.2f, looping:false, lifetime:(0.4f,0.9f),
            speed:(1f,6f), size:(0.2f,0.6f), gravity:-0.3f, count:40,
            shape: ParticleSystemShapeType.Sphere, shapeRadius:0.2f);
        WithColorOverLife(ps, new Color(1f,0.7f,0.1f,1f), new Color(0.5f,0.1f,0f,0f));

        // Debris shards
        var shards = AddChild("Shards", go.transform);
        var sps    = shards.AddComponent<ParticleSystem>();
        SetAdditiveRenderer(shards.GetComponent<ParticleSystemRenderer>(), 0.08f,
            ParticleSystemRenderMode.Stretch);
        WithMain(sps, duration:0.1f, looping:false, lifetime:(0.5f,1.2f),
            speed:(5f,18f), size:(0.03f,0.09f), gravity:2f, count:20,
            shape: ParticleSystemShapeType.Sphere, shapeRadius:0.05f);
        WithColorOverLife(sps, new Color(0.8f,0.5f,0.1f,1f), new Color(0.2f,0.1f,0f,0f));
    }

    // ── PaintSplat ────────────────────────────────────────────────────────

    private static void BuildPaintSplat(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.25f, ParticleSystemRenderMode.HorizontalBillboard);

        var main = ps.main;
        main.duration    = 0.2f;
        main.loop        = false;
        main.startLifetime  = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSpeed     = new ParticleSystem.MinMaxCurve(0.5f, 3f);
        main.startSize      = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startColor     = new ParticleSystem.MinMaxGradient(new Color(1f,0.3f,0.1f), new Color(0.8f,0.1f,0.8f));
        main.gravityModifierMultiplier = 0f;

        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0, 28) });

        var shape = ps.shape;
        shape.enabled     = true;
        shape.shapeType   = ParticleSystemShapeType.Cone;
        shape.angle       = 60f;
        shape.radius      = 0.15f;
        shape.rotation    = new Vector3(90f, 0, 0); // fire downward

        WithColorOverLife(ps, Color.white, new Color(1f,1f,1f,0f));
    }

    // ── IceCrack ──────────────────────────────────────────────────────────

    private static void BuildIceCrack(GameObject go)
    {
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.08f, ParticleSystemRenderMode.Stretch);

        WithMain(ps, duration:0.15f, looping:false, lifetime:(0.3f,0.8f),
            speed:(3f,10f), size:(0.02f,0.08f), gravity:1f, count:20,
            shape: ParticleSystemShapeType.Circle, shapeRadius:0.05f);
        WithColorOverLife(ps, new Color(0.7f,0.9f,1f,1f), new Color(0.3f,0.7f,1f,0f));

        r.velocityScale = 0.4f;
        r.lengthScale   = 2.5f;
    }

    // ── ElimFlash ─────────────────────────────────────────────────────────

    private static void BuildElimFlash(GameObject go)
    {
        // Full-screen-ish ring
        var ring = AddChild("Ring", go.transform);
        var rps  = ring.AddComponent<ParticleSystem>();
        SetAdditiveRenderer(ring.GetComponent<ParticleSystemRenderer>(), 0.6f,
            ParticleSystemRenderMode.Billboard);
        WithMain(rps, duration:0.05f, looping:false, lifetime:(0.5f,0.7f),
            speed:(3f,8f), size:(0.3f,0.8f), gravity:0f, count:18,
            shape: ParticleSystemShapeType.Circle, shapeRadius:0.4f);
        WithColorOverLife(rps, Color.white, new Color(1f,1f,1f,0f));

        // Grey out shards
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.12f, ParticleSystemRenderMode.Stretch);
        WithMain(ps, duration:0.1f, looping:false, lifetime:(0.5f,1.0f),
            speed:(2f,9f), size:(0.04f,0.1f), gravity:1.5f, count:30,
            shape: ParticleSystemShapeType.Sphere, shapeRadius:0.3f);
        WithColorOverLife(ps, new Color(0.6f,0.6f,0.6f,1f), new Color(0.3f,0.3f,0.3f,0f));
    }

    // ── JoinPing ──────────────────────────────────────────────────────────

    private static void BuildJoinPing(GameObject go)
    {
        // Soft expanding ring for lobby player-join glow
        var ps = go.AddComponent<ParticleSystem>();
        var r  = go.GetComponent<ParticleSystemRenderer>();
        SetAdditiveRenderer(r, 0.3f, ParticleSystemRenderMode.HorizontalBillboard);

        WithMain(ps, duration:0.1f, looping:false, lifetime:(0.8f,1.2f),
            speed:(1f,3f), size:(0.1f,0.4f), gravity:0f, count:24,
            shape: ParticleSystemShapeType.Circle, shapeRadius:0.2f);
        WithColorOverLife(ps, new Color(0.4f,1f,0.8f,1f), new Color(0.2f,0.8f,1f,0f));

        // Floating sparkles
        var sparks = AddChild("Sparks", go.transform);
        var sps    = sparks.AddComponent<ParticleSystem>();
        WithMain(sps, duration:0.3f, looping:false, lifetime:(0.8f,1.8f),
            speed:(0.3f,1.5f), size:(0.04f,0.1f), gravity:-0.5f, count:12,
            shape: ParticleSystemShapeType.Sphere, shapeRadius:0.3f);
        SetAdditiveRenderer(sparks.GetComponent<ParticleSystemRenderer>(), 0.07f,
            ParticleSystemRenderMode.Billboard);
        WithColorOverLife(sps, Color.white, new Color(0.6f,1f,0.8f,0f));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void WithMain(ParticleSystem ps,
        float duration, bool looping,
        (float min, float max) lifetime,
        (float min, float max) speed,
        (float min, float max) size,
        float gravity, int count,
        ParticleSystemShapeType shape, float shapeRadius)
    {
        var main = ps.main;
        main.duration     = duration;
        main.loop         = looping;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.min, lifetime.max);
        main.startSpeed   = new ParticleSystem.MinMaxCurve(speed.min, speed.max);
        main.startSize    = new ParticleSystem.MinMaxCurve(size.min, size.max);
        main.gravityModifierMultiplier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var em = ps.emission;
        em.enabled = true;
        em.rateOverTime = 0;
        em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });

        var sh = ps.shape;
        sh.enabled    = true;
        sh.shapeType  = shape;
        sh.radius     = shapeRadius;
    }

    private static void WithColorOverLife(ParticleSystem ps, Color start, Color end)
    {
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(
            new[] { new GradientColorKey(start, 0f), new GradientColorKey(end, 1f) },
            new[] { new GradientAlphaKey(start.a, 0f), new GradientAlphaKey(end.a, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(grad);
    }

    private static void SetAdditiveRenderer(ParticleSystemRenderer r, float size,
        ParticleSystemRenderMode mode)
    {
        r.renderMode  = mode;
        r.minParticleSize = 0f;
        r.maxParticleSize = size;
        // Material will be assigned by VFXManager from shared particleMat slot
        // Fallback: Unity default particle material (sprites-default)
    }

    private static GameObject AddChild(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        return go;
    }
}
