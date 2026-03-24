using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Wave Rider — Mini-game #5.
/// Surf Gerstner ocean waves on a board. Stay on the wave crest for score multiplier.
/// Lean left/right to steer; tilt phone to balance; actionPressed = air trick.
///
/// Scoring:
///   - Riding wave crest:  +3 pts/sec × multiplier
///   - Trick landing:      +50 pts, multiplier +0.5 (max 4×)
///   - Wiping out:         multiplier reset to 1×, 3s recovery
///   - Longest crest time: bonus 200 pts at end
///
/// TV-authoritative. Phones send: moveX (lean), tiltX/tiltY (balance), actionPressed (trick).
/// OceanWater.shader drives visual waves; wave math is replicated in C# for physics.
/// </summary>
public class WaveRiderGame : MiniGameBase
{
    protected override PostProcessingManager.ArenaPreset ArenaPostFX => PostProcessingManager.ArenaPreset.WaveRider;
    protected override string LayoutId => "wave-rider";

    // ── Inspector Config ──────────────────────────────────────────────────
    [Header("Wave Parameters (must match OceanWater.shader)")]
    [SerializeField] private float waveAmplitude  = 1.6f;    // A
    [SerializeField] private float waveSteepness  = 0.55f;   // Q (Gerstner steepness)
    [SerializeField] private float waveLength     = 12f;     // L (metres)
    [SerializeField] private float waveSpeed      = 6f;      // speed (metres/sec)
    [SerializeField] private float waveDirectionX = 0.707f;  // D.x (normalized)
    [SerializeField] private float waveDirectionZ = 0.707f;  // D.z

    [Header("Board Physics")]
    [SerializeField] private float steerSpeed     = 80f;     // Degrees/sec on Y axis
    [SerializeField] private float forwardSpeed   = 12f;     // Board constant forward speed
    [SerializeField] private float boardBuoyancy  = 25f;     // Spring force upward from water
    [SerializeField] private float boardDamping   = 6f;      // Damping on vertical oscillation
    [SerializeField] private float tiltTolerance  = 0.35f;   // Tilt magnitude below which no wipeout
    [SerializeField] private float wipeoutTiltThreshold = 0.75f;

    [Header("Trick System")]
    [SerializeField] private float airTimeForTrick   = 0.4f;  // Seconds airborne needed for trick
    [SerializeField] private float trickCooldown     = 2.5f;
    [SerializeField] private float maxMultiplier     = 4f;

    [Header("Recovery")]
    [SerializeField] private float wipeoutRecoveryTime = 3f;

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform   oceanTransform;   // Ocean plane origin for wave sampling

    // ── Per-player runtime ────────────────────────────────────────────────
    private class SurferState
    {
        public Rigidbody    rb;
        public Transform    boardTransform;
        public CharacterVisuals visuals;

        // Input
        public float leanInput;      // moveX: -1 left, +1 right
        public float tiltX;
        public float tiltY;
        public bool  trickRequested;

        // Game state
        public float scoreMultiplier   = 1f;
        public float airTime;
        public bool  isAirborne;
        public bool  isWipingOut;
        public float wipeoutTimer;
        public float trickCooldownTimer;
        public float crestTimeTotal;   // Cumulative seconds on crest
        public bool  isOnCrest;        // Currently riding crest bonus zone
    }

    private readonly Dictionary<string, SurferState> _surfers = new();
    private float _waveTime;

    // ── MiniGameBase Overrides ────────────────────────────────────────────

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        _surfers.Clear();
        _waveTime = 0f;

