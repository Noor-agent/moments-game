using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

/// <summary>
/// Post-processing manager for Moments.
/// Handles per-scene volume transitions and gameplay-reactive effects.
/// 
/// Polar Push effects:
///   - Bloom: heavy (aurora + ice glow readability)
///   - Chromatic aberration: subtle at rest, spikes on impact
///   - Vignette: tight focus on platform action
///   - Tonemapping: ACES for cinematic warmth
///   - Depth of field: background blur (Gaussian, far focus)
///   - Color grading: cold LUT (arctic blue-grey)
/// </summary>
[RequireComponent(typeof(Volume))]
public class PostProcessingManager : MonoBehaviour
{
    public static PostProcessingManager Instance { get; private set; }

    [Header("Volume Reference")]
    [SerializeField] private Volume globalVolume;

    [Header("Polar Push Preset")]
    [SerializeField] private float ppBloomIntensity      = 1.8f;
    [SerializeField] private float ppBloomThreshold      = 0.9f;
    [SerializeField] private float ppBloomScatter        = 0.72f;
    [SerializeField] private float ppChromaticBase       = 0.04f;
    [SerializeField] private float ppVignetteIntensity   = 0.38f;
    [SerializeField] private float ppDofFocalLength      = 22f;
    [SerializeField] private float ppDofAperture         = 4.5f;

    // Cached component references
    private Bloom              _bloom;
    private ChromaticAberration _chroma;
    private Vignette           _vignette;
    private DepthOfField       _dof;
    private ColorAdjustments   _colorAdj;
    private Tonemapping        _tonemap;

