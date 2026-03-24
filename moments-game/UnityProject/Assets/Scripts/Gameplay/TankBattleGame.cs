using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Tank Battle — Mini-game #3.
/// Destroy enemy tanks in a desert canyon arena.
/// Proves: steering + turret aiming + twin-stick combat.
/// Players get one respawn at half HP. Second elimination = out.
/// </summary>
public class TankBattleGame : MiniGameBase
{
    [Header("Tank Battle Config")]
    [SerializeField] private float tankSpeed = 7f;
    [SerializeField] private float turnSpeed = 180f;  // degrees/sec
    [SerializeField] private float fireRate = 3f;     // shots per second
    [SerializeField] private int maxHP = 4;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float riccochetLifetime = 5f;

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject cannonballPrefab;
    [SerializeField] private Transform[] coverBlocks;  // Destructible cover

    private class TankState
    {
        public string playerId;
        public int hp;
        public int respawnsRemaining = 1;
        public float fireCooldown;
        public float driveInput;
        public Vector2 aimInput;
        public bool fireRequested;
        public GameObject tankObject;
        public Transform turret;
    }

    private readonly Dictionary<string, TankState> _tanks = new();

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        for (int i = 0; i < players.Count; i++)
        {
            var p = players[i];
            _tanks[p.playerId] = new TankState
            {
                playerId = p.playerId,
                hp = maxHP
            };
            SpawnTank(p, spawnPoints[i % spawnPoints.Length].position);
        }
    }

    private void SpawnTank(PlayerData player, Vector3 position)
    {
        // Instantiate colored tank prefab; in production replace primitive with real prefab
        var go   = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name  = $"Tank_{player.nickname}";
        go.transform.position   = position;
        go.transform.localScale = new Vector3(1.4f, 0.7f, 2f);

        // Color tank to player color
        var mr = go.GetComponent<MeshRenderer>();
        if (mr)
        {
            var mpb = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", player.playerColor);
            mr.SetPropertyBlock(mpb);
        }

        // Rigidbody
        var rb  = go.AddComponent<Rigidbody>();
        rb.mass = 2f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ |
                         RigidbodyConstraints.FreezePositionY;
        rb.linearDamping     = 3f;
        rb.angularDamping    = 8f;
        rb.interpolation     = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // Turret (child object rotates independently)
        var turretGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        turretGo.name = "Turret";
        turretGo.transform.SetParent(go.transform);
        turretGo.transform.localPosition = new Vector3(0, 0.6f, 0.3f);
        turretGo.transform.localScale    = new Vector3(0.25f, 0.7f, 0.25f);
        turretGo.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Tank collision reporter
        var reporter = go.AddComponent<TankHitReporter>();
        reporter.Init(player.playerId, this);

        if (_tanks.TryGetValue(player.playerId, out var state))
        {
            state.tankObject = go;
            state.turret     = turretGo.transform;
        }

        Debug.Log($"[TankBattle] Spawned tank for {player.nickname} at {position}");
    }

    private IEnumerator RespawnTank(PlayerData player)
    {
        if (!_tanks.TryGetValue(player.playerId, out var tank)) yield break;

        // Destroy old object
        if (tank.tankObject != null)
            Object.Destroy(tank.tankObject);

        tank.tankObject = null;
        tank.turret     = null;

        ControllerGateway.Instance?.SendUICommand(player.playerId, "show_feedback",
            $"Respawning in {respawnDelay:F0}s…");

        yield return new WaitForSeconds(respawnDelay);

        // Pick farthest-from-enemy spawn point
        var bestSpawn = spawnPoints[0];
        float bestDist = 0f;
        foreach (var sp in spawnPoints)
        {
            float minEnemyDist = float.MaxValue;
            foreach (var kv in _tanks)
            {
                if (kv.Key == player.playerId || kv.Value.tankObject == null) continue;
                float d = Vector3.Distance(sp.position, kv.Value.tankObject.transform.position);
                if (d < minEnemyDist) minEnemyDist = d;
            }
            if (minEnemyDist > bestDist) { bestDist = minEnemyDist; bestSpawn = sp; }
        }

        SpawnTank(player, bestSpawn.position);

        VFXManager.Instance?.Play(VFXManager.VFXType.ScoreBurst, bestSpawn.position);
        ControllerGateway.Instance?.SendHaptic(player.playerId, "pickup");
        ControllerGateway.Instance?.SendUICommand(player.playerId, "show_feedback", "Back in action!");
    }

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying || input.type != "input") return;
        if (!_tanks.TryGetValue(playerId, out var tank)) return;

        // Drive = left joystick Y (forward/back)
        tank.driveInput = input.moveY;
        // Aim = right joystick (turret direction)
        tank.aimInput = new Vector2(input.aimX, input.aimY);
        // Fire
        if (input.firePressed && tank.fireCooldown <= 0f)
            tank.fireRequested = true;
    }

    protected override void Update()
    {
        base.Update();
        if (!isPlaying) return;

        foreach (var kv in _tanks)
        {
            var tank = kv.Value;
            if (tank.tankObject == null) continue;

            // Drive
            var fwd = tank.tankObject.transform.forward;
            tank.tankObject.transform.position += fwd * tank.driveInput * tankSpeed * Time.deltaTime;

            // Rotate toward aim direction
            if (tank.aimInput.sqrMagnitude > 0.1f)
            {
                var targetAngle = Mathf.Atan2(tank.aimInput.x, tank.aimInput.y) * Mathf.Rad2Deg;
                if (tank.turret != null)
                    tank.turret.rotation = Quaternion.RotateTowards(
                        tank.turret.rotation,
                        Quaternion.Euler(0, targetAngle, 0),
                        turnSpeed * Time.deltaTime
                    );
            }

            // Fire
            tank.fireCooldown = Mathf.Max(0f, tank.fireCooldown - Time.deltaTime);
            if (tank.fireRequested && tank.fireCooldown <= 0f)
            {
                FireCannon(tank);
                tank.fireCooldown = 1f / fireRate;
                tank.fireRequested = false;
                ControllerGateway.Instance?.SendHaptic(tank.playerId, "fire");
            }
        }
    }

    private void FireCannon(TankState tank)
    {
        if (tank.turret == null) return;

        GameObject ball;
        if (cannonballPrefab != null)
            ball = Object.Instantiate(cannonballPrefab, tank.turret.position + tank.turret.forward, tank.turret.rotation);
        else
        {
            ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.localScale = Vector3.one * 0.3f;
            ball.transform.position   = tank.turret.position + tank.turret.forward;
        }

        ball.tag = "Cannonball";
        var owner = ball.AddComponent<CannonballOwner>();
        owner.OwnerId = tank.playerId;

        var rb = ball.GetComponent<Rigidbody>();
        if (rb == null) rb = ball.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity = tank.turret.forward * 20f;

        Destroy(ball, riccochetLifetime);

        VFXManager.Instance?.Play(VFXManager.VFXType.ScoreBurst, tank.turret.position);
    }

    public void OnCannonballHit(string targetPlayerId, string shooterPlayerId)
    {
        if (!_tanks.TryGetValue(targetPlayerId, out var tank)) return;
        tank.hp--;
        ControllerGateway.Instance?.SendHaptic(targetPlayerId, "hit");

        if (tank.hp <= 0)
        {
            if (tank.respawnsRemaining > 0)
            {
                tank.respawnsRemaining--;
                tank.hp = maxHP / 2;  // Respawn at half HP

                // Find player data for respawn
                var playerData = activePlayers.Find(p => p.playerId == targetPlayerId);
                if (playerData != null)
                    StartCoroutine(RespawnTank(playerData));
                else
                    Debug.LogWarning($"[TankBattle] Could not find PlayerData for {targetPlayerId}");

                Debug.Log($"[TankBattle] {targetPlayerId} respawning with {tank.hp} HP");
            }
            else
            {
                // Destroy tank object visually
                if (tank.tankObject != null)
                {
                    VFXManager.Instance?.ElimFlash(tank.tankObject.transform.position,
                        tank.tankObject.GetComponent<MeshRenderer>()?.material.color ?? Color.grey);
                    Object.Destroy(tank.tankObject);
                    tank.tankObject = null;
                }
                EliminatePlayer(targetPlayerId);
                AddScore(shooterPlayerId, 500);

                // Count tanks still in activePlayers with a live object
                int alive = 0;
                foreach (var kv in _tanks)
                    if (kv.Value.tankObject != null &&
                        activePlayers.Exists(p => p.playerId == kv.Key))
                        alive++;
                if (alive <= 1) EndGame();
            }
        }
    }

    protected override void CalculateFinalScores()
    {
        // Surviving tanks get HP bonus
        foreach (var p in activePlayers)
            if (_tanks.TryGetValue(p.playerId, out var tank) && tank.tankObject != null)
            {
                AddScore(p.playerId, tank.hp * 100);
                tank.tankObject.GetComponent<CharacterVisuals>()?.OnWin();
                ControllerGateway.Instance?.SendHaptic(p.playerId, "win");
                VFXManager.Instance?.Confetti(tank.tankObject.transform.position + Vector3.up * 2f);
            }
    }

    protected override void OnGameStart()
    {
        Debug.Log("[TankBattle] Engines hot. Fire at will!");
        ControllerGateway.Instance?.BroadcastHaptic("game_start");
    }

    protected override void OnGameEnd() => Debug.Log("[TankBattle] Ceasefire!");
}

/// <summary>Attached to each tank. Routes cannonball collisions to TankBattleGame.</summary>
public class TankHitReporter : MonoBehaviour
{
    private string          _playerId;
    private TankBattleGame  _game;

    public void Init(string playerId, TankBattleGame game)
    {
        _playerId = playerId;
        _game     = game;
    }

    private void OnCollisionEnter(Collision col)
    {
        // If hit by a cannonball (tagged "Cannonball"), find shooter + notify game
        if (!col.gameObject.CompareTag("Cannonball")) return;
        var shooter = col.gameObject.GetComponent<CannonballOwner>();
        if (shooter == null) return;
        _game?.OnCannonballHit(_playerId, shooter.OwnerId);
        Object.Destroy(col.gameObject); // Cannonball consumed on hit
    }
}

/// <summary>Tiny component on each instantiated cannonball to track its owner.</summary>
public class CannonballOwner : MonoBehaviour
{
    public string OwnerId;
}
}
