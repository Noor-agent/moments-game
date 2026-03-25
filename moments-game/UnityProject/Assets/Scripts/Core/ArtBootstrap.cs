using UnityEngine;

/// <summary>
/// Bootstraps all procedural art at scene load.
/// Replaces placeholder primitive meshes in prefabs with proper procedural geometry.
/// Attach to the Bootstrap scene root — fires once on Awake, before first frame.
///
/// Each arena instantiates its own ArtBootstrap override that applies
/// arena-specific mesh swaps and material setup.
/// </summary>
public class ArtBootstrap : MonoBehaviour
{
    [Header("Character Materials (assign 8 Char_*.mat assets)")]
    [SerializeField] private Material[] heroMaterials;   // Index matches HEROES array order

    [Header("Arena Materials")]
    [SerializeField] private Material iceTileIntactMat;
    [SerializeField] private Material iceTileCrackedMat;
    [SerializeField] private Material colorTileMat;
    [SerializeField] private Material tankFloorMat;
    [SerializeField] private Material oceanMat;
    [SerializeField] private Material rinkFloorMat;
    [SerializeField] private Material rooftopMat;
    [SerializeField] private Material spacePlatformMat;

    [Header("VFX Materials")]
    [SerializeField] private Material particleMat;        // Unlit, additive, white → tinted by VFX

    private void Awake()
    {
        SwapCharacterMeshes();
        SwapArenaMeshes();
        Debug.Log("[ArtBootstrap] Procedural art initialized.");
    }

    // ── Character mesh swap ────────────────────────────────────────────────

    private void SwapCharacterMeshes()
    {
        var heroMesh = ProceduralMeshLibrary.SuperEllipsoidCapsule(
            rx: 0.42f, ry: 0.82f, rz: 0.42f,
            n1: 0.55f, n2: 0.55f,
            stacks: 22, slices: 28);

        // Find all spawned character objects (tagged "Player")
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            var mf = go.GetComponent<MeshFilter>();
            var mr = go.GetComponent<MeshRenderer>();
            if (mf != null) mf.mesh = heroMesh;
        }
    }

    // ── Arena mesh swap ────────────────────────────────────────────────────

    private void SwapArenaMeshes()
    {
        // Hex tiles — replace all IceTile cubes with proper hex prisms
        var hexMesh = ProceduralMeshLibrary.Hexagon(radius: 1.15f, height: 0.2f);
        foreach (var go in GameObject.FindGameObjectsWithTag("IceTile"))
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) mf.mesh = hexMesh;
        }

        // Color tiles — replace with rounded tile mesh
        var tileRoundedMesh = ProceduralMeshLibrary.RoundedBox(1.92f, 0.18f, 1.92f, 0.06f, 3);
        foreach (var go in GameObject.FindGameObjectsWithTag("ColorTile"))
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) mf.mesh = tileRoundedMesh;
        }

        // Ocean plane
        var oceanMesh = ProceduralMeshLibrary.OceanPlane(200f, 80);
        var ocean     = GameObject.Find("Ocean");
        if (ocean != null)
        {
            var mf = ocean.GetComponent<MeshFilter>();
            if (mf != null) mf.mesh = oceanMesh;
        }

        // Gravity orb platform ring
        var torusMesh = ProceduralMeshLibrary.Torus(11f, 0.8f);
        var platform  = GameObject.Find("OrbitalPlatform");
        if (platform != null)
        {
            var mf = platform.GetComponent<MeshFilter>();
            if (mf != null) mf.mesh = torusMesh;
        }

        // Boost pads — boost arrow decal quads
        var arrowMesh = ProceduralMeshLibrary.BoostArrow(1.6f, 0.8f);
        foreach (var go in GameObject.FindGameObjectsWithTag("BoostPad"))
        {
            var mf = go.GetComponent<MeshFilter>();
            if (mf != null) mf.mesh = arrowMesh;
        }
    }
}
