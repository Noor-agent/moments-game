using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Polar Push — Mini-game #1.
/// Last player standing on the ice platform wins.
/// 
/// Mechanics:
///   - 8-directional movement with ice friction (slippery feel)
///   - Dash: burst of force in move direction (2s cooldown, haptic feedback)
///   - Physical knockback on collision (Rigidbody mass-based)
///   - Platform shrinks every 20s (IcePlatformManager cracks outer tiles)
///   - Fall below eliminationHeight → eliminated (VFX + haptic)
///   - Last player alive wins, others ranked by survival time
///
/// TV-authoritative. Phones send: moveX, moveY, dashPressed.
/// No game state exists on phone. All physics runs server-side (on TV).
/// </summary>
public class PolarPushGame : MiniGameBase
{
    protected override PostProcessingManager.ArenaPreset ArenaPostFX => PostProcessingManager.ArenaPreset.PolarPush;
    protected override string LayoutId => "polar-push";

    // ── Inspector Config ───────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float moveSpeed       = 7f;
    [SerializeField] private float iceFriction     = 0.88f;   // Slippery (0=slip, 1=instant stop)
    [SerializeField] private float maxVelocity     = 14f;
    [SerializeField] private float rotationSpeed   = 720f;    // Degrees/sec for smooth turning

    [Header("Dash")]
    [SerializeField] private float dashForce       = 22f;
    [SerializeField] private float dashCooldown    = 2f;
    [SerializeField] private float dashDuration    = 0.18f;   // How long dash immunity lasts

    [Header("Knockback")]
    [SerializeField] private float collisionForce  = 12f;     // Extra impulse on player-player hit
    [SerializeField] private float knockbackUpward = 2f;      // Slight upward kick for drama

    [Header("Elimination")]
    [SerializeField] private float eliminationHeight = -6f;
    [SerializeField] private float respawnGraceTime  = 0f;    // No respawn in Polar Push

    [Header("Platform")]
    [SerializeField] private float platformShrinkInterval = 20f;
    [SerializeField] private float countdownToFirstShrink = 10f;

    [Header("References")]
    [SerializeField] private Transform[]       spawnPoints;
    [SerializeField] private IcePlatformManager platformManager;
    [SerializeField] private GameObject[]      characterPrefabs;   // 8 hero prefabs, indexed by slot

    // ── Runtime State ──────────────────────────────────────────────────────
    private struct PlayerRuntime
    {
        public Rigidbody   rb;
        public Transform   transform;
        public CharacterVisuals visuals;
        public Vector2     moveInput;
        public float       dashCooldownRemaining;
        public bool        isDashing;
        public bool        isEliminated;
        public float       eliminatedAt;
        public int         elimOrder;
    }

    private readonly Dictionary<string, PlayerRuntime> _runtime = new();
    private float _nextShrinkTime;
    private int   _elimCounter;
    private int   _aliveCount;
    private bool  _setupDone;

    // ── MiniGameBase Overrides ─────────────────────────────────────────────

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        _elimCounter = 0;
        _aliveCount  = players.Count;
        _runtime.Clear();

        SpawnPlayers(players);

        _nextShrinkTime = Time.time + countdownToFirstShrink;
        _setupDone = true;

