using UnityEngine;

/// <summary>
/// Shared VFX pool for all mini-games.
/// Spawns and recycles: HitSpark, DashTrail, ScoreBurst, ConfettiPop, Explosion, PaintSplat.
/// Lives in Bootstrap (persistent). Each mini-game calls VFXManager.Instance.Play(...).
/// Uses object pooling — no GC allocations during gameplay.
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [System.Serializable]
    public struct VFXEntry
    {
        public VFXType type;
        public GameObject prefab;
        [Range(4, 32)] public int poolSize;
    }

    public enum VFXType
    {
        HitSpark,
        DashTrail,
        ScoreBurst,
        Confetti,
        Explosion,
        PaintSplat,
        IceCrack,
        ElimFlash,
        JoinPing
    }

    [Header("VFX Prefab Entries")]
    [SerializeField] private VFXEntry[] vfxEntries;

    private System.Collections.Generic.Dictionary<VFXType, System.Collections.Generic.Queue<GameObject>> _pools
        = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildPools();
    }

    private void BuildPools()
    {
        foreach (var entry in vfxEntries)
        {
            var q = new System.Collections.Generic.Queue<GameObject>();
            for (int i = 0; i < entry.poolSize; i++)
            {
                if (entry.prefab == null) continue;
                var go = Instantiate(entry.prefab, transform);
                go.SetActive(false);
                go.name = $"{entry.type}_{i:D2}";
                q.Enqueue(go);
            }
            _pools[entry.type] = q;
        }
    }

    /// <summary>
    /// Spawn a VFX at world position with optional color tint.
    /// Returns the GameObject so callers can attach to transforms if needed.
    /// </summary>
    public GameObject Play(VFXType type, Vector3 worldPosition, Color? color = null, float autoReturnSeconds = 3f)
    {
        if (!_pools.TryGetValue(type, out var pool) || pool.Count == 0)
        {
            Debug.LogWarning($"[VFX] Pool empty or missing: {type}");
            return null;
        }

        var go = pool.Dequeue();
        go.transform.position = worldPosition;
        go.SetActive(true);

        // Tint particle color if requested
        if (color.HasValue)
        {
            var ps = go.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = new ParticleSystem.MinMaxGradient(color.Value);
            }
        }

        // Play all child particle systems
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            ps.Play();

        // Auto-return to pool
        StartCoroutine(ReturnAfterDelay(go, type, autoReturnSeconds));
        return go;
    }

    private System.Collections.IEnumerator ReturnAfterDelay(GameObject go, VFXType type, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go == null) yield break;

        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        go.SetActive(false);
        if (_pools.TryGetValue(type, out var pool))
            pool.Enqueue(go);
    }

    // ── Convenience methods ───────────────────────────────────────────────────

    public void HitSpark(Vector3 pos, Color playerColor)
        => Play(VFXType.HitSpark, pos, playerColor, 1.5f);

    public void PaintSplat(Vector3 pos, Color playerColor)
        => Play(VFXType.PaintSplat, pos, playerColor, 2f);

    public void Explosion(Vector3 pos)
        => Play(VFXType.Explosion, pos, null, 2f);

    public void ScoreBurst(Vector3 pos, Color playerColor)
        => Play(VFXType.ScoreBurst, pos, playerColor, 1f);

    public void Confetti(Vector3 pos)
        => Play(VFXType.Confetti, pos, null, 5f);

    public void IceCrack(Vector3 pos)
        => Play(VFXType.IceCrack, pos, new Color(0.6f, 0.85f, 1f), 2f);

    public void ElimFlash(Vector3 pos, Color playerColor)
        => Play(VFXType.ElimFlash, pos, playerColor, 1f);
}
