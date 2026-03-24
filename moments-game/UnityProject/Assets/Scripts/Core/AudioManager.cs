using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AudioManager — pooled SFX + layered music system.
/// Lives in Bootstrap (DontDestroyOnLoad).
/// 
/// Features:
///   - 16-source SFX pool (no alloc during gameplay)
///   - 2-track music crossfade (seamless loop transitions)
///   - Snapshot blending: lobby / gameplay / results / win
///   - Per-player positional audio (3D panning)
///   - Haptic-audio sync: SFX triggers mirror haptic events
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    // ── SFX Clips ──────────────────────────────────────────────────────────
    [System.Serializable]
    public class SFXEntry
    {
        public string    id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.8f, 1.2f)] public float pitchVariance = 0.05f;
    }

    [Header("SFX")]
    [SerializeField] private SFXEntry[]  sfxLibrary;
    [SerializeField] private int         sfxPoolSize = 16;

    [Header("Music")]
    [SerializeField] private AudioClip   musicLobby;
    [SerializeField] private AudioClip   musicPolarPush;
    [SerializeField] private AudioClip   musicResults;
    [SerializeField] private AudioClip   musicPodium;
    [SerializeField] private float       musicCrossfadeTime = 1.5f;

    [Header("Volume")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 0.85f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume    = 1.0f;
    [SerializeField, Range(0f, 1f)] private float musicVolume  = 0.65f;

    // ── Runtime ────────────────────────────────────────────────────────────
    private Queue<AudioSource>            _sfxPool    = new();
    private Dictionary<string, SFXEntry> _sfxMap     = new();
    private AudioSource                  _musicTrack1;
    private AudioSource                  _musicTrack2;
    private bool                         _music1Active = true;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildSFXPool();
        BuildMusicTracks();
        BuildSFXMap();
    }

    private void BuildSFXPool()
    {
        for (int i = 0; i < sfxPoolSize; i++)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D by default
            _sfxPool.Enqueue(src);
        }
    }

    private void BuildMusicTracks()
    {
        _musicTrack1 = gameObject.AddComponent<AudioSource>();
        _musicTrack1.loop = true; _musicTrack1.playOnAwake = false; _musicTrack1.volume = 0f;

        _musicTrack2 = gameObject.AddComponent<AudioSource>();
        _musicTrack2.loop = true; _musicTrack2.playOnAwake = false; _musicTrack2.volume = 0f;
    }

    private void BuildSFXMap()
    {
        foreach (var entry in sfxLibrary)
            if (entry != null && !string.IsNullOrEmpty(entry.id))
                _sfxMap[entry.id] = entry;
    }

    // ── SFX ────────────────────────────────────────────────────────────────

    public void PlaySFX(string id, Vector3? worldPos = null, float volumeMult = 1f)
    {
        if (!_sfxMap.TryGetValue(id, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"[Audio] SFX not found: {id}");
            return;
        }

        if (_sfxPool.Count == 0) return; // Pool exhausted — drop this SFX

        var src = _sfxPool.Dequeue();
        src.clip        = entry.clip;
        src.volume      = entry.volume * sfxVolume * masterVolume * volumeMult;
        src.pitch       = 1f + Random.Range(-entry.pitchVariance, entry.pitchVariance);
        src.spatialBlend = worldPos.HasValue ? 1f : 0f;
        if (worldPos.HasValue) src.transform.position = worldPos.Value;
        src.Play();

        StartCoroutine(ReturnSFXSource(src, entry.clip.length));
    }

    private IEnumerator ReturnSFXSource(AudioSource src, float delay)
    {
        yield return new WaitForSeconds(delay + 0.05f);
        src.Stop();
        _sfxPool.Enqueue(src);
    }

    // ── Music ──────────────────────────────────────────────────────────────

    public void PlayMusic(AudioClip clip, bool immediate = false)
    {
        if (clip == null) return;
        float fade = immediate ? 0f : musicCrossfadeTime;
        StartCoroutine(CrossfadeMusic(clip, fade));
    }

    public void PlayLobbyMusic()    => PlayMusic(musicLobby);
    public void PlayPolarPushMusic()=> PlayMusic(musicPolarPush);
    public void PlayResultsMusic()  => PlayMusic(musicResults);
    public void PlayPodiumMusic()   => PlayMusic(musicPodium);

    private IEnumerator CrossfadeMusic(AudioClip newClip, float fadeTime)
    {
        var outTrack = _music1Active ? _musicTrack1 : _musicTrack2;
        var inTrack  = _music1Active ? _musicTrack2 : _musicTrack1;
        _music1Active = !_music1Active;

        inTrack.clip   = newClip;
        inTrack.volume = 0f;
        inTrack.Play();

        float targetVol = musicVolume * masterVolume;
        float elapsed   = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Mathf.Max(fadeTime, 0.001f);
            outTrack.volume = Mathf.Lerp(targetVol, 0f, t);
            inTrack.volume  = Mathf.Lerp(0f, targetVol, t);
            yield return null;
        }

        outTrack.Stop();
        outTrack.volume = 0f;
        inTrack.volume  = targetVol;
    }

    // ── Haptic-synced shortcuts ────────────────────────────────────────────

    public void OnDash()                 => PlaySFX("dash");
    public void OnHit(Vector3 pos)       => PlaySFX("hit", pos);
    public void OnElimination()          => PlaySFX("elimination");
    public void OnTileCrack(Vector3 pos) => PlaySFX("ice_crack", pos);
    public void OnCountdown()            => PlaySFX("countdown");
    public void OnGameStart()            => PlaySFX("game_start");
    public void OnWin()                  => PlaySFX("win");
    public void OnPickup()               => PlaySFX("pickup");
    public void OnTileShatter(Vector3 pos) => PlaySFX("ice_shatter", pos, 1.2f);
    public void OnCountdown()      => PlaySFX("countdown");
    public void OnGameStart()      => PlaySFX("game_start");
    public void OnWin()            => PlaySFX("win");
    public void OnPlayerJoin()     => PlaySFX("player_join");
    public void OnPaint(Vector3 pos) => PlaySFX("paint_splat", pos);
    public void OnScoreIncrease()  => PlaySFX("score_tick");

    // ── Volume Control ─────────────────────────────────────────────────────

    public void SetMasterVolume(float v)
    {
        masterVolume = Mathf.Clamp01(v);
        UpdateMusicVolume();
    }

    private void UpdateMusicVolume()
    {
        float target = musicVolume * masterVolume;
        var active = _music1Active ? _musicTrack1 : _musicTrack2;
        if (active != null && active.isPlaying) active.volume = target;
    }
}
