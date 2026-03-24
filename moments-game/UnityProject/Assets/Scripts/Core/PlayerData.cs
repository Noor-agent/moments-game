using UnityEngine;
using System;

/// <summary>
/// PlayerData — runtime state for one connected player.
/// Serializable so Unity's JsonUtility can encode it for network snapshots.
/// TV-side only. Phones never hold a PlayerData object.
/// </summary>
[Serializable]
public class PlayerData
{
    // ── Identity ───────────────────────────────────────────────────────────
    public string playerId;         // Server-assigned GUID short (8 chars)
    public string nickname;         // Display name (max 12 chars, sanitized)
    public string heroId;           // e.g. "byte", "nova", "orbit"
    public string reconnectToken;   // Stored in phone localStorage

    // ── Slot / Color ───────────────────────────────────────────────────────
    public int    slot;             // 0–7, determines color + spawn
    public int    playerSlot;       // Alias for backwards compat
    public Color  playerColor;      // Assigned from SessionStateManager.PlayerColors

    // ── Connection ─────────────────────────────────────────────────────────
    public bool   isConnected;
    public bool   isReady;
    public float  disconnectTime;   // Time.time when disconnected (for grace window)

    // ── Score ──────────────────────────────────────────────────────────────
    public int    totalScore;       // Accumulated across all rounds this session
    public int    sessionScore;     // Alias used by ResultsAggregator (same as totalScore)
    public int    lastRoundScore;   // Points earned in the most recent mini-game
    public int    currentRoundScore;// Live round score (alias for lastRoundScore)
    public int    wins;             // Number of mini-games won
    public int    elimOrder;        // Elimination order this round (1 = first out)
    public int    placement;        // Session/round placement (1 = 1st)

    // ── Character reference ────────────────────────────────────────────────
    [NonSerialized] public CharacterDefinition characterDef;  // Resolved at game start
    [NonSerialized] public GameObject spawnedCharacter;       // Runtime prefab instance

    // ── Helpers ────────────────────────────────────────────────────────────

    /// <summary>Reset per-round state before each mini-game.</summary>
    public void ResetRoundState()
    {
        lastRoundScore   = 0;
        currentRoundScore = 0;
        elimOrder        = 0;
        placement        = 0;
        isReady          = false;
    }

    /// <summary>CSS hex string for this player's color (for sending to phone UI).</summary>
    public string ColorHex =>
        $"#{(int)(playerColor.r * 255):X2}{(int)(playerColor.g * 255):X2}{(int)(playerColor.b * 255):X2}";

    public override string ToString()
        => $"[Player {slot}] {nickname} | hero={heroId} | score={totalScore} | connected={isConnected}";
}
