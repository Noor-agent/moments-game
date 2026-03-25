using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds procedural character geometry for all 8 Moments heroes.
/// Each hero gets a unique silhouette, proportions, and surface detail
/// assembled from shared mesh primitives — no external meshes required.
///
/// Called by CharacterVisuals.Initialize() when instantiating a hero.
/// The resulting GameObject has: body, head, accent parts, name billboard.
///
/// Visual identity per hero:
///   Byte    — compact, rounded, tech-visor — cyan
///   Nova    — tall antenna, goggles, lab coat silhouette — gold
///   Orbit   — helmet dome, jetpack lumps — teal
///   Striker — broad shoulders, knee pads, cleats — lime
///   Sizzle  — chef hat bump, apron belly, ladle arm — orange
///   Shade   — slim, hooded, offset eye slit — purple
///   Dusty   — wide brim hat, poncho silhouette — brown
///   Pop     — quad roller skate wheels at feet, small & fast — pink
/// </summary>
public static class CharacterArtBuilder
{
    // ── Hero definitions ──────────────────────────────────────────────────────

    private struct HeroSpec
    {
        public float bodyRx, bodyRy, bodyRz;    // Body superellipsoid radii
        public float bodyN1, bodyN2;             // Squareness (0.5=round, 1=square)
        public float headRadius;
        public float headY;                      // Head Y offset from body centre
        public float neckRadius;
        public AccentType[] accents;
    }

    private enum AccentType
    {
        TechVisor,      // Byte — horizontal glowing band across head
        Antenna,        // Nova — thin spike + ball tip
        JetpackLumps,   // Orbit — two cylinders on back
        KneePads,       // Striker — flat discs on lower body
        ChefHat,        // Sizzle — tall white dome on head
        Hood,           // Shade — ovoid shadow over head
        WideBrimHat,    // Dusty — disc above head
        RollerSkates,   // Pop — four small spheres at foot level
    }

    private static readonly Dictionary<string, HeroSpec> _specs = new()
    {
        ["byte"]    = new HeroSpec { bodyRx=0.40f, bodyRy=0.75f, bodyRz=0.38f, bodyN1=0.5f, bodyN2=0.5f, headRadius=0.32f, headY=0.95f, neckRadius=0.14f,
                                     accents=new[]{AccentType.TechVisor} },
        ["nova"]    = new HeroSpec { bodyRx=0.36f, bodyRy=0.88f, bodyRz=0.34f, bodyN1=0.45f, bodyN2=0.45f, headRadius=0.30f, headY=1.10f, neckRadius=0.12f,
                                     accents=new[]{AccentType.Antenna} },
        ["orbit"]   = new HeroSpec { bodyRx=0.42f, bodyRy=0.80f, bodyRz=0.44f, bodyN1=0.55f, bodyN2=0.55f, headRadius=0.38f, headY=0.98f, neckRadius=0.16f,
                                     accents=new[]{AccentType.JetpackLumps} },
        ["striker"] = new HeroSpec { bodyRx=0.48f, bodyRy=0.82f, bodyRz=0.44f, bodyN1=0.65f, bodyN2=0.60f, headRadius=0.33f, headY=1.00f, neckRadius=0.15f,
                                     accents=new[]{AccentType.KneePads} },
        ["sizzle"]  = new HeroSpec { bodyRx=0.50f, bodyRy=0.76f, bodyRz=0.50f, bodyN1=0.70f, bodyN2=0.65f, headRadius=0.31f, headY=0.95f, neckRadius=0.14f,
                                     accents=new[]{AccentType.ChefHat} },
        ["shade"]   = new HeroSpec { bodyRx=0.33f, bodyRy=0.90f, bodyRz=0.30f, bodyN1=0.40f, bodyN2=0.38f, headRadius=0.28f, headY=1.12f, neckRadius=0.10f,
                                     accents=new[]{AccentType.Hood} },
        ["dusty"]   = new HeroSpec { bodyRx=0.46f, bodyRy=0.79f, bodyRz=0.50f, bodyN1=0.60f, bodyN2=0.70f, headRadius=0.32f, headY=0.97f, neckRadius=0.13f,
                                     accents=new[]{AccentType.WideBrimHat} },
        ["pop"]     = new HeroSpec { bodyRx=0.38f, bodyRy=0.68f, bodyRz=0.36f, bodyN1=0.48f, bodyN2=0.48f, headRadius=0.29f, headY=0.86f, neckRadius=0.12f,
                                     accents=new[]{AccentType.RollerSkates} },
    };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the full hero art hierarchy under <paramref name="root"/>.
    /// The root already has a Rigidbody + CapsuleCollider from the prefab.
    /// </summary>
    public static void Build(string heroId, GameObject root, Color primaryColor, Color accentColor, Material heroMat)
    {
        if (!_specs.TryGetValue(heroId.ToLower(), out var spec))
        {
            Debug.LogWarning($"[CharacterArt] Unknown heroId: {heroId} — using Byte spec");
            spec = _specs["byte"];
        }

        // Grab or create shared meshes
        var bodyMesh = ProceduralMeshLibrary.SuperEllipsoidCapsule(
            spec.bodyRx, spec.bodyRy, spec.bodyRz, spec.bodyN1, spec.bodyN2, 22, 28);

        var headMesh = ProceduralMeshLibrary.SuperEllipsoidCapsule(
            spec.headRadius, spec.headRadius * 1.05f, spec.headRadius,
            0.45f, 0.45f, 16, 20);

        // ── Body ──
        var body = MakePartGO("Body", root.transform, bodyMesh, Vector3.zero, heroMat);
        ApplyMPBColor(body, primaryColor, accentColor);

        // ── Neck connector ──
        MakeSphere("Neck", root.transform,
            new Vector3(0, spec.headY - spec.headRadius * 1.1f, 0),
            spec.neckRadius, heroMat, primaryColor);

        // ── Head ──
        var head = MakePartGO("Head", root.transform, headMesh,
            new Vector3(0, spec.headY, 0), heroMat);
        ApplyMPBColor(head, primaryColor, accentColor);
        head.transform.localScale = Vector3.one * (spec.headRadius * 2f);

        // ── Eyes (two small emissive spheres) ──
        var eyeMesh = ProceduralMeshLibrary.GravityOrb(0.08f, 8);
        MakeEmissivePart("EyeL", root.transform, eyeMesh,
            new Vector3(-0.12f, spec.headY + 0.04f, spec.headRadius * 1.7f),
            Color.white, 2.5f);
        MakeEmissivePart("EyeR", root.transform, eyeMesh,
            new Vector3( 0.12f, spec.headY + 0.04f, spec.headRadius * 1.7f),
            Color.white, 2.5f);

        // ── Hero-specific accents ──
        foreach (var accent in spec.accents)
            BuildAccent(accent, root.transform, spec, primaryColor, accentColor, heroMat);

        // ── Name billboard ──
        BuildNameBillboard(heroId, root.transform, spec.headY + spec.headRadius * 2.1f);
    }

