using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Gravity Grab — Mini-game #8 (final).
/// Zero-gravity arena. Grab floating orbs to score; slam opponents to steal their orbs.
///
/// Mechanics:
///   - Free-flying movement: moveX/Y drives thrust in 2D; no gravity
///   - actionPressed = Grab / Slam (context-sensitive):
///       · Near an orb → Grab it (carried on player)
///       · Near an opponent carrying orbs → Slam into them (steal half their orbs)
///   - Carrying orbs increases mass (slows you), attracts smaller orbs
///   - Orbs respawn at random positions after 5s
///   - Orb values: White=1pt, Blue=3pt, Gold=5pt (rarer)
///   - Game ends at time limit; most total orb value wins
///
/// TV-authoritative. Phones send: moveX, moveY, actionPressed.
/// </summary>
public class GravityGrabGame : MiniGameBase
{
    // ── Inspector Config ──────────────────────────────────────────────────
    [Header("Flight Physics")]
    [SerializeField] private float thrustForce       = 12f;
    [SerializeField] private float linearDamping     = 1.2f;    // Space drag
    [SerializeField] private float maxSpeed          = 15f;
    [SerializeField] private float carryMassAdd      = 0.3f;    // Per-orb extra mass

    [Header("Grab")]
    [SerializeField] private float grabRadius        = 2f;
    [SerializeField] private float slamRadius        = 2.5f;
    [SerializeField] private float slamImpulse       = 22f;
    [SerializeField] private float grabCooldown      = 0.5f;

    [Header("Orb Spawning")]
    [SerializeField] private int   whiteOrbCount     = 8;
    [SerializeField] private int   blueOrbCount      = 4;
    [SerializeField] private int   goldOrbCount      = 2;
    [SerializeField] private float orbRespawnDelay   = 5f;
    [SerializeField] private float arenaRadius       = 12f;      // Circular arena bounds

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;

    // ── Orb Data ──────────────────────────────────────────────────────────
    private enum OrbTier { White = 1, Blue = 3, Gold = 5 }

    private class Orb
    {
        public GameObject obj;
        public Rigidbody  rb;
        public OrbTier    tier;
        public string     carriedBy;      // null = floating free
        public bool       respawning;
    }

    // ── Per-player runtime ────────────────────────────────────────────────
    private class GravState
    {
        public Rigidbody        rb;
        public Transform        body;
        public CharacterVisuals visuals;

        // Input
        public Vector2 thrustInput;
        public bool    grabPressed;

        // State
        public List<Orb>  heldOrbs      = new();
        public float      grabCooldownT;
        public bool       isEliminated;

        // Score: 10pts per orb-value-point
        public int OrbScore
        {
            get
            {
                int s = 0;
                foreach (var o in heldOrbs) s += (int)o.tier;
                return s;
            }
        }
    }

    private readonly Dictionary<string, GravState> _players2 = new();
    private readonly List<Orb>                      _orbs     = new();

    // ── Setup ─────────────────────────────────────────────────────────────

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        _players2.Clear();
        _orbs.Clear();

        // Spawn players
        for (int i = 0; i < players.Count; i++)
        {
            var p     = players[i];
            var spawn = spawnPoints[i % spawnPoints.Length];

            var go = new GameObject($"GravPlayer_{p.nickname}");
            go.transform.position = spawn.position;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity        = false;
            rb.mass              = 1f;
            rb.linearDamping     = linearDamping;
            rb.angularDamping    = 10f;
            rb.constraints       = RigidbodyConstraints.FreezeRotationX |
                                   RigidbodyConstraints.FreezeRotationZ |
                                   RigidbodyConstraints.FreezePositionY; // 2D plane
            rb.interpolation     = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var col = go.AddComponent<SphereCollider>();
            col.radius = 0.5f;

            var visuals = go.GetComponent<CharacterVisuals>();
            visuals?.Initialize(p, p.characterDef);

            _players2[p.playerId] = new GravState
            {
                rb      = rb,
                body    = go.transform,
                visuals = visuals,
            };
        }

