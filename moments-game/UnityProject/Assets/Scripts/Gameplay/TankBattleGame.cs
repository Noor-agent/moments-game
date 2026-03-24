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
        // In production: instantiate colored tank prefab, bind to player slot color
        Debug.Log($"[TankBattle] Spawning tank for {player.nickname} at {position}");
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
        if (cannonballPrefab == null || tank.turret == null) return;
        var ball = Object.Instantiate(cannonballPrefab, tank.turret.position + tank.turret.forward, tank.turret.rotation);
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = tank.turret.forward * 20f;
        Destroy(ball, riccochetLifetime);
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
                // Trigger respawn coroutine
                Debug.Log($"[TankBattle] {targetPlayerId} respawning with {tank.hp} HP");
            }
            else
            {
                EliminatePlayer(targetPlayerId);
                AddScore(shooterPlayerId, 500);
            }
        }
    }

    protected override void CalculateFinalScores()
    {
        // Surviving tanks get bonus
        foreach (var p in activePlayers)
            if (_tanks.TryGetValue(p.playerId, out var tank))
                AddScore(p.playerId, tank.hp * 100);
    }

    protected override void OnGameStart() => Debug.Log("[TankBattle] Engines hot. Fire at will!");
    protected override void OnGameEnd() => Debug.Log("[TankBattle] Ceasefire!");
}