    // ── Accent builders ───────────────────────────────────────────────────────

    private static void BuildAccent(AccentType type, Transform parent, HeroSpec spec,
        Color primary, Color accent, Material mat)
    {
        switch (type)
        {
            case AccentType.TechVisor:
            {
                // Glowing horizontal band across front of head
                var visorMesh = ProceduralMeshLibrary.RoundedBox(0.55f, 0.08f, 0.12f, 0.03f, 2);
                MakeEmissivePart("Visor", parent, visorMesh,
                    new Vector3(0, spec.headY + 0.02f, spec.headRadius * 1.5f),
                    accent, 3.5f);
                break;
            }
            case AccentType.Antenna:
            {
                // Thin pole + ball tip
                var poleMesh = ProceduralMeshLibrary.RoundedBox(0.04f, 0.4f, 0.04f, 0.01f, 2);
                MakePartGO("AntennaPole", parent, poleMesh,
                    new Vector3(0.08f, spec.headY + spec.headRadius + 0.25f, 0), mat)
                    .GetComponent<MeshRenderer>()?.SetPropertyBlock(ColorBlock(primary));
                MakeSphere("AntennaTip", parent,
                    new Vector3(0.08f, spec.headY + spec.headRadius + 0.52f, 0),
                    0.07f, mat, accent, 3f);
                break;
            }
            case AccentType.JetpackLumps:
            {
                // Two rounded boxes on back
                var packMesh = ProceduralMeshLibrary.RoundedBox(0.18f, 0.32f, 0.14f, 0.05f, 2);
                for (int s = -1; s <= 1; s += 2)
                {
                    MakePartGO($"JetpackL{s}", parent, packMesh,
                        new Vector3(s * 0.14f, 0.1f, -0.42f), mat)
                        .GetComponent<MeshRenderer>()?.SetPropertyBlock(ColorBlock(accent));
                    // Thruster glow disc
                    MakeEmissivePart($"Thruster{s}", parent,
                        ProceduralMeshLibrary.GravityOrb(0.07f, 8),
                        new Vector3(s * 0.14f, -0.12f, -0.44f), accent * 2f, 4f);
                }
                break;
            }
            case AccentType.KneePads:
            {
                var padMesh = ProceduralMeshLibrary.RoundedBox(0.22f, 0.1f, 0.16f, 0.04f, 2);
                for (int s = -1; s <= 1; s += 2)
                {
                    MakePartGO($"KneePad{s}", parent, padMesh,
                        new Vector3(s * 0.21f, -0.35f, 0.25f), mat)
                        .GetComponent<MeshRenderer>()?.SetPropertyBlock(ColorBlock(accent));
                }
                break;
            }
            case AccentType.ChefHat:
            {
                // Tall dome: squished sphere on top of head
                var hatMesh = ProceduralMeshLibrary.SuperEllipsoidCapsule(0.3f, 0.45f, 0.3f, 0.6f, 0.6f, 12, 16);
                var hat = MakePartGO("ChefHat", parent, hatMesh,
                    new Vector3(0, spec.headY + spec.headRadius + 0.22f, 0), mat);
                // White hat
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", new Color(0.97f, 0.97f, 0.97f));
                mpb.SetColor("_RimColor", accent);
                hat.GetComponent<MeshRenderer>()?.SetPropertyBlock(mpb);
                break;
            }
            case AccentType.Hood:
            {
                // Slightly larger, darker ovoid that overhangs head
                var hoodMesh = ProceduralMeshLibrary.SuperEllipsoidCapsule(
                    spec.headRadius * 1.2f, spec.headRadius * 1.15f, spec.headRadius * 1.2f, 0.42f, 0.42f, 12, 16);
                var hood = MakePartGO("Hood", parent, hoodMesh,
                    new Vector3(0, spec.headY + 0.04f, 0), mat);
                var mpb = new MaterialPropertyBlock();
                mpb.SetColor("_BaseColor", primary * 0.55f);
                mpb.SetColor("_RimColor", accent);
                mpb.SetFloat("_EmissionStrength", 0.2f);
                mpb.SetColor("_EmissionColor", accent * 0.5f);
                hood.GetComponent<MeshRenderer>()?.SetPropertyBlock(mpb);
                break;
            }
            case AccentType.WideBrimHat:
            {
                // Cylinder crown + wide flat disc brim
                var crownMesh = ProceduralMeshLibrary.RoundedBox(0.38f, 0.28f, 0.38f, 0.08f, 3);
                MakePartGO("HatCrown", parent, crownMesh,
                    new Vector3(0, spec.headY + spec.headRadius + 0.15f, 0), mat)
                    .GetComponent<MeshRenderer>()?.SetPropertyBlock(ColorBlock(primary * 0.6f));
                var brimMesh = ProceduralMeshLibrary.RoundedBox(0.95f, 0.06f, 0.88f, 0.04f, 2);
                MakePartGO("HatBrim", parent, brimMesh,
                    new Vector3(0, spec.headY + spec.headRadius, 0), mat)
                    .GetComponent<MeshRenderer>()?.SetPropertyBlock(ColorBlock(primary * 0.5f));
                break;
            }
            case AccentType.RollerSkates:
            {
                // 4 small orbs at foot level
                var wheelMesh = ProceduralMeshLibrary.GravityOrb(0.1f, 8);
                float[] xs = { -0.18f, 0.18f };
                float[] zs = { -0.12f, 0.12f };
                foreach (float x in xs) foreach (float z in zs)
                {
                    var wheel = MakeEmissivePart("Wheel", parent, wheelMesh,
                        new Vector3(x, -0.78f, z), accent, 1.5f);
                }
                break;
            }
        }
    }

