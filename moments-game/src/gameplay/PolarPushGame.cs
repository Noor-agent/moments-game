using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Polar Push — Mini-game #1.
/// Last player standing on the ice platform wins.
/// Proves: movement + dash + physical knockback mechanics.
/// TV-authoritative. Phones send intent: moveX/Y + dashPressed.
/// </summary>
public class PolarPushGame : MiniGameBase
{
    [Header("Polar Push Config")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float dashForce = 25f;
    [SerializeField] private float dashCooldown = 2f;
    [SerializeField] private float eliminationHeight = -5f;  // Y below platform
    [SerializeField] private float platformShrinkInterval = 20f;  // Ice cracks every 20s

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private IcePlatformManager platformManager;

    private readonly Dictionary<string, CharacterController> _controllers = new();
    private readonly Dictionary<string, float> _dashCooldowns = new();
    private readonly Dictionary<string, Vector2> _moveInputs = new();
    private readonly Dictionary<string, Rigidbody> _rigidbodies = new();

    private float _nextShrinkTime;

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        SpawnPlayers(players);
        _nextShrinkTime = platformShrinkInterval;
    }

    private void SpawnPlayers(List<PlayerData> players)
    {
        for (int i = 0; i < players.Count; i++)
        {
            var spawnPoint = spawnPoints[i % spawnPoints.Length];
            // In actual build: instantiate player prefab from CharacterDefinition
            // var prefab = players[i].characterDef.prefab3D;
            // var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            
            Debug.Log($"[PolarPush] Spawning {players[i].nickname} at {spawnPoint.position}");
            _dashCooldowns[players[i].playerId] = 0f;
            _moveInputs[players[i].playerId] = Vector2.zero;
        }
    }

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying) return;
        if (input.type != "input") return;

        _moveInputs[playerId] = new Vector2(input.moveX, input.moveY);

        if (input.dashPressed && _dashCooldowns.TryGetValue(playerId, out float cd) && cd <= 0f)
            TriggerDash(playerId);
    }

    private void TriggerDash(string playerId)
    {
        _dashCooldowns[playerId] = dashCooldown;
        ControllerGateway.Instance?.SendHaptic(playerId, "dash");

        if (_rigidbodies.TryGetValue(playerId, out var rb))
        {
            var dir = new Vector3(_moveInputs[playerId].x, 0, _moveInputs[playerId].y).normalized;
            if (dir == Vector3.zero) dir = rb.transform.forward;
            rb.AddForce(dir * dashForce, ForceMode.Impulse);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!isPlaying) return;

        // Apply movement to all players
        foreach (var player in activePlayers)
        {
            var pid = player.playerId;
            if (_rigidbodies.TryGetValue(pid, out var rb))
            {
                var input = _moveInputs.TryGetValue(pid, out var v) ? v : Vector2.zero;
                var moveDir = new Vector3(input.x, 0, input.y) * moveSpeed;
                rb.linearVelocity = new Vector3(moveDir.x, rb.linearVelocity.y, moveDir.z);
            }

            // Tick dash cooldowns
            if (_dashCooldowns.ContainsKey(pid))
                _dashCooldowns[pid] = Mathf.Max(0f, _dashCooldowns[pid] - Time.deltaTime);

            // Check elimination (fell off platform)
            if (_rigidbodies.TryGetValue(pid, out var rb2) && rb2.position.y < eliminationHeight)
                EliminatePlayer(pid);
        }

        // Platform shrink
        if (isPlaying && Time.time >= _nextShrinkTime)
        {
            platformManager?.ShrinkPlatform();
            _nextShrinkTime = Time.time + platformShrinkInterval;
        }
    }

    protected override void OnGameStart()
    {
        Debug.Log("[PolarPush] Game started — last one standing wins!");
    }

    protected override void OnGameEnd()
    {
        Debug.Log("[PolarPush] Game over!");
        // Trigger hero shots on TV + haptics for all
        foreach (var p in activePlayers)
            ControllerGateway.Instance?.SendHaptic(p.playerId, "win");
    }

    protected override void CalculateFinalScores()
    {
        // Surviving player(s) get maximum score
        int maxScore = 1000;
        foreach (var p in activePlayers)
            AddScore(p.playerId, maxScore);
        
        // Partial score for how long each eliminated player survived
        // (tracked separately via elimination timestamps in production)
    }
}
