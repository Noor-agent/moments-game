using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Bumper Blitz — Mini-game #6.
/// High-speed bumper-car arena. Ram opponents off the edge or into hazards.
/// Boost pads + rotating bumpers add chaos; survive longest / deal most damage wins.
///
/// Scoring:
///   - Dealing knockback (hit): +80 pts per confirmed hit
///   - Eliminating opponent:    +300 pts
///   - Riding boost pad:        no score, grants 3s turbo cooldown reset
///   - Survivor bonus:          +200 pts (last alive) or +100 pts (still alive at time end)
///
/// TV-authoritative. Phones send: moveX, moveY (steering + throttle), actionPressed (boost).
/// </summary>
public class BumperBlitzGame : MiniGameBase
{
    protected override PostProcessingManager.ArenaPreset ArenaPostFX => PostProcessingManager.ArenaPreset.BumperBlitz;
    protected override string LayoutId => "bumper-blitz";

    // ── Inspector Config ──────────────────────────────────────────────────
    [Header("Car Physics")]
    [SerializeField] private float driveForce     = 18f;
    [SerializeField] private float steerTorque    = 120f;    // Nm applied on Y axis
    [SerializeField] private float maxSpeed       = 16f;
    [SerializeField] private float linearDamping  = 2.5f;
    [SerializeField] private float boostForce     = 35f;
    [SerializeField] private float boostDuration  = 0.6f;
    [SerializeField] private float boostCooldown  = 6f;

    [Header("Collision")]
    [SerializeField] private float bumpImpulse    = 20f;     // Impulse on collision
    [SerializeField] private float minBumpSpeed   = 3f;      // Relative speed threshold to score
    [SerializeField] private float upwardKick     = 1.5f;    // Slight upward component on hit

    [Header("Elimination")]
    [SerializeField] private float fallHeight     = -5f;
    [SerializeField] private float edgePushForce  = 8f;      // Applied outward when near edge

    [Header("Arena Hazards")]
    [SerializeField] private RotatingBumper[] hazardBumpers;
    [SerializeField] private Transform[]      boostPads;
    [SerializeField] private float            boostPadRadius = 1.5f;

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;

    // ── Per-player runtime ────────────────────────────────────────────────
    private class CarState
    {
        public Rigidbody    rb;
        public Transform    carTransform;
        public CharacterVisuals visuals;

        // Input
        public float throttle;    // moveY: forward/back
        public float steer;       // moveX: left/right
        public bool  boostPressed;

        // State
        public float boostCooldownTimer;
        public bool  isBoosting;
        public bool  isEliminated;
        public int   hitsDealt;
    }

    private readonly Dictionary<string, CarState> _cars = new();
    private int _aliveCount;

    // ── Setup ─────────────────────────────────────────────────────────────

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        _cars.Clear();
        _aliveCount = players.Count;

        for (int i = 0; i < players.Count; i++)
        {
            var p     = players[i];
            var spawn = spawnPoints[i % spawnPoints.Length];

            var go   = new GameObject($"BumperCar_{p.nickname}");
            go.transform.SetPositionAndRotation(spawn.position, spawn.rotation);

            // Rigidbody
            var rb = go.AddComponent<Rigidbody>();
            rb.mass              = 1.5f;
            rb.linearDamping     = linearDamping;
            rb.angularDamping    = 10f;
            rb.constraints       = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation     = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Box collider
            var col  = go.AddComponent<BoxCollider>();
            col.size = new Vector3(1.2f, 0.8f, 1.6f);

            // Collision handler
            var handler = go.AddComponent<BumperCarCollisionHandler>();

            var visuals = go.GetComponent<CharacterVisuals>();
            visuals?.Initialize(p, p.characterDef);

            var state = new CarState
            {
                rb           = rb,
                carTransform = go.transform,
                visuals      = visuals,
            };

            handler.Init(p.playerId, this);
            _cars[p.playerId] = state;
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying) return;
        if (!_cars.TryGetValue(playerId, out var car) || car.isEliminated) return;