    // Impact flash state
    private float _impactChromaTarget;
    private float _impactChromaCurrent;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (globalVolume == null) globalVolume = GetComponent<Volume>();
        SetupVolume();
    }

    private void SetupVolume()
    {
        var profile = globalVolume.profile;

        // Bloom — heavy for ice glow and aurora
        if (!profile.TryGet(out _bloom))     _bloom     = profile.Add<Bloom>(false);
        _bloom.active        = true;
        _bloom.intensity.Override(ppBloomIntensity);
        _bloom.threshold.Override(ppBloomThreshold);
        _bloom.scatter.Override(ppBloomScatter);
        _bloom.highQualityFiltering.Override(true);
        _bloom.dirtIntensity.Override(0f);

        // Chromatic aberration — subtle, spikes on hit
        if (!profile.TryGet(out _chroma))   _chroma    = profile.Add<ChromaticAberration>(false);
        _chroma.active = true;
        _chroma.intensity.Override(ppChromaticBase);

        // Vignette — focus player on platform
        if (!profile.TryGet(out _vignette)) _vignette  = profile.Add<Vignette>(false);
        _vignette.active = true;
        _vignette.intensity.Override(ppVignetteIntensity);
        _vignette.smoothness.Override(0.5f);
        _vignette.rounded.Override(true);

        // Depth of field — soft background blur
        if (!profile.TryGet(out _dof))      _dof       = profile.Add<DepthOfField>(false);
        _dof.active = true;
        _dof.mode.Override(DepthOfFieldMode.Gaussian);
        _dof.gaussianStart.Override(ppDofFocalLength);
        _dof.gaussianEnd.Override(ppDofFocalLength + 40f);
        _dof.gaussianMaxRadius.Override(1.2f);

        // Color adjustments — cool/cold grade for arctic
        if (!profile.TryGet(out _colorAdj)) _colorAdj  = profile.Add<ColorAdjustments>(false);
        _colorAdj.active = true;
        _colorAdj.contrast.Override(8f);
        _colorAdj.saturation.Override(12f);
        _colorAdj.colorFilter.Override(new Color(0.88f, 0.94f, 1.0f)); // Cold blue tint
        _colorAdj.postExposure.Override(0.15f);

        // Tonemapping — ACES for cinematic
        if (!profile.TryGet(out _tonemap))  _tonemap   = profile.Add<Tonemapping>(false);
        _tonemap.active = true;
        _tonemap.mode.Override(TonemappingMode.ACES);

        Debug.Log("[PostFX] Volume configured for Polar Push");
    }

    // ── Per-Arena Presets ──────────────────────────────────────────────────

    public enum ArenaPreset
    {
        PolarPush,
        ColorClash,
        TankBattle,
        WaveRider,
        BumperBlitz,
        BlinkShot,
        GravityGrab,
        Lobby,
        Podium,
    }

    /// <summary>
    /// Transition post-processing to match a mini-game's arena aesthetic.
    /// Call from MiniGameBase.OnGameStart() or MiniGameLoader scene activation.
    /// </summary>
    public void ApplyArenaPreset(ArenaPreset preset, float transitionTime = 1.2f)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionPreset(preset, transitionTime));
    }

    private IEnumerator TransitionPreset(ArenaPreset preset, float duration)
    {
        // Capture current values
        float startBloom      = _bloom?.intensity.value ?? ppBloomIntensity;
        float startChroma     = _chroma?.intensity.value ?? ppChromaticBase;
        float startContrast   = _colorAdj?.contrast.value ?? 8f;
        float startSaturation = _colorAdj?.saturation.value ?? 12f;
        Color startFilter     = _colorAdj?.colorFilter.value ?? Color.white;

        // Target values per arena
        float tBloom, tChroma, tContrast, tSat;
        Color tFilter;

        switch (preset)
        {
            case ArenaPreset.ColorClash:
                // Warm warehouse — vivid, saturated, high contrast paint colours
                tBloom    = 1.2f; tChroma = 0.02f;
                tContrast = 15f;  tSat    = 28f;
                tFilter   = new Color(1.0f, 0.96f, 0.88f);
                break;

            case ArenaPreset.TankBattle:
                // Harsh desert sun — warm, high exposure, sharp shadows
                tBloom    = 0.9f; tChroma = 0.01f;
                tContrast = 20f;  tSat    = 8f;
                tFilter   = new Color(1.0f, 0.94f, 0.78f);
                break;

            case ArenaPreset.WaveRider:
                // Tropical sunrise — warm bloom, high saturation, low chroma
                tBloom    = 2.2f; tChroma = 0.02f;
                tContrast = 12f;  tSat    = 22f;
                tFilter   = new Color(1.0f, 0.88f, 0.72f);
                _dof?.gaussianStart.Override(30f);
                _dof?.gaussianEnd.Override(100f);
                break;

            case ArenaPreset.BumperBlitz:
                // Neon carnival night — heavy bloom, strong chroma, purple tint
                tBloom    = 2.8f; tChroma = 0.06f;
                tContrast = 18f;  tSat    = 35f;
                tFilter   = new Color(0.88f, 0.78f, 1.0f);
                break;

            case ArenaPreset.BlinkShot:
                // Cyberpunk — heavy neon bloom, max chroma, cool purple
                tBloom    = 3.2f; tChroma = 0.08f;
                tContrast = 22f;  tSat    = 30f;
                tFilter   = new Color(0.82f, 0.75f, 1.0f);
                break;

            case ArenaPreset.GravityGrab:
                // Deep space — minimal bloom (stars), near-zero chroma, deep blue
                tBloom    = 1.5f; tChroma = 0.03f;
                tContrast = 25f;  tSat    = 15f;
                tFilter   = new Color(0.78f, 0.88f, 1.0f);
                _dof?.gaussianStart.Override(80f);
                _dof?.gaussianEnd.Override(300f);
                break;

            case ArenaPreset.Lobby:
                // Warm friendly — soft bloom, neutral
                tBloom    = 1.0f; tChroma = 0.01f;
                tContrast = 5f;   tSat    = 10f;
                tFilter   = new Color(1.0f, 0.98f, 0.94f);
                break;

            case ArenaPreset.Podium:
                // Celebration — warm golden, heavy bloom
                tBloom    = 3.5f; tChroma = 0.02f;
                tContrast = 8f;   tSat    = 20f;
                tFilter   = new Color(1.0f, 0.95f, 0.75f);
                break;

            default: // PolarPush
                tBloom    = ppBloomIntensity; tChroma = ppChromaticBase;
                tContrast = 8f;               tSat    = 12f;
                tFilter   = new Color(0.88f, 0.94f, 1.0f);
                break;
        }

        // Lerp to target
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = Mathf.SmoothStep(0, 1, elapsed / duration);

            _bloom?.intensity.Override(Mathf.Lerp(startBloom, tBloom, t));
            _chroma?.intensity.Override(Mathf.Lerp(startChroma, tChroma, t));
            _colorAdj?.contrast.Override(Mathf.Lerp(startContrast, tContrast, t));
            _colorAdj?.saturation.Override(Mathf.Lerp(startSaturation, tSat, t));
            _colorAdj?.colorFilter.Override(Color.Lerp(startFilter, tFilter, t));

            yield return null;
        }

        _bloom?.intensity.Override(tBloom);
        _chroma?.intensity.Override(tChroma);
        _colorAdj?.contrast.Override(tContrast);
        _colorAdj?.saturation.Override(tSat);
        _colorAdj?.colorFilter.Override(tFilter);

        Debug.Log($"[PostFX] Transitioned to preset: {preset}");
    }

    /// <summary>Trigger chromatic aberration spike on player impact.</summary>
    public void OnPlayerHit(float strength = 1f)
    {
        _impactChromaTarget = ppChromaticBase + (0.35f * strength);
        StopCoroutine(nameof(DecayChroma));
        StartCoroutine(nameof(DecayChroma));
    }

    /// <summary>Screen shake (vignette pulse) on tile collapse.</summary>
    public void OnTileCollapse()
        => StartCoroutine(VignettePulse(0.55f, 0.25f));

    /// <summary>Full-screen flash (brief bloom spike) on elimination.</summary>
    public void OnPlayerEliminated()
        => StartCoroutine(BloomPulse(5f, 0.4f));

    /// <summary>Win state: warm up color grading.</summary>
    public void SetWinState()
    {
        if (_colorAdj != null)
            _colorAdj.colorFilter.Override(new Color(1f, 0.95f, 0.8f));
        if (_bloom != null)
            _bloom.intensity.Override(ppBloomIntensity * 2.2f);
    }

    public void ResetState()
    {
        SetupVolume();
    }

    // ── Coroutines ─────────────────────────────────────────────────────────

    private IEnumerator DecayChroma()
    {
        while (_impactChromaCurrent < _impactChromaTarget)
        {
            _impactChromaCurrent = Mathf.MoveTowards(
                _impactChromaCurrent, _impactChromaTarget, Time.deltaTime * 8f);
            _chroma?.intensity.Override(_impactChromaCurrent);
            yield return null;
        }
        while (_impactChromaCurrent > ppChromaticBase)
        {
            _impactChromaCurrent = Mathf.MoveTowards(
                _impactChromaCurrent, ppChromaticBase, Time.deltaTime * 3f);
            _chroma?.intensity.Override(_impactChromaCurrent);
            yield return null;
        }
        _impactChromaCurrent = ppChromaticBase;
        _chroma?.intensity.Override(ppChromaticBase);
    }

    private IEnumerator VignettePulse(float peak, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float v = Mathf.Lerp(peak, ppVignetteIntensity, t / duration);
            _vignette?.intensity.Override(v);
            yield return null;
        }
        _vignette?.intensity.Override(ppVignetteIntensity);
    }

    private IEnumerator BloomPulse(float peak, float duration)
    {
        _bloom?.intensity.Override(peak);
        yield return new WaitForSeconds(duration * 0.2f);
        float t = 0f;
        while (t < duration * 0.8f)
        {
            t += Time.deltaTime;
            float b = Mathf.Lerp(peak, ppBloomIntensity, t / (duration * 0.8f));
            _bloom?.intensity.Override(b);
            yield return null;
        }
        _bloom?.intensity.Override(ppBloomIntensity);
    }
}