    // ── Name billboard ────────────────────────────────────────────────────────

    private static void BuildNameBillboard(string heroId, Transform parent, float y)
    {
        // Quad billboard — TextMeshPro would be better but we stay dependency-free
        // The quad is positioned above head; real text needs TMP in inspector
        var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "NameBillboard";
        quad.transform.SetParent(parent);
        quad.transform.localPosition = new Vector3(0, y + 0.15f, 0);
        quad.transform.localScale    = new Vector3(0.8f, 0.18f, 1f);
        Object.Destroy(quad.GetComponent<MeshCollider>());

        // Tag for LookAt update in CharacterVisuals
        quad.tag = "Untagged";
    }

    // ── Part construction helpers ─────────────────────────────────────────────

    private static GameObject MakePartGO(string name, Transform parent, Mesh mesh,
        Vector3 localPos, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        var mf = go.AddComponent<MeshFilter>();  mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows    = true;
        return go;
    }

    private static void MakeSphere(string name, Transform parent, Vector3 pos,
        float r, Material mat, Color color, float emission = 0f)
    {
        var go = MakePartGO(name, parent, ProceduralMeshLibrary.GravityOrb(r, 10), pos, mat);
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color);
        mpb.SetColor("_RimColor", color);
        if (emission > 0f)
        {
            mpb.SetColor("_EmissionColor", color);
            mpb.SetFloat("_EmissionStrength", emission);
        }
        go.GetComponent<MeshRenderer>().SetPropertyBlock(mpb);
    }

    private static GameObject MakeEmissivePart(string name, Transform parent, Mesh mesh,
        Vector3 pos, Color emissionColor, float strength)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale    = Vector3.one;

        var mf = go.AddComponent<MeshFilter>();  mf.mesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        // Use Standard unlit glow fallback when no char mat
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor",       emissionColor * 0.4f);
        mpb.SetColor("_EmissionColor",   emissionColor);
        mpb.SetFloat("_EmissionStrength", strength);
        mr.SetPropertyBlock(mpb);
        return go;
    }

    private static void ApplyMPBColor(GameObject go, Color primary, Color accent)
    {
        var mr = go.GetComponent<MeshRenderer>();
        if (mr == null) return;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", primary);
        mpb.SetColor("_RimColor",  accent);
        mr.SetPropertyBlock(mpb);
    }

    private static MaterialPropertyBlock ColorBlock(Color c)
    {
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", c);
        mpb.SetColor("_RimColor",  c * 1.5f);
        return mpb;
    }
}