        car.throttle     = input.moveY;
        car.steer        = input.moveX;
        car.boostPressed = input.actionPressed;
    }

    // ── Update ────────────────────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();
        if (!isPlaying) return;

        foreach (var kv in _cars)
        {
            var pid = kv.Key;
            var car = kv.Value;
            if (car.isEliminated || car.rb == null) continue;

            // Boost cooldown tick
            if (car.boostCooldownTimer > 0f)
                car.boostCooldownTimer -= Time.deltaTime;

            // Trigger boost
            if (car.boostPressed && car.boostCooldownTimer <= 0f && !car.isBoosting)
                StartCoroutine(ApplyBoost(pid, car));

            // Check boost pad overlap
            foreach (var pad in boostPads)
            {
                if (pad == null) continue;
                if (Vector3.Distance(car.rb.position, pad.position) < boostPadRadius)
                {
                    car.boostCooldownTimer = 0f; // Instant reset
                    VFXManager.Instance?.Play(VFXManager.VFXType.ScoreBurst, pad.position);
                }
            }

            // Clamp speed
            var hVel = new Vector3(car.rb.linearVelocity.x, 0, car.rb.linearVelocity.z);
            if (hVel.magnitude > maxSpeed)
            {
                hVel = hVel.normalized * maxSpeed;
                car.rb.linearVelocity = new Vector3(hVel.x, car.rb.linearVelocity.y, hVel.z);
            }

            // Elimination fall check
            if (car.rb.position.y < fallHeight)
                StartCoroutine(EliminateCar(pid, car));
        }
    }

    private void FixedUpdate()
    {
        if (!isPlaying) return;

        foreach (var kv in _cars)
        {
            var car = kv.Value;
            if (car.isEliminated || car.rb == null || car.isBoosting) continue;

            // Drive force
            var fwd   = car.carTransform.forward;
            car.rb.AddForce(fwd * car.throttle * driveForce, ForceMode.Force);

            // Steer torque (only when moving)
            float speed = car.rb.linearVelocity.magnitude;
            if (speed > 0.5f)
                car.rb.AddTorque(Vector3.up * car.steer * steerTorque * Time.fixedDeltaTime, ForceMode.Force);
        }
    }

    // ── Boost ─────────────────────────────────────────────────────────────

    private IEnumerator ApplyBoost(string playerId, CarState car)
    {
        car.isBoosting         = true;
        car.boostCooldownTimer = boostCooldown;

        var dir = car.carTransform.forward;
        car.rb.AddForce(dir * boostForce, ForceMode.Impulse);

        car.visuals?.OnDashStart();
        VFXManager.Instance?.Play(VFXManager.VFXType.DashTrail, car.rb.position);
        ControllerGateway.Instance?.SendHaptic(playerId, "dash");

        yield return new WaitForSeconds(boostDuration);

        car.isBoosting = false;
        car.visuals?.OnDashEnd();
    }

    // ── Collision (called by BumperCarCollisionHandler) ───────────────────

    public void OnCarCollision(string attackerId, string victimId, Vector3 contactNormal, float relativeSpeed)
    {
        if (!isPlaying) return;
        if (!_cars.TryGetValue(attackerId, out var attacker) || attacker.isEliminated) return;
        if (!_cars.TryGetValue(victimId,   out var victim)   || victim.isEliminated)   return;

        if (relativeSpeed < minBumpSpeed) return; // Soft nudge, don't score

        // Apply impulse to victim
        var bumpDir  = (contactNormal + Vector3.up * upwardKick).normalized;
        float force  = attacker.isBoosting ? bumpImpulse * 1.6f : bumpImpulse;
        victim.rb.AddForce(bumpDir * force, ForceMode.Impulse);

        // Slight rebound on attacker
        attacker.rb.AddForce(-contactNormal * force * 0.3f, ForceMode.Impulse);

        // Score + haptics
        attacker.hitsDealt++;
        AddScore(attackerId, 80);

        VFXManager.Instance?.HitSpark(victim.rb.position, victim.visuals?.PlayerColor ?? Color.white);
        ControllerGateway.Instance?.SendHaptic(attackerId, attacker.isBoosting ? "strong_hit" : "hit");
        ControllerGateway.Instance?.SendHaptic(victimId, "hit");

        victim.visuals?.OnHit();
    }

    // ── Elimination ───────────────────────────────────────────────────────

    private IEnumerator EliminateCar(string playerId, CarState car)
    {
        if (car.isEliminated) yield break;
        car.isEliminated = true;
        _aliveCount--;

        car.visuals?.OnEliminated();
        VFXManager.Instance?.ElimFlash(car.rb.position, car.visuals?.PlayerColor ?? Color.grey);
        ControllerGateway.Instance?.SendHaptic(playerId, "eliminated");
        ControllerGateway.Instance?.SendUICommand(playerId, "show_eliminated", "OUT!");

        base.EliminatePlayer(playerId);

        yield return new WaitForSeconds(0.3f);
        if (_aliveCount <= 1) EndGame();
    }

    // ── Scores ────────────────────────────────────────────────────────────

    protected override void CalculateFinalScores()
    {
        foreach (var kv in _cars)
        {
            var car = kv.Value;
            if (!car.isEliminated)
            {
                AddScore(kv.Key, _aliveCount == 1 ? 200 : 100); // Survivor bonus
                car.visuals?.OnWin();
                ControllerGateway.Instance?.SendHaptic(kv.Key, "win");
                VFXManager.Instance?.Confetti(car.rb.position + Vector3.up * 2f);
            }
        }
    }

    protected override void OnGameStart()
    {
        Debug.Log("[BumperBlitz] ▶ BUMP!");
        ControllerGateway.Instance?.BroadcastHaptic("game_start");
    }

    protected override void OnGameEnd()
    {
        Debug.Log("[BumperBlitz] ■ Wreck!");
    }
}

// ── Helper Components ─────────────────────────────────────────────────────

/// <summary>Rotating hazard bumper in the arena. Applies force on contact.</summary>
public class RotatingBumper : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 60f;   // Degrees/sec
    [SerializeField] private float bumpForce   = 15f;

    private void Update() => transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.World);

    private void OnCollisionEnter(Collision col)
    {
        var rb = col.rigidbody;
        if (rb == null) return;
        var dir = (col.contacts[0].point - transform.position).normalized;
        dir.y = 0.4f;
        rb.AddForce(dir.normalized * bumpForce, ForceMode.Impulse);
    }
}

/// <summary>Per-car collision reporter.</summary>
public class BumperCarCollisionHandler : MonoBehaviour
{
    private string          _playerId;
    private BumperBlitzGame _game;

    public void Init(string playerId, BumperBlitzGame game)
    {
        _playerId = playerId;
        _game     = game;
    }

    private void OnCollisionEnter(Collision col)
    {
        if (_game == null) return;
        var other = col.gameObject.GetComponent<BumperCarCollisionHandler>();
        if (other == null) return;

        var rb      = GetComponent<Rigidbody>();
        var otherRb = col.rigidbody;
        float relSpeed = rb != null && otherRb != null
            ? (rb.linearVelocity - otherRb.linearVelocity).magnitude
            : 0f;

        var normal = col.contacts[0].normal;
        _game.OnCarCollision(_playerId, other._playerId, normal, relSpeed);
    }
}
