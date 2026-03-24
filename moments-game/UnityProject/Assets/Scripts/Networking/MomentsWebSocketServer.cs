using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Production WebSocket server for TV↔phone communication.
/// Uses .NET System.Net.WebSockets (built into .NET 4.x / Unity 6).
/// TV-authoritative: phones send intent, TV broadcasts state.
/// Runs WebSocket I/O on background threads. Events are queued to main thread.
/// </summary>
public class MomentsWebSocketServer : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int wsPort = 8765;
    [SerializeField] private float pingIntervalSeconds = 5f;

    public static MomentsWebSocketServer Instance { get; private set; }
    public string ServerAddress { get; private set; }

    // Thread-safe event queue for main thread dispatch
    private readonly Queue<(string connectionId, string message)> _incomingQueue = new();
    private readonly object _queueLock = new();

    private readonly Dictionary<string, WebSocketConnection> _connections = new();
    private readonly object _connectionsLock = new();

    private HttpListener _httpListener;
    private CancellationTokenSource _cts;
    private bool _running;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _cts = new CancellationTokenSource();
        StartServer();
    }

    private void StartServer()
    {
        ServerAddress = $"http://+:{wsPort}/join/";
        _httpListener = new HttpListener();
        _httpListener.Prefixes.Add(ServerAddress);
        _httpListener.Start();
        _running = true;

        var acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "WS-Accept" };
        acceptThread.Start();

        StartCoroutine(PingLoop());
        Debug.Log($"[WS] Server listening on ws://0.0.0.0:{wsPort}/join");
    }

    // ── Accept Loop (background thread) ──────────────────────────────────────

    private void AcceptLoop()
    {
        while (_running)
        {
            try
            {
                var ctx = _httpListener.GetContext();
                if (ctx.Request.IsWebSocketRequest)
                {
                    ThreadPool.QueueUserWorkItem(_ => HandleClientAsync(ctx));
                }
                else
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.Close();
                }
            }
            catch (Exception e)
            {
                if (_running) Debug.LogWarning($"[WS] Accept error: {e.Message}");
            }
        }
    }

    private async void HandleClientAsync(HttpListenerContext ctx)
    {
        var wsCtx = await ctx.AcceptWebSocketAsync(null);
        var ws = wsCtx.WebSocket;
        var connectionId = Guid.NewGuid().ToString("N").Substring(0, 8);

        var conn = new WebSocketConnection(connectionId, ws);
        lock (_connectionsLock) _connections[connectionId] = conn;

        Debug.Log($"[WS] Client connected: {connectionId}");

        var buffer = new byte[4096];
        try
        {
            while (ws.State == WebSocketState.Open && !_cts.IsCancellationRequested)
            {
                var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    lock (_queueLock) _incomingQueue.Enqueue((connectionId, message));
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log($"[WS] Client {connectionId} disconnected: {e.Message}");
        }
        finally
        {
            lock (_connectionsLock) _connections.Remove(connectionId);
            // Notify session manager of disconnect
            UnityMainThreadDispatcher.Enqueue(() =>
                ControllerGateway.Instance?.HandleDisconnect(connectionId));
        }
    }

    // ── Main Thread Dispatch ──────────────────────────────────────────────────

    private void Update()
    {
        lock (_queueLock)
        {
            while (_incomingQueue.Count > 0)
            {
                var (connId, msg) = _incomingQueue.Dequeue();
                ControllerGateway.Instance?.HandleIncomingMessage(connId, msg);
            }
        }
    }

    // ── Send API ──────────────────────────────────────────────────────────────

    public void SendToConnection(string connectionId, string json)
    {
        lock (_connectionsLock)
        {
            if (_connections.TryGetValue(connectionId, out var conn))
                conn.SendAsync(json, _cts.Token);
        }
    }

    public void Broadcast(string json)
    {
        lock (_connectionsLock)
        {
            foreach (var conn in _connections.Values)
                conn.SendAsync(json, _cts.Token);
        }
    }

    // ── Ping ─────────────────────────────────────────────────────────────────

    private IEnumerator PingLoop()
    {
        while (_running)
        {
            yield return new WaitForSeconds(pingIntervalSeconds);
            Broadcast("{\"type\":\"ping\"}");
        }
    }

    private void OnDestroy()
    {
        _running = false;
        _cts?.Cancel();
        _httpListener?.Stop();
    }

    // ── Inner connection class ────────────────────────────────────────────────

    private class WebSocketConnection
    {
        public string Id { get; }
        private readonly WebSocket _ws;
        private readonly SemaphoreSlim _sendLock = new(1, 1);

        public WebSocketConnection(string id, WebSocket ws)
        {
            Id = id;
            _ws = ws;
        }

        public async void SendAsync(string json, CancellationToken ct)
        {
            if (_ws.State != WebSocketState.Open) return;
            await _sendLock.WaitAsync(ct);
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[WS] Send error to {Id}: {e.Message}");
            }
            finally
            {
                _sendLock.Release();
            }
        }
    }
}

/// <summary>
/// Simple main-thread dispatcher for background thread → Unity main thread callbacks.
/// Add this to the Bootstrap scene root alongside the WebSocket server.
/// </summary>
public static class UnityMainThreadDispatcher
{
    private static readonly Queue<Action> _queue = new();
    private static readonly object _lock = new();

    public static void Enqueue(Action action)
    {
        lock (_lock) _queue.Enqueue(action);
    }

    // Call this from a MonoBehaviour Update() on a persistent scene object
    public static void Flush()
    {
        lock (_lock)
        {
            while (_queue.Count > 0)
                _queue.Dequeue()?.Invoke();
        }
    }
}
