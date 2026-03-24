using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for all Moments mini-games.
/// Enforces the shared lifecycle: Setup → Countdown → Play → EndGame → Results.
/// All scoring, elimination, and state must be managed here — TV-authoritative.
/// 
/// Subclasses implement:
///   - HandleInput()         — parse phone input packets
///   - OnGameStart()         — called when play begins (after countdown)
///   - OnGameEnd()           — celebration / cleanup
///   - CalculateFinalScores() — write final round points via AddScore()
/// </summary>
public abstract class MiniGameBase : MonoBehaviour
{
    [Header("Mini-Game Config")]
    public MiniGameDefinition definition;

    // ── Protected State ────────────────────────────────────────────────────
    protected float         timeRemaining;
    protected bool          isPlaying;
    protected List<PlayerData>          activePlayers  = new();
    protected Dictionary<string, int>   roundScores    = new();

    // ── Events ─────────────────────────────────────────────────────────────
    public event System.Action<Dictionary<string, int>> OnRoundComplete;
    public event System.Action<string>                  OnPlayerEliminated;

    // ── Unity Lifecycle ────────────────────────────────────────────────────

    protected virtual void Start()
    {
        if (ControllerGateway.Instance != null)
            ControllerGateway.Instance.OnInputReceived += ReceiveInput;

        // Register as active game so Gateway routes inputs here
        ControllerGateway.Instance?.SetActiveGame(this);
    }

    protected virtual void OnDestroy()
    {
        if (ControllerGateway.Instance != null)
            ControllerGateway.Instance.OnInputReceived -= ReceiveInput;
        ControllerGateway.Instance?.ClearActiveGame();
    }

    // ── Game Lifecycle ─────────────────────────────────────────────────────

    /// <summary>Called by MiniGameLoader after scene loads. Initialize everything here.</summary>
    public virtual void Setup(List<PlayerData> players)
    {
        activePlayers  = new List<PlayerData>(players);
        timeRemaining  = definition != null ? definition.durationSeconds : 90f;
        roundScores.Clear();
        foreach (var p in players)
        {
            roundScores[p.playerId] = 0;
            p.ResetRoundState();
        }

        // Push the correct controller layout to all phones
        string layoutId = definition?.controllerLayout ?? "MoveAndDash";
        foreach (var p in players)
            ControllerGateway.Instance?.SendUICommand(p.playerId, "set_layout", layoutId);

        Debug.Log($"[MiniGame] Setup: {GetType().Name} with {players.Count} players");
    }

    public void StartGame()
    {
        isPlaying = true;
        SessionStateManager.Instance?.StartMiniGame(definition?.gameId ?? GetType().Name);
        OnGameStart();
        Debug.Log($"[MiniGame] ▶ {GetType().Name} started");
    }

    // ── Update ─────────────────────────────────────────────────────────────

    protected virtual void Update()
    {
        if (!isPlaying) return;

        timeRemaining -= Time.deltaTime;

        // Broadcast time to phones every 5 seconds
        if (Mathf.FloorToInt(timeRemaining) % 5 == 0 && timeRemaining > 0)
            ControllerGateway.Instance?.BroadcastStateSnapshot();

        if (timeRemaining <= 0f) EndGame();
    }

    // ── End Game ───────────────────────────────────────────────────────────

    protected void EndGame()
    {
        if (!isPlaying) return;
        isPlaying = false;

        CalculateFinalScores();

        // Write round scores to session totals
        foreach (var kv in roundScores)
        {
            SessionStateManager.Instance?.AddScore(kv.Key, kv.Value);
            var p = SessionStateManager.Instance?.GetPlayer(kv.Key);
            if (p != null) p.lastRoundScore = kv.Value;
        }

        OnRoundComplete?.Invoke(roundScores);
        SessionStateManager.Instance?.EndMiniGame();
        OnGameEnd();

        Debug.Log($"[MiniGame] ■ {GetType().Name} ended");
    }

    // ── Elimination ────────────────────────────────────────────────────────

    /// <summary>Remove a player from activePlayers. Subclasses call base.EliminatePlayer() after their own logic.</summary>
    protected void EliminatePlayer(string playerId)
    {
        activePlayers.RemoveAll(p => p.playerId == playerId);
        OnPlayerEliminated?.Invoke(playerId);
        Debug.Log($"[MiniGame] Player eliminated: {playerId} — {activePlayers.Count} remaining");
    }

    // ── Scoring ────────────────────────────────────────────────────────────

    protected void AddScore(string playerId, int points)
    {
        if (roundScores.ContainsKey(playerId))
            roundScores[playerId] += points;
    }

    protected int GetScore(string playerId)
        => roundScores.TryGetValue(playerId, out int s) ? s : 0;

    // ── Input Bridge ───────────────────────────────────────────────────────

    /// <summary>Called by ControllerGateway on main thread. Routes to HandleInput().</summary>
    public void ReceiveInput(string playerId, InputMessage input)
        => HandleInput(playerId, input);

    // ── Abstracts ──────────────────────────────────────────────────────────

    protected abstract void HandleInput(string playerId, InputMessage input);
    protected abstract void OnGameStart();
    protected abstract void OnGameEnd();
    protected abstract void CalculateFinalScores();
}

/// <summary>
/// Parsed input packet from phone controller.
/// Unity JsonUtility-serializable.
/// </summary>
[System.Serializable]
public class InputMessage
{
    public string type       = "input";
    public string playerId;      // Filled in by Gateway before routing

    // Movement (joystick: -1 to +1)
    public float moveX;
    public float moveY;

    // Right stick / aim joystick (Tank Battle twin-stick)
    public float aimX;
    public float aimY;

    // Actions (true on the frame the button is held)
    public bool  actionPressed;   // Primary: dash, grab, bump, fire, trick
    public bool  action2Pressed;  // Secondary: shield, slam
    public bool  duckPressed;     // Wave Rider duck/crouch

    // Tilt (Wave Rider balance)
    public float tiltX;
    public float tiltY;

    // Touch coordinates normalised -1..1 (Blink Shot aim tap on pad)
    public float touchX;
    public float touchY;
    public bool  touchDown;

    // Timestamp (phone-side, for latency calc)
    public long  ts;
}
