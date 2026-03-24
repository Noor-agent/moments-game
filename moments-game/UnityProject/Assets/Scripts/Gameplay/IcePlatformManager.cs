using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the ice platform in Polar Push.
/// Spawns the hexagonal tile grid, tracks cracks, and handles tile collapse.
/// Procedural: tiles crack based on player proximity and impact events.
/// </summary>
public class IcePlatformManager : MonoBehaviour
{
    [Header("Platform Config")]
    [SerializeField] private GameObject hexTilePrefab;
    [SerializeField] private int ringCount = 4;           // Number of hex rings (4 = ~48 tiles)
    [SerializeField] private float tileRadius = 1.2f;
    [SerializeField] private float tileGap = 0.05f;
    [SerializeField] private float collapseDelay = 1.5f;  // Time between crack and fall
    [SerializeField] private float fallSpeed = 8f;

    [Header("Shrink Schedule")]
    [SerializeField] private int[] tilesToRemovePerShrink = { 6, 8, 10, 14 };
    private int _shrinkLevel;

    private List<IceTile> _tiles = new();
    private List<IceTile> _outerRingTiles = new();

    private void Awake()
    {
        GenerateHexGrid();
    }

    private void GenerateHexGrid()
    {
        // Axial hex grid generation
        float w = tileRadius * 2f + tileGap;
        float h = Mathf.Sqrt(3f) * tileRadius + tileGap;

        for (int q = -ringCount; q <= ringCount; q++)
        {
            int r1 = Mathf.Max(-ringCount, -q - ringCount);
            int r2 = Mathf.Min(ringCount, -q + ringCount);

            for (int r = r1; r <= r2; r++)
            {
                float xPos = w * (q + r * 0.5f);
                float zPos = h * r;
                var pos = transform.position + new Vector3(xPos, 0f, zPos);

                var tileObj = Instantiate(hexTilePrefab, pos, Quaternion.identity, transform);
                var tile = tileObj.GetComponent<IceTile>();
                if (tile != null)
                {
                    int distance = (Mathf.Abs(q) + Mathf.Abs(r) + Mathf.Abs(q + r)) / 2;
                    tile.Init(this, distance == ringCount);
                    _tiles.Add(tile);
                    if (distance == ringCount) _outerRingTiles.Add(tile);
                }
            }
        }

        Debug.Log($"[IcePlatform] Generated {_tiles.Count} tiles in {ringCount} rings");
    }

    /// <summary>Called by PolarPushGame every N seconds.</summary>
    public void ShrinkPlatform()
    {
        if (_shrinkLevel >= tilesToRemovePerShrink.Length) return;
        int count = tilesToRemovePerShrink[_shrinkLevel];
        _shrinkLevel++;

        // Crack outer tiles first
        var candidates = new List<IceTile>(_outerRingTiles);
        candidates.RemoveAll(t => t == null || t.IsCollapsed);
        candidates.Sort((a, b) => Random.value.CompareTo(0.5f));

        for (int i = 0; i < Mathf.Min(count, candidates.Count); i++)
            candidates[i].TriggerCrack();

        // Rebuild outer ring list
        _outerRingTiles.RemoveAll(t => t == null || t.IsCollapsed);

        Debug.Log($"[IcePlatform] Shrink level {_shrinkLevel}: cracked {count} tiles");
    }

    /// <summary>Called when a player lands a dash impact on a tile.</summary>
    public void OnPlayerImpact(Vector3 worldPos)
    {
        float crackRadius = tileRadius * 2.5f;
        foreach (var tile in _tiles)
        {
            if (tile == null || tile.IsCollapsed) continue;
            if (Vector3.Distance(tile.transform.position, worldPos) < crackRadius)
                tile.TriggerCrack();
        }
    }
}

/// <summary>
/// Individual hexagonal ice tile. Handles crack state and collapse animation.
/// </summary>
public class IceTile : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private Material intactMaterial;
    [SerializeField] private Material crackedMaterial;
    [SerializeField] private Material shatteringMaterial;
    [SerializeField] private ParticleSystem crackParticles;
    [SerializeField] private ParticleSystem shatterParticles;

    public bool IsCollapsed { get; private set; }
    public bool IsOuter { get; private set; }

    private IcePlatformManager _manager;
    private bool _isCracked;
    private Rigidbody _rb;

    public void Init(IcePlatformManager manager, bool isOuter)
    {
        _manager = manager;
        IsOuter = isOuter;
        _rb = GetComponent<Rigidbody>();
        if (_rb != null) _rb.isKinematic = true;
    }

    public void TriggerCrack()
    {
        if (_isCracked || IsCollapsed) return;
        _isCracked = true;

        if (tileRenderer != null && crackedMaterial != null)
            tileRenderer.material = crackedMaterial;

        crackParticles?.Play();
        StartCoroutine(CollapseAfterDelay(Random.Range(1f, 2.5f)));
    }

    private IEnumerator CollapseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (tileRenderer != null && shatteringMaterial != null)
            tileRenderer.material = shatteringMaterial;
        shatterParticles?.Play();

        // Enable physics and let it fall
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.AddForce(Vector3.down * 2f + Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            _rb.AddTorque(Random.insideUnitSphere * 3f, ForceMode.Impulse);
        }

        IsCollapsed = true;

        // Disable collider so players aren't stopped by falling tile
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Destroy after falling off screen
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }
}
