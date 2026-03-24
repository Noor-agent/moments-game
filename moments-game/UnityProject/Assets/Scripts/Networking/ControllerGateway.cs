using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// WebSocket gateway between TV host and phone controllers.
/// Normalizes all incoming input events and dispatches them to the active mini-game.
/// Data-driven: controller layout is defined per MiniGameDefinition ScriptableObject.
/// </summary>
public class ControllerGateway : MonoBehaviour
{
    public static ControllerGateway Instance { get; private set; }

    [Header("Server Config")]
    [SerializeField] private int wsPort = 8765;
    [SerializeField] private float pingIntervalSeconds = 2f;

    private WebSocketServer _server;
    private readonly Dictionary<string, string> _connectionToPlayer = new();
    public event System.Action<string, InputMessage> OnInputReceived;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartServer();
    }

    private void StartServer()
    {
        // WebSocketSharp server — swap for Unity Netcode WebSocket transport if preferred
        Debug.Log($"[Gateway] WebSocket server listening on ws://0.0.0.0:{wsPort}");
        // _server = new WebSocketServer($"ws://0.0.0.0:{wsPort}");
        // _server.AddWebSocketService<MomentsWebSocketBehavior>("/join", () => new MomentsWebSocketBehavior(this));
        // _server.Start();
    }

    /// <summary>
    /// Called by WebSocket behavior when a message arrives from a phone client.
    /// </summary>
    public void HandleIncomingMessage(string connectionId, string rawJson)
    {
        var msg = JsonUtility.FromJson<InputMessage>(rawJson);
        if (msg == null) return;

        var player = ResolvePlayer(connectionId, msg);
        if (player == null) return;

        msg.playerId = player.playerId;
        OnInputReceived?.Invoke(player.playerId, msg);

        // Route to session state for non-gameplay events
        switch (msg.type)
        {
            case "join":
                HandleJoin(connectionId, msg);
                break;
            case "heroHover":
                SessionStateManager.Instance.SetPlayerHero(player.playerId, msg.heroId);
                BroadcastState();
                break;
            case "heroLock":
                SessionStateManager.Instance.SetPlayerHero(player.playerId, msg.heroId);
                BroadcastState();
                break;
            case "ready":
                SessionStateManager.Instance.SetPlayerReady(player.playerId, true);
                BroadcastState();
                break;
            case "reconnect":
                HandleReconnect(connectionId, msg);
                break;
        }
    }

    private void HandleJoin(string connectionId, InputMessage msg)
    {
        var player = SessionStateManager.Instance.AddPlayer(connectionId, msg.nickname);
        if (player == null) { SendToConnection(connectionId, "{\"type\":\"error\",\"code\":\"room_full\"}"); return; }
        _connectionToPlayer[connectionId] = player.playerId;
        BroadcastState();
        SendHaptic(player.playerId, "join");
    }

    private void HandleReconnect(string connectionId, InputMessage msg)
    {
        var player = SessionStateManager.Instance.GetPlayerByReconnectToken(msg.reconnectToken);
        if (player == null) { HandleJoin(connectionId, msg); return; }
        player.isConnected = true;
        _connectionToPlayer[connectionId] = player.playerId;
        BroadcastState();
    }

    private PlayerData ResolvePlayer(string connectionId, InputMessage msg)
    {
        if (_connectionToPlayer.TryGetValue(connectionId, out var pid))
            return SessionStateManager.Instance.GetPlayer(pid);
        return null;
    }

    public void BroadcastState()
    {
        // Serialize session state snapshot and broadcast to all connected phones
        var snapshot = BuildStateSnapshot();
        // _server.WebSocketServices.Broadcast(snapshot);
        Debug.Log($"[Gateway] Broadcast state: {snapshot}");
    }

    public void SendHaptic(string playerId, string pattern)
    {
        // Send haptic command to specific player's phone
        var msg = $"{{\"type\":\"haptic\",\"pattern\":\"{pattern}\"}}";
        SendToPlayer(playerId, msg);
    }

    public void SendControllerLayout(string playerId, string layoutId)
    {
        var msg = $"{{\"type\":\"layout\",\"layoutId\":\"{layoutId}\"}}";
        SendToPlayer(playerId, msg);
    }

    private void SendToPlayer(string playerId, string json)
    {
        foreach (var kv in _connectionToPlayer)
            if (kv.Value == playerId) { SendToConnection(kv.Key, json); break; }
    }

    private void SendToConnection(string connectionId, string json)
    {
        // _server.WebSocketServices["/join"].Sessions.SendTo(json, connectionId);
        Debug.Log($"[Gateway] → {connectionId}: {json}");
    }

    private string BuildStateSnapshot()
    {
        // Build minimal JSON state for all phone clients
        return JsonUtility.ToJson(new
        {
            type = "stateUpdate",
            lobbyState = SessionStateManager.Instance.CurrentState.ToString(),
            players = SessionStateManager.Instance.Players
        });
    }

    private void OnDestroy()
    {
        // _server?.Stop();
    }
}

[System.Serializable]
public class InputMessage
{
    public string type;         // "join" | "heroHover" | "heroLock" | "ready" | "input" | "reconnect"
    public string playerId;     // Filled in by gateway after auth
    public string nickname;
    public string heroId;
    public string reconnectToken;
    
    // Gameplay input fields (used during active mini-game)
    public float moveX;         // Left joystick X
    public float moveY;         // Left joystick Y
    public float aimX;          // Right joystick / aim pad X
    public float aimY;          // Right joystick / aim pad Y
    public bool dashPressed;
    public bool firePressed;
    public bool actionPressed;
    public bool duckPressed;
    public float tiltX;         // Device tilt for Wave Rider
}