        for (int i = 0; i < players.Count; i++)
        {
            var p    = players[i];
            var spawn = spawnPoints[i % spawnPoints.Length];

            // Instantiate board prefab (reuse character prefab with board parented)
            var go   = new GameObject($"Surfer_{p.nickname}");
            go.transform.position = spawn.position;
            var rb   = go.AddComponent<Rigidbody>();
            rb.mass              = 1f;
            rb.constraints       = RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationX;
            rb.linearDamping     = 1.5f;
            rb.angularDamping    = 8f;
            rb.interpolation     = RigidbodyInterpolation.Interpolate;

            var visuals = go.GetComponent<CharacterVisuals>();
            visuals?.Initialize(p, p.characterDef);

            _surfers[p.playerId] = new SurferState
            {
                rb             = rb,
                boardTransform = go.transform,
                visuals        = visuals
            };
        }
    }

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying) return;
        if (!_surfers.TryGetValue(playerId, out var s) || s.isWipingOut) return;

        s.leanInput  = input.moveX;
        s.tiltX      = input.tiltX;
        s.tiltY      = input.tiltY;

        if (input.actionPressed && s.trickCooldownTimer <= 0f)
            s.trickRequested = true;
    }

    protected override void Update()
    {
        base.Update();
        if (!isPlaying) return;

        _waveTime += Time.deltaTime;

        foreach (var kv in _surfers)
        {
            var pid = kv.Key;
            var s   = kv.Value;
            if (s.rb == null) continue;

            // ── Wipeout recovery ──────────────────────────────────────────
            if (s.isWipingOut)
            {
                s.wipeoutTimer -= Time.deltaTime;
                if (s.wipeoutTimer <= 0f)
                {
                    s.isWipingOut     = false;
                    s.scoreMultiplier = 1f;
                    ControllerGateway.Instance?.SendUICommand(pid, "show_feedback", "Back up!");
                }
                continue;
            }

            // ── Sample wave height at board position ──────────────────────
            float waveY = SampleGerstnerY(s.rb.position);
            float surfaceY = waveY;

            // ── Buoyancy ──────────────────────────────────────────────────
            float depthUnderWater = surfaceY - s.rb.position.y;
            if (depthUnderWater > 0)
            {
                float buoyForce = depthUnderWater * boardBuoyancy - s.rb.linearVelocity.y * boardDamping;
                s.rb.AddForce(Vector3.up * buoyForce, ForceMode.Force);
                s.isAirborne = false;
                s.airTime    = 0f;
            }
            else
            {
                s.isAirborne = true;
                s.airTime   += Time.deltaTime;
            }

            // ── Steering ──────────────────────────────────────────────────
            s.boardTransform.Rotate(0, s.leanInput * steerSpeed * Time.deltaTime, 0, Space.World);

            // Forward propulsion
            var fwd = s.boardTransform.forward;
            fwd.y   = 0;
            s.rb.AddForce(fwd.normalized * forwardSpeed, ForceMode.Acceleration);

            // ── Balance / wipeout check ───────────────────────────────────
            float tiltMag = Mathf.Sqrt(s.tiltX * s.tiltX + s.tiltY * s.tiltY);
            if (tiltMag > wipeoutTiltThreshold && !s.isAirborne)
            {
                StartCoroutine(TriggerWipeout(pid, s));
                continue;
            }

            // ── Crest detection ───────────────────────────────────────────
            float crestThreshold = waveAmplitude * 0.6f;
            s.isOnCrest = (s.rb.position.y >= surfaceY - 0.5f) && (waveY >= crestThreshold);

            if (s.isOnCrest)
            {
                s.crestTimeTotal += Time.deltaTime;
                AddScore(pid, Mathf.RoundToInt(3f * s.scoreMultiplier * Time.deltaTime));
                ControllerGateway.Instance?.SendUICommand(pid, "show_multiplier", $"{s.scoreMultiplier:F1}×");
            }

            // ── Tricks ────────────────────────────────────────────────────
            s.trickCooldownTimer = Mathf.Max(0, s.trickCooldownTimer - Time.deltaTime);

            if (s.trickRequested && s.isAirborne && s.airTime >= airTimeForTrick)
            {
                StartCoroutine(ExecuteTrick(pid, s));
            }
            s.trickRequested = false;
        }
    }

    private IEnumerator TriggerWipeout(string playerId, SurferState s)
    {
        s.isWipingOut     = true;
        s.wipeoutTimer    = wipeoutRecoveryTime;
        s.scoreMultiplier = 1f;
        s.isOnCrest       = false;

        s.visuals?.OnHit();
        VFXManager.Instance?.Play(VFXManager.VFXType.HitSpark, s.rb.position);
        ControllerGateway.Instance?.SendHaptic(playerId, "hit");
        ControllerGateway.Instance?.SendUICommand(playerId, "show_feedback", "WIPEOUT!");

        // Submerge briefly
        s.rb.AddForce(Vector3.down * 12f, ForceMode.Impulse);

        yield return new WaitForSeconds(wipeoutRecoveryTime);
        // Reset handled in Update timer above
    }

    private IEnumerator ExecuteTrick(string playerId, SurferState s)
    {
        s.trickCooldownTimer = trickCooldown;
        s.trickRequested     = false;

        // Spin animation hint
        s.visuals?.OnDashStart();
        VFXManager.Instance?.Play(VFXManager.VFXType.ScoreBurst, s.rb.position + Vector3.up * 2f);
        ControllerGateway.Instance?.SendHaptic(playerId, "pickup");

        AddScore(playerId, 50);
        s.scoreMultiplier = Mathf.Min(maxMultiplier, s.scoreMultiplier + 0.5f);

        ControllerGateway.Instance?.SendUICommand(playerId, "show_feedback", $"+50 🏄 {s.scoreMultiplier:F1}×");

        yield return new WaitForSeconds(0.5f);
        s.visuals?.OnDashEnd();
    }

    /// <summary>
    /// Single-wave Gerstner Y displacement at world position p.
    /// Must match OceanWater.shader wave 1 parameters.
    /// </summary>
    private float SampleGerstnerY(Vector3 pos)
    {
        float k   = 2f * Mathf.PI / waveLength;
        float c   = Mathf.Sqrt(9.8f / k);
        float d   = waveDirectionX * pos.x + waveDirectionZ * pos.z;
        float phi = k * d - c * _waveTime;
        return waveAmplitude * Mathf.Sin(phi);
    }

    protected override void CalculateFinalScores()
    {
        // Crest-time bonus
        float topCrest = 0f;
        string topPid  = null;

        foreach (var kv in _surfers)
        {
            if (kv.Value.crestTimeTotal > topCrest)
            {
                topCrest = kv.Value.crestTimeTotal;
                topPid   = kv.Key;
            }
        }

        if (topPid != null)
        {
            AddScore(topPid, 200);
            Debug.Log($"[WaveRider] Longest crest bonus → {topPid} ({topCrest:F1}s)");
        }
    }

    protected override void OnGameStart()
    {
        Debug.Log("[WaveRider] ▶ Surf's up!");
        ControllerGateway.Instance?.BroadcastHaptic("game_start");
    }

    protected override void OnGameEnd()
    {
        Debug.Log("[WaveRider] ■ Hang loose!");
        foreach (var kv in _surfers)
        {
            if (!kv.Value.isWipingOut)
            {
                kv.Value.visuals?.OnWin();
                ControllerGateway.Instance?.SendHaptic(kv.Key, "win");
            }
        }
    }
}
