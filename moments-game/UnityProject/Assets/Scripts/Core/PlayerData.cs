using System;

/// <summary>
/// Serializable player data record. Authoritative on TV host only.
/// Phones receive read-only snapshots via ControllerGateway.
/// </summary>
[Serializable]
public class PlayerData
{
    public string playerId;          // Assigned by server on WebSocket connect
    public string nickname;          // Chosen by player on phone
    public string heroId;            // "byte" | "nova" | "orbit" | "striker" | "sizzle" | "shade" | "dusty" | "pop"
    public int playerSlot;           // 0–7 (determines color, camera marker, HUD position)
    public bool isReady;
    public bool isConnected;
    public int sessionScore;         // Cumulative score across all mini-games
    public int currentRoundScore;
    public int placement;            // 1st, 2nd, etc. in last mini-game
    public string reconnectToken;    // Used to restore session on reconnect
    public long lastPingMs;

    // Derived at runtime from CharacterDefinition asset
    [NonSerialized] public UnityEngine.Color playerColor;
    [NonSerialized] public CharacterDefinition characterDef;
}
