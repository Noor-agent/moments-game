using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

/// <summary>
/// ControllerGateway — bridges MomentsWebSocketServer messages to game systems.
/// 
/// Responsibilities:
///   - Maps WebSocket clientIds → PlayerData (via session token)
///   - Parses incoming JSON input packets
///   - Routes inputs to the active mini-game
///   - Sends state snapshots, haptics, and UI commands back to phones
///
/// TV-authoritative: phones send INTENT only. No game state on client side.
/// </summary>
public class ControllerGateway : MonoBehaviour
{
    public static ControllerGateway Instance { get; private set; }

    [Header("References")]
    [SerializeField] private MomentsWebSocketServer wsServer;
    [SerializeField] private SessionStateManager sessionManager;

    // clientId → playerId mapping
    private readonly Dictionary<string, string> _clientToPlayer = new();
    private readonly Dictionary<string, string> _playerToClient = new();
    // reconnect token → playerId
    private readonly Dictionary<string, string> _reconnectTokens = new();

    // Active mini-game receives inputs
    private MiniGameBase _activeGame;

    public event Action<string, InputMessage> OnInputReceived;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (wsServer == null) wsServer = MomentsWebSocketServer.Instance;
        if (wsServer != null)
        {
            wsServer.OnMessageReceived  += OnRawMessage;
            wsServer.OnClientConnected  += OnClientConnected;
            wsServer.OnClientDisconnected += OnClientDisconnected;
        }
        else
        {
            Debug.LogError("[Gateway] MomentsWebSocketServer not found!");
        }
    }

    private void OnDestroy()
    {
        if (wsServer != null)
        {
            wsServer.OnMessageReceived  -= OnRawMessage;
            wsServer.OnClientConnected  -= OnClientConnected;
            wsServer.OnClientDisconnected -= OnClientDisconnected;
        }
    }

    // ── Message Routing ────────────────────────────────────────────────────

    private void OnClientConnected(string clientId)
    {
        Debug.Log($"[Gateway] Client connected: {clientId}");
        // send nothing here — wait for 'join' message
    }

    private void OnClientDisconnected(string clientId)
    {
        if (_clientToPlayer.TryGetValue(clientId, out var playerId))
        {
            Debug.Log($"[Gateway] Player {playerId} disconnected (client {clientId})");
            _playerToClient.Remove(playerId);
            _clientToPlayer.Remove(clientId);
            sessionManager?.OnPlayerDisconnected(playerId);

            // Broadcast to remaining clients
            var msg = new PlayerEventMsg { type = "player_disconnected", playerId = playerId };
            wsServer.Broadcast(JsonUtility.ToJson(msg));
        }
    }

    private void OnRawMessage(string clientId, string json)
    {
        try
        {
            // Peek at the type field first
            var peek = JsonUtility.FromJson<TypePeek>(json);
            if (peek == null) return;

            switch (peek.type)
            {
                case "join":
                    HandleJoin(clientId, json);
                    break;
                case "reconnect":
                    HandleReconnect(clientId, json);
                    break;
                case "input":
                    HandleInput(clientId, json);
                    break;
                case "ping":
                    wsServer.Send(clientId, "{\"type\":\"pong\"}");
                    break;
                default:
                    Debug.LogWarning($"[Gateway] Unknown message type: {peek.type}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Gateway] Message parse error from {clientId}: {ex.Message}\nJSON: {json}");
        }
    }

    // ── Join / Reconnect ───────────────────────────────────────────────────

    private void HandleJoin(string clientId, string json)
    {
        var msg = JsonUtility.FromJson<JoinMsg>(json);
        if (msg == null) return;

        // Check if session has room
        if (sessionManager == null || !sessionManager.CanJoin)
        {
            var reject = new JoinResponseMsg { type = "join_rejected", reason = "Session full or game in progress" };
            wsServer.Send(clientId, JsonUtility.ToJson(reject));
            return;
        }

        // Register player
        var player = sessionManager.RegisterPlayer(msg.nickname, msg.heroId, msg.slot);
        if (player == null)
        {
            var reject = new JoinResponseMsg { type = "join_rejected", reason = "Slot taken" };
            wsServer.Send(clientId, JsonUtility.ToJson(reject));
            return;
        }

        // Generate reconnect token
        string token = GenerateToken();
        _reconnectTokens[token] = player.playerId;
        _clientToPlayer[clientId] = player.playerId;
        _playerToClient[player.playerId] = clientId;

        var response = new JoinResponseMsg
        {
            type           = "join_accepted",   // matches phone case 'join_accepted'
            playerId       = player.playerId,
            slot           = player.slot,
            reconnectToken = token,
            playerColor    = ColorToHex(player.playerColor)
        };
        wsServer.Send(clientId, JsonUtility.ToJson(response));

        // Broadcast new player to all others
        var announcement = new PlayerEventMsg
        {
            type     = "player_joined",
            playerId = player.playerId,
            nickname = player.nickname,
            heroId   = player.heroId,
            slot     = player.slot
        };
        wsServer.Broadcast(JsonUtility.ToJson(announcement));

        Debug.Log($"[Gateway] Player joined: {player.nickname} (slot {player.slot}) ← {clientId}");
    }

    private void HandleReconnect(string clientId, string json)
    {
        var msg = JsonUtility.FromJson<ReconnectMsg>(json);
        if (msg == null) return;

        if (!_reconnectTokens.TryGetValue(msg.token, out var playerId))
        {
            wsServer.Send(clientId, "{\"type\":\"reconnect_rejected\",\"reason\":\"Invalid token\"}");
            return;
        }

        var player = sessionManager?.GetPlayer(playerId);
        if (player == null)
        {
            wsServer.Send(clientId, "{\"type\":\"reconnect_rejected\",\"reason\":\"Player not found\"}");
            return;
        }

        // Update mapping
        if (_playerToClient.TryGetValue(playerId, out var oldClient))
            _clientToPlayer.Remove(oldClient);

        _clientToPlayer[clientId] = playerId;
        _playerToClient[playerId] = clientId;

        var response = new JoinResponseMsg
        {
            type           = "reconnect_accepted",
            playerId       = player.playerId,
            slot           = player.slot,
            reconnectToken = msg.token,
            playerColor    = ColorToHex(player.playerColor)
        };
        wsServer.Send(clientId, JsonUtility.ToJson(response));
        Debug.Log($"[Gateway] Player reconnected: {player.nickname} ← {clientId}");

        // Send current game state
        SendStateSnapshot(clientId);
    }

    private void HandleInput(string clientId, string json)
    {
        if (!_clientToPlayer.TryGetValue(clientId, out var playerId)) return;

        var input = JsonUtility.FromJson<InputMessage>(json);
        if (input == null) return;
        input.playerId = playerId;

        // Route to active game
        _activeGame?.ReceiveInput(playerId, input);
        OnInputReceived?.Invoke(playerId, input);
    }

    // ── Outgoing Commands ──────────────────────────────────────────────────

    /// <summary>Send haptic feedback to a specific player's phone.</summary>
    public void SendHaptic(string playerId, string pattern)
    {
        if (!_playerToClient.TryGetValue(playerId, out var clientId)) return;
        var msg = new HapticMsg { type = "haptic", pattern = pattern };
        wsServer?.Send(clientId, JsonUtility.ToJson(msg));
    }

    /// <summary>Send haptic to all connected phones.</summary>
    public void BroadcastHaptic(string pattern)
    {
        var msg = new HapticMsg { type = "haptic", pattern = pattern };
        wsServer?.Broadcast(JsonUtility.ToJson(msg));
    }

    /// <summary>Send a UI command to a phone (e.g., show countdown, game name, score).</summary>
    public void SendUICommand(string playerId, string command, string payload = "")
    {
        if (!_playerToClient.TryGetValue(playerId, out var clientId)) return;
        var msg = new UICommandMsg { type = "ui_command", command = command, payload = payload };
        wsServer?.Send(clientId, JsonUtility.ToJson(msg));
    }

    /// <summary>Broadcast current game state snapshot to all phones.</summary>
    public void BroadcastStateSnapshot()
    {
        if (sessionManager == null) return;
        var snapshot = BuildStateSnapshot();
        wsServer?.Broadcast(snapshot);
    }

    private void SendStateSnapshot(string clientId)
    {
        var snapshot = BuildStateSnapshot();
        wsServer?.Send(clientId, snapshot);
    }

    private string BuildStateSnapshot()
    {
        var snap = new StateSnapshotMsg
        {
            type          = "state_snapshot",
            sessionPhase  = sessionManager?.CurrentPhase.ToString() ?? "Unknown",
            activeMiniGame = sessionManager?.ActiveMiniGameId ?? "",
            playerCount   = sessionManager?.PlayerCount ?? 0,
            roundNumber   = sessionManager?.RoundNumber ?? 0
        };
        return JsonUtility.ToJson(snap);
    }

    // ── Mini-game Registration ─────────────────────────────────────────────

    public void SetActiveGame(MiniGameBase game) => _activeGame = game;
    public void ClearActiveGame() => _activeGame = null;

    // ── Aliases & Additional Sends ─────────────────────────────────────────

    /// <summary>Alias for SendHaptic — used by PodiumSceneController.</summary>
    public void SendHapticToPlayer(string playerId, string pattern)
        => SendHaptic(playerId, pattern);

    /// <summary>Send raw JSON string to a specific player's phone.</summary>
    public void SendToPlayer(string playerId, string json)
    {
        if (!_playerToClient.TryGetValue(playerId, out var clientId)) return;
        wsServer?.Send(clientId, json);
    }

    /// <summary>Broadcast a UI command to all connected phones.</summary>
    public void BroadcastUICommand(string command, string payload)
    {
        var msg = new UICommandMsg { type = "ui_command", command = command, payload = payload };
        wsServer?.Broadcast(JsonUtility.ToJson(msg));
    }

    // ── Utilities ──────────────────────────────────────────────────────────

    private string GenerateToken()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var token = new System.Text.StringBuilder(6);
        var rng = new System.Random();
        for (int i = 0; i < 6; i++) token.Append(chars[rng.Next(chars.Length)]);
        return token.ToString();
    }

    private string ColorToHex(Color c)
        => $"#{(int)(c.r * 255):X2}{(int)(c.g * 255):X2}{(int)(c.b * 255):X2}";

    // ── Message Types (Unity JsonUtility-serializable) ─────────────────────

    [Serializable] private class TypePeek          { public string type; }
    [Serializable] private class JoinMsg           { public string type; public string nickname; public string heroId; public int slot; }
    [Serializable] private class ReconnectMsg      { public string type; public string token; }
    [Serializable] private class ServerHelloMsg    { public string type; public string version; public string roomToken; }
    [Serializable] private class JoinResponseMsg   { public string type; public string playerId; public int slot; public string reconnectToken; public string playerColor; public string reason; }
    [Serializable] private class PlayerEventMsg    { public string type; public string playerId; public string nickname; public string heroId; public int slot; }
    [Serializable] private class HapticMsg         { public string type; public string pattern; }
    [Serializable] private class UICommandMsg      { public string type; public string command; public string payload; }
    [Serializable] private class StateSnapshotMsg  { public string type; public string sessionPhase; public string activeMiniGame; public int playerCount; public int roundNumber; }
}