        // Brief camera shake on game start
        StartCoroutine(CountdownSequence());
    }

    private void SpawnPlayers(List<PlayerData> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            var player   = players[i];
            var spawnPt  = spawnPoints[i % spawnPoints.Length];

            // Instantiate prefab for this hero slot
            GameObject prefab = GetPrefabForSlot(player.slot);
            GameObject go     = Instantiate(prefab, spawnPt.position, spawnPt.rotation);
            go.name = $"Player_{player.nickname}_{player.slot}";
            player.spawnedCharacter = go;

            // Setup physics
            var rb = go.GetComponent<Rigidbody>();
            if (rb == null) rb = go.AddComponent<Rigidbody>();
            rb.mass             = 1f;
            rb.linearDamping    = 0f;   // We apply friction manually for ice feel
            rb.angularDamping   = 5f;
            rb.constraints      = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation    = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Setup visuals
            var visuals = go.GetComponent<CharacterVisuals>();
            visuals?.Initialize(player, player.characterDef);

            // Add collision handler
            var collHandler = go.AddComponent<PlayerCollisionHandler>();
            collHandler.Init(player.playerId, this);

            _runtime[player.playerId] = new PlayerRuntime
            {
                rb            = rb,
                transform     = go.transform,
                visuals       = visuals,
                moveInput     = Vector2.zero,
                dashCooldownRemaining = 0f,
                isDashing     = false,
                isEliminated  = false,
                elimOrder     = 0
            };

            Debug.Log($"[PolarPush] Spawned {player.nickname} at {spawnPt.position}");
        }
    }

    private IEnumerator CountdownSequence()
    {
        // 3-2-1 countdown before movement enabled
        isPlaying = false;
        ControllerGateway.Instance?.BroadcastHaptic("countdown");

        yield return new WaitForSeconds(1f);
        ControllerGateway.Instance?.BroadcastHaptic("countdown");
        yield return new WaitForSeconds(1f);
        ControllerGateway.Instance?.BroadcastHaptic("countdown");
        yield return new WaitForSeconds(1f);

        isPlaying = true;
        ControllerGateway.Instance?.BroadcastHaptic("game_start");
        VFXManager.Instance?.Confetti(Vector3.up * 3f);
    }

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying) return;
        if (!_runtime.TryGetValue(playerId, out var rt) || rt.isEliminated) return;

        rt.moveInput = new Vector2(input.moveX, input.moveY);

        if (input.actionPressed && rt.dashCooldownRemaining <= 0f && !rt.isDashing)
            StartCoroutine(ExecuteDash(playerId));

        _runtime[playerId] = rt;
    }

    private IEnumerator ExecuteDash(string playerId)
    {
        if (!_runtime.TryGetValue(playerId, out var rt)) yield break;

        // Apply dash force
        var dir = new Vector3(rt.moveInput.x, 0, rt.moveInput.y).normalized;
        if (dir == Vector3.zero) dir = rt.transform.forward;

        rt.rb.AddForce(dir * dashForce, ForceMode.Impulse);
        rt.isDashing = true;
        rt.dashCooldownRemaining = dashCooldown;
        _runtime[playerId] = rt;

        // Haptic + VFX
        ControllerGateway.Instance?.SendHaptic(playerId, "dash");
        rt.visuals?.OnDashStart();
        VFXManager.Instance?.Play(VFXManager.VFXType.DashTrail, rt.transform.position);

        // Impact on nearby tiles
        platformManager?.OnPlayerImpact(rt.transform.position);

        yield return new WaitForSeconds(dashDuration);

        if (_runtime.TryGetValue(playerId, out rt))
        {
            rt.isDashing = false;
            _runtime[playerId] = rt;
            rt.visuals?.OnDashEnd();
        }
    }

    // ── Called by PlayerCollisionHandler ──────────────────────────────────

    public void OnPlayerCollision(string attackerId, string victimId, Vector3 contactPoint)
    {
        if (!isPlaying) return;
        if (!_runtime.TryGetValue(attackerId, out var attacker) || attacker.isEliminated) return;
        if (!_runtime.TryGetValue(victimId,   out var victim)   || victim.isEliminated)   return;

        // Extra knockback if attacker is dashing
        float force = attacker.isDashing ? collisionForce * 1.8f : collisionForce;

        var knockDir = (victim.transform.position - attacker.transform.position).normalized;
        knockDir.y = knockbackUpward;
        knockDir.Normalize();

        victim.rb.AddForce(knockDir * force, ForceMode.Impulse);
        attacker.rb.AddForce(-knockDir * (force * 0.35f), ForceMode.Impulse);

        // VFX + haptic
        VFXManager.Instance?.HitSpark(contactPoint, victim.rb.GetComponent<Renderer>()?.material.color ?? Color.white);
        ControllerGateway.Instance?.SendHaptic(attackerId, attacker.isDashing ? "strong_hit" : "hit");
        ControllerGateway.Instance?.SendHaptic(victimId, "hit");

        victim.visuals?.OnHit();

        // Crack tile below impact
        platformManager?.OnPlayerImpact(contactPoint);
    }

    // ── FixedUpdate: physics movement ──────────────────────────────────────

    private void FixedUpdate()
    {
        if (!isPlaying || !_setupDone) return;

        foreach (var kv in _runtime)
        {
            var pid = kv.Key;
            var rt  = kv.Value;
            if (rt.isEliminated || rt.rb == null) continue;

            // Apply ice friction (lerp toward target velocity)
            var input      = rt.moveInput;
            var targetVel  = new Vector3(input.x, 0, input.y) * moveSpeed;
            var currentVel = new Vector3(rt.rb.linearVelocity.x, 0, rt.rb.linearVelocity.z);

            // Ice: smooth acceleration, slow deceleration
            var newVel = Vector3.Lerp(currentVel, targetVel, iceFriction * Time.fixedDeltaTime * 10f);

            // Clamp max horizontal speed
            if (newVel.magnitude > maxVelocity)
                newVel = newVel.normalized * maxVelocity;

            rt.rb.linearVelocity = new Vector3(newVel.x, rt.rb.linearVelocity.y, newVel.z);

            // Smooth character rotation to face move direction
            if (input.magnitude > 0.1f)
            {
                var targetRot = Quaternion.LookRotation(new Vector3(input.x, 0, input.y));
                rt.transform.rotation = Quaternion.RotateTowards(
                    rt.transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            }
        }
    }

    // ── Update: cooldowns + elimination check ──────────────────────────────

    protected override void Update()
    {
        base.Update();
        if (!isPlaying || !_setupDone) return;

        var keys = new List<string>(_runtime.Keys);
        foreach (var pid in keys)
        {
            var rt = _runtime[pid];
            if (rt.isEliminated) continue;

            // Tick dash cooldown
            if (rt.dashCooldownRemaining > 0f)
            {
                rt.dashCooldownRemaining -= Time.deltaTime;
                _runtime[pid] = rt;
            }

            // Elimination check
            if (rt.rb != null && rt.rb.position.y < eliminationHeight)
                StartCoroutine(EliminatePlayer(pid));
        }

        // Platform shrink schedule
        if (Time.time >= _nextShrinkTime && _aliveCount > 1)
        {
            platformManager?.ShrinkPlatform();
            _nextShrinkTime = Time.time + platformShrinkInterval;
        }
    }

    private new IEnumerator EliminatePlayer(string playerId)
    {
        if (!_runtime.TryGetValue(playerId, out var rt) || rt.isEliminated) yield break;

        rt.isEliminated = true;
        rt.elimOrder    = ++_elimCounter;
        rt.eliminatedAt = Time.time;
        _runtime[playerId] = rt;
        _aliveCount--;

        // Visuals + haptic
        rt.visuals?.OnEliminated();
        VFXManager.Instance?.ElimFlash(rt.transform.position, rt.rb.GetComponent<Renderer>()?.material.color ?? Color.grey);
        ControllerGateway.Instance?.SendHaptic(playerId, "eliminated");
        ControllerGateway.Instance?.SendUICommand(playerId, "show_eliminated", $"#{rt.elimOrder}");

        // Notify base game
        base.EliminatePlayer(playerId);

        Debug.Log($"[PolarPush] Player {playerId} eliminated (order {rt.elimOrder})");

        // Short pause then camera cut if 1 player left
        yield return new WaitForSeconds(0.5f);
        if (_aliveCount == 1) EndGame();
        if (_aliveCount == 0) EndGame(); // edge case: simultaneous
    }

    protected override void OnGameStart()
    {
        Debug.Log("[PolarPush] ▶ Game started — last one standing wins!");
    }

    protected override void OnGameEnd()
    {
        Debug.Log("[PolarPush] ■ Game over!");

        // Win celebration for survivors
        foreach (var kv in _runtime)
        {
            if (!kv.Value.isEliminated)
            {
                kv.Value.visuals?.OnWin();
                ControllerGateway.Instance?.SendHaptic(kv.Key, "win");
                VFXManager.Instance?.Confetti(kv.Value.transform.position + Vector3.up * 2f);
            }
        }
    }

    protected override void CalculateFinalScores()
    {
        // Scoring: 1000 pts survivor, ranked points for eliminated (200, 400, 600, 800)
        int[] elimPoints = { 200, 300, 450, 600, 750 };

        foreach (var kv in _runtime)
        {
            int points;
            if (!kv.Value.isEliminated)
                points = 1000;
            else
            {
                int idx = Mathf.Clamp(kv.Value.elimOrder - 1, 0, elimPoints.Length - 1);
                points = elimPoints[elimPoints.Length - 1 - idx];
            }
            AddScore(kv.Key, points);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private GameObject GetPrefabForSlot(int slot)
    {
        if (characterPrefabs != null && characterPrefabs.Length > slot % 8)
            return characterPrefabs[slot % 8];
        // Fallback: primitive capsule
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        return go;
    }
}

/// <summary>
/// Attached to each player GameObject. Detects collision with other players and notifies PolarPushGame.
/// </summary>
public class PlayerCollisionHandler : MonoBehaviour
{
    private string         _playerId;
    private PolarPushGame  _game;

    public void Init(string playerId, PolarPushGame game)
    {
        _playerId = playerId;
        _game     = game;
    }

    private void OnCollisionEnter(Collision col)
    {
        var other = col.gameObject.GetComponent<PlayerCollisionHandler>();
        if (other == null || _game == null) return;

        Vector3 contact = col.GetContact(0).point;
        _game.OnPlayerCollision(_playerId, other._playerId, contact);
    }
}
