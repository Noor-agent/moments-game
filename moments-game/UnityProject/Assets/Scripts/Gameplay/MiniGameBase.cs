using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Base class for all Moments mini-games.
/// Enforces the shared lifecycle: Setup → Intro → Play → Results.
/// All scoring, elimination, and state must be managed here (TV-authoritative).
/// </summary>
public abstract class MiniGameBase : MonoBehaviour
{
    [Header("Mini-Game Config")]
    public MiniGameDefinition definition;

    protected float timeRemaining;
    protected bool isPlaying;
    protected List<PlayerData> activePlayers = new();
    protected Dictionary<string, int> roundScores = new();

    public event System.Action<Dictionary<string, int>> OnRoundComplete;

    protected virtual void Start()
    {
        // Subscribe to controller gateway input
        if (ControllerGateway.Instance != null)
            ControllerGateway.Instance.OnInputReceived += HandleInput;
    }

    /// <summary>
    /// Called by MiniGameLoader after scene loads. Initializes players.
    /// </summary>
    public virtual void Setup(List<PlayerData> players)
    {
        activePlayers = new List<PlayerData>(players);
        timeRemaining = definition.durationSeconds;
        foreach (var p in players) roundScores[p.playerId] = 0;

        // Push controller layout to all phones
        foreach (var p in players)
            ControllerGateway.Instance?.SendControllerLayout(p.playerId, definition.gameId);
    }

    public virtual void StartGame()
    {
        isPlaying = true;
        OnGameStart();
    }

    protected virtual void Update()
    {
        if (!isPlaying) return;
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f) EndGame();
    }

    protected virtual void EndGame()
    {
        isPlaying = false;
        CalculateFinalScores();
        OnRoundComplete?.Invoke(roundScores);
        OnGameEnd();
    }

    protected void EliminatePlayer(string playerId)
    {
        activePlayers.RemoveAll(p => p.playerId == playerId);
        ControllerGateway.Instance?.SendHaptic(playerId, "eliminate");
        if (activePlayers.Count <= 1) EndGame();
    }

    protected void AddScore(string playerId, int points)
    {
        if (roundScores.ContainsKey(playerId))
            roundScores[playerId] += points;
    }

    protected abstract void HandleInput(string playerId, InputMessage input);
    protected abstract void OnGameStart();
    protected abstract void OnGameEnd();
    protected abstract void CalculateFinalScores();
}