        // Spawn orbs
        SpawnOrbBatch(OrbTier.White, whiteOrbCount);
        SpawnOrbBatch(OrbTier.Blue,  blueOrbCount);
        SpawnOrbBatch(OrbTier.Gold,  goldOrbCount);
    }

    private void SpawnOrbBatch(OrbTier tier, int count)
    {
        for (int i = 0; i < count; i++)
            SpawnOrb(tier, RandomArenaPoint());
    }

    private Orb SpawnOrb(OrbTier tier, Vector3 pos)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.transform.localScale = Vector3.one * (tier == OrbTier.Gold ? 0.5f : tier == OrbTier.Blue ? 0.38f : 0.28f);
        go.transform.position   = pos;

        var rb        = go.GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearDamping      = 0.8f;

        // Color
        var mr  = go.GetComponent<MeshRenderer>();
        if (mr) mr.material.color = tier switch
        {
            OrbTier.Gold  => new Color(1f, 0.84f, 0f),
            OrbTier.Blue  => new Color(0.2f, 0.6f, 1f),
            _             => Color.white,
        };

        var orb = new Orb { obj = go, rb = rb, tier = tier };
        _orbs.Add(orb);
        return orb;
    }

    // ── Input ─────────────────────────────────────────────────────────────

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying) return;
        if (!_players2.TryGetValue(playerId, out var g) || g.isEliminated) return;

        g.thrustInput = new Vector2(input.moveX, input.moveY);
        if (input.actionPressed) g.grabPressed = true;
    }

    // ── Update ────────────────────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();
        if (!isPlaying) return;

        foreach (var kv in _players2)
        {
            var pid = kv.Key;
            var g   = kv.Value;
            if (g.isEliminated || g.rb == null) continue;

            // Cooldown
            g.grabCooldownT = Mathf.Max(0, g.grabCooldownT - Time.deltaTime);

            // Grab/Slam action
            if (g.grabPressed && g.grabCooldownT <= 0f)
            {
                TryGrabOrSlam(pid, g);
                g.grabPressed   = false;
                g.grabCooldownT = grabCooldown;
            }

            // Orbit carried orbs around player
            for (int i = 0; i < g.heldOrbs.Count; i++)
            {
                var orb = g.heldOrbs[i];
                if (orb.obj == null) continue;
                float angle = Time.time * 120f + i * (360f / Mathf.Max(g.heldOrbs.Count, 1));
                float r     = 1.0f + i * 0.2f;
                orb.obj.transform.position = g.body.position + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * r, 0,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * r);
            }

            // Arena boundary push (soft)
            float dist = Vector3.Distance(g.rb.position, Vector3.zero);
            if (dist > arenaRadius)
            {
                var inward = (Vector3.zero - g.rb.position).normalized;
                g.rb.AddForce(inward * 20f, ForceMode.Force);
            }

            // Update mass based on orbs carried
            g.rb.mass = 1f + g.heldOrbs.Count * carryMassAdd;
        }
    }

    private void FixedUpdate()
    {
        if (!isPlaying) return;

        foreach (var kv in _players2)
        {
            var g = kv.Value;
            if (g.isEliminated || g.rb == null) continue;

            // Thrust in XZ plane
            var thrust = new Vector3(g.thrustInput.x, 0, g.thrustInput.y) * thrustForce;
            g.rb.AddForce(thrust, ForceMode.Force);

            // Clamp speed
            var hVel = new Vector3(g.rb.linearVelocity.x, 0, g.rb.linearVelocity.z);
            if (hVel.magnitude > maxSpeed)
            {
                hVel = hVel.normalized * maxSpeed;
                g.rb.linearVelocity = new Vector3(hVel.x, 0, hVel.z);
            }
        }
    }

    // ── Grab / Slam ───────────────────────────────────────────────────────

    private void TryGrabOrSlam(string playerId, GravState g)
    {
        // 1. Check for slam target (opponent with orbs)
        foreach (var kv in _players2)
        {
            if (kv.Key == playerId || kv.Value.isEliminated) continue;
            var other = kv.Value;
            if (other.heldOrbs.Count == 0) continue;

            float dist = Vector3.Distance(g.body.position, other.body.position);
            if (dist <= slamRadius)
            {
                ExecuteSlam(playerId, g, kv.Key, other);
                return;
            }
        }

        // 2. Check for free orb nearby
        Orb nearest     = null;
        float nearestD  = float.MaxValue;

        foreach (var orb in _orbs)
        {
            if (orb.respawning || orb.carriedBy != null || orb.obj == null) continue;
            float d = Vector3.Distance(g.body.position, orb.obj.transform.position);
            if (d < grabRadius && d < nearestD)
            {
                nearest  = orb;
                nearestD = d;
            }
        }

        if (nearest != null)
        {
            nearest.carriedBy = playerId;
            nearest.rb.isKinematic = true;
            g.heldOrbs.Add(nearest);

            VFXManager.Instance?.Play(VFXManager.VFXType.ScoreBurst, nearest.obj.transform.position);
            ControllerGateway.Instance?.SendHaptic(playerId, "pickup");
            ControllerGateway.Instance?.SendUICommand(playerId, "show_feedback",
                $"Got {(int)nearest.tier}pt orb!");
        }
    }

    private void ExecuteSlam(string attackerId, GravState attacker, string victimId, GravState victim)
    {
        if (victim.heldOrbs.Count == 0) return;

        // Steal half (rounded up)
        int stealCount = Mathf.CeilToInt(victim.heldOrbs.Count / 2f);
        var stolen     = victim.heldOrbs.GetRange(0, stealCount);
        victim.heldOrbs.RemoveRange(0, stealCount);

        foreach (var orb in stolen)
        {
            orb.carriedBy = attackerId;
            attacker.heldOrbs.Add(orb);
        }

        // Impulse on victim
        var dir = (victim.body.position - attacker.body.position).normalized;
        victim.rb.AddForce(dir * slamImpulse, ForceMode.Impulse);

        // Feedback
        VFXManager.Instance?.HitSpark(victim.body.position, victim.visuals?.PlayerColor ?? Color.white);
        ControllerGateway.Instance?.SendHaptic(attackerId, "strong_hit");
        ControllerGateway.Instance?.SendHaptic(victimId, "hit");
        victim.visuals?.OnHit();

        AddScore(attackerId, stealCount * 20); // Bonus per stolen orb
        ControllerGateway.Instance?.SendUICommand(attackerId, "show_feedback",
            $"Stole {stealCount} orb{(stealCount > 1 ? "s" : "")}!");
        ControllerGateway.Instance?.SendUICommand(victimId, "show_feedback",
            $"Lost {stealCount} orb{(stealCount > 1 ? "s" : "")}!");

        // Respawn victim's mass update happens next Update()
        attacker.rb.mass = 1f + attacker.heldOrbs.Count * carryMassAdd;
        victim.rb.mass   = 1f + victim.heldOrbs.Count   * carryMassAdd;
    }

    // ── Final Scores ──────────────────────────────────────────────────────

    protected override void CalculateFinalScores()
    {
        int topScore = -1;
        string topPid = null;

        foreach (var kv in _players2)
        {
            var g     = kv.Value;
            int orbPts = g.OrbScore;
            AddScore(kv.Key, orbPts * 10);  // 10 pts per orb-value-point
            Debug.Log($"[GravityGrab] {kv.Key}: {g.heldOrbs.Count} orbs = {orbPts} orb-value → {orbPts * 10} pts");

            if (orbPts > topScore) { topScore = orbPts; topPid = kv.Key; }
        }

        // Winner bonus
        if (topPid != null)
        {
            AddScore(topPid, 300);
            if (_players2.TryGetValue(topPid, out var winner))
            {
                winner.visuals?.OnWin();
                VFXManager.Instance?.Confetti(winner.body.position + Vector3.up * 2f);
                ControllerGateway.Instance?.SendHaptic(topPid, "win");
            }
        }
    }

    protected override void OnGameStart()
    {
        Debug.Log("[GravityGrab] ▶ Zero-G — grab everything!");
        ControllerGateway.Instance?.BroadcastHaptic("game_start");
    }

    protected override void OnGameEnd()
    {
        // Drop all held orbs
        foreach (var kv in _players2)
            foreach (var orb in kv.Value.heldOrbs)
                if (orb.obj != null) orb.rb.isKinematic = false;

        Debug.Log("[GravityGrab] ■ Gravity returns.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private Vector3 RandomArenaPoint()
    {
        var rng = Random.insideUnitCircle * (arenaRadius * 0.8f);
        return new Vector3(rng.x, 0, rng.y);
    }

    private static int SumOrbValues(List<int> list)
    {
        int s = 0; foreach (var v in list) s += v; return s;
    }
}
