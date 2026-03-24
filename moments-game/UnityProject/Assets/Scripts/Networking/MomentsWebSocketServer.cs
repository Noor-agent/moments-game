using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

/// <summary>
/// Production WebSocket server for Moments — pure .NET, no external DLL required.
/// Implements RFC 6455 WebSocket protocol directly using TcpListener.
/// Handles: player join, input packets, reconnect, ping/pong.
/// 
/// TV acts as authoritative server. Phones connect here, send intent only.
/// Thread-safe: all Unity API calls dispatched via MainThreadQueue.
/// </summary>
public class MomentsWebSocketServer : MonoBehaviour
{
    public static MomentsWebSocketServer Instance { get; private set; }

    [Header("Server Config")]
    [SerializeField] private int port = 8765;
    [SerializeField] private float pingInterval = 5f;
    [SerializeField] private float clientTimeout = 30f;

    // Thread-safe queues
    private readonly ConcurrentQueue<Action> _mainThreadQueue = new();
    private readonly ConcurrentDictionary<string, WsClient> _clients = new();

    // Events dispatched on main thread
    public event Action<string, string> OnMessageReceived;   // (clientId, jsonPayload)
    public event Action<string> OnClientConnected;           // (clientId)
    public event Action<string> OnClientDisconnected;        // (clientId)

    private TcpListener _listener;
    private Thread _listenThread;
    private bool _running;
    private float _pingTimer;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start() => StartServer();

    private void Update()
    {
        // Drain main thread queue
        while (_mainThreadQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { Debug.LogError($"[WSS] Main thread action error: {ex.Message}"); }
        }

        // Ping all clients periodically
        _pingTimer += Time.deltaTime;
        if (_pingTimer >= pingInterval)
        {
            _pingTimer = 0f;
            PingAll();
        }

        // Timeout check
        var now = DateTime.UtcNow;
        foreach (var kv in _clients)
        {
            if ((now - kv.Value.LastPong).TotalSeconds > clientTimeout)
            {
                Debug.Log($"[WSS] Client {kv.Key} timed out");
                DisconnectClient(kv.Key);
            }
        }
    }

    private void OnDestroy() => StopServer();

    // ── Server Control ─────────────────────────────────────────────────────

    public void StartServer()
    {
        if (_running) return;
        _running = true;
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        _listenThread = new Thread(ListenLoop) { IsBackground = true, Name = "WSS-Accept" };
        _listenThread.Start();
        Debug.Log($"[WSS] WebSocket server started on ws://0.0.0.0:{port}");
    }

    public void StopServer()
    {
        _running = false;
        try { _listener?.Stop(); } catch { }
        foreach (var kv in _clients)
        {
            try { kv.Value.TcpClient?.Close(); } catch { }
        }
        _clients.Clear();
        Debug.Log("[WSS] Server stopped");
    }

    // ── Accept Loop (background thread) ────────────────────────────────────

    private void ListenLoop()
    {
        while (_running)
        {
            try
            {
                var tcp = _listener.AcceptTcpClient();
                var thread = new Thread(() => HandleClient(tcp)) { IsBackground = true };
                thread.Start();
            }
            catch (SocketException) when (!_running) { break; }
            catch (Exception ex)
            {
                if (_running) Debug.LogError($"[WSS] Accept error: {ex.Message}");
            }
        }
    }

    // ── Per-client handler (background thread) ─────────────────────────────

    private void HandleClient(TcpClient tcp)
    {
        string clientId = Guid.NewGuid().ToString("N")[..8];
        var stream = tcp.GetStream();

        try
        {
            // WebSocket handshake
            if (!PerformHandshake(stream, out string path))
            {
                tcp.Close();
                return;
            }

            var client = new WsClient
            {
                ClientId   = clientId,
                TcpClient  = tcp,
                Stream     = stream,
                ConnectedAt = DateTime.UtcNow,
                LastPong   = DateTime.UtcNow,
                Path       = path
            };
            _clients[clientId] = client;

            _mainThreadQueue.Enqueue(() =>
            {
                Debug.Log($"[WSS] Client connected: {clientId} path={path}");
                OnClientConnected?.Invoke(clientId);
            });

            // Read loop
            while (_running && tcp.Connected)
            {
                string msg = ReadFrame(stream);
                if (msg == null) break;

                if (msg == "__pong__")
                {
                    client.LastPong = DateTime.UtcNow;
                    continue;
                }

                // Dispatch to main thread
                string captured = msg;
                string cid = clientId;
                _mainThreadQueue.Enqueue(() => OnMessageReceived?.Invoke(cid, captured));
            }
        }
        catch (Exception ex)
        {
            if (_running) Debug.Log($"[WSS] Client {clientId} disconnected: {ex.Message}");
        }
        finally
        {
            try { tcp.Close(); } catch { }
            _clients.TryRemove(clientId, out _);
            _mainThreadQueue.Enqueue(() =>
            {
                Debug.Log($"[WSS] Client disconnected: {clientId}");
                OnClientDisconnected?.Invoke(clientId);
            });
        }
    }

    // ── RFC 6455 Handshake ─────────────────────────────────────────────────

    private bool PerformHandshake(NetworkStream stream, out string path)
    {
        path = "/";
        var buf = new byte[4096];
        int n = stream.Read(buf, 0, buf.Length);
        var request = Encoding.UTF8.GetString(buf, 0, n);

        // Parse path
        var lines = request.Split('\n');
        if (lines.Length > 0)
        {
            var parts = lines[0].Trim().Split(' ');
            if (parts.Length > 1) path = parts[1];
        }

        // Extract Sec-WebSocket-Key
        string key = null;
        foreach (var line in lines)
        {
            if (line.StartsWith("Sec-WebSocket-Key:", StringComparison.OrdinalIgnoreCase))
            {
                key = line.Substring("Sec-WebSocket-Key:".Length).Trim();
                break;
            }
        }
        if (key == null) return false;

        // Compute accept key
        string magic = key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
        var hash = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(magic));
        string acceptKey = Convert.ToBase64String(hash);

        var response = "HTTP/1.1 101 Switching Protocols\r\n" +
                       "Upgrade: websocket\r\n" +
                       "Connection: Upgrade\r\n" +
                       $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n";
        var respBytes = Encoding.UTF8.GetBytes(response);
        stream.Write(respBytes, 0, respBytes.Length);
        return true;
    }

    // ── RFC 6455 Frame Reader ──────────────────────────────────────────────

    private string ReadFrame(NetworkStream stream)
    {
        var header = new byte[2];
        if (!ReadExact(stream, header, 2)) return null;

        bool masked    = (header[1] & 0x80) != 0;
        int  opcode    = header[0] & 0x0F;
        long payloadLen = header[1] & 0x7F;

        if (opcode == 8) return null; // Close
        if (opcode == 10) return "__pong__"; // Pong

        if (payloadLen == 126)
        {
            var ext = new byte[2];
            if (!ReadExact(stream, ext, 2)) return null;
            payloadLen = (ext[0] << 8) | ext[1];
        }
        else if (payloadLen == 127)
        {
            var ext = new byte[8];
            if (!ReadExact(stream, ext, 8)) return null;
            payloadLen = BitConverter.ToInt64(ext, 0);
        }

        byte[] mask = null;
        if (masked)
        {
            mask = new byte[4];
            if (!ReadExact(stream, mask, 4)) return null;
        }

        var payload = new byte[payloadLen];
        if (!ReadExact(stream, payload, (int)payloadLen)) return null;

        if (masked)
            for (int i = 0; i < payload.Length; i++)
                payload[i] ^= mask[i % 4];

        if (opcode == 1) return Encoding.UTF8.GetString(payload); // Text
        return null;
    }

    private bool ReadExact(NetworkStream stream, byte[] buf, int count)
    {
        int read = 0;
        while (read < count)
        {
            int n = stream.Read(buf, read, count - read);
            if (n == 0) return false;
            read += n;
        }
        return true;
    }

    // ── RFC 6455 Frame Writer ──────────────────────────────────────────────

    private void WriteFrame(NetworkStream stream, string text)
    {
        var payload = Encoding.UTF8.GetBytes(text);
        var frame = new List<byte>();

        frame.Add(0x81); // FIN + text opcode

        if (payload.Length <= 125)
            frame.Add((byte)payload.Length);
        else if (payload.Length <= 65535)
        {
            frame.Add(126);
            frame.Add((byte)(payload.Length >> 8));
            frame.Add((byte)(payload.Length & 0xFF));
        }
        else
        {
            frame.Add(127);
            var lenBytes = BitConverter.GetBytes((long)payload.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lenBytes);
            frame.AddRange(lenBytes);
        }

        frame.AddRange(payload);
        var frameArr = frame.ToArray();
        stream.Write(frameArr, 0, frameArr.Length);
    }

    // ── Send / Broadcast ───────────────────────────────────────────────────

    public void Send(string clientId, string json)
    {
        if (!_clients.TryGetValue(clientId, out var client)) return;
        try
        {
            lock (client)
            {
                WriteFrame(client.Stream, json);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[WSS] Send to {clientId} failed: {ex.Message}");
            DisconnectClient(clientId);
        }
    }

    public void Broadcast(string json)
    {
        foreach (var kv in _clients)
            Send(kv.Key, json);
    }

    // ── Utilities ──────────────────────────────────────────────────────────

    private void PingAll()
    {
        // RFC 6455 ping frame: 0x89 0x00
        foreach (var kv in _clients)
        {
            try
            {
                lock (kv.Value)
                {
                    kv.Value.Stream.Write(new byte[] { 0x89, 0x00 }, 0, 2);
                }
            }
            catch { DisconnectClient(kv.Key); }
        }
    }

    private void DisconnectClient(string clientId)
    {
        if (_clients.TryRemove(clientId, out var client))
        {
            try { client.TcpClient?.Close(); } catch { }
        }
    }

    public string GetLocalIP()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { }
        return "127.0.0.1";
    }

    public int ConnectedClientCount => _clients.Count;

    // ── Inner Types ────────────────────────────────────────────────────────

    private class WsClient
    {
        public string ClientId;
        public TcpClient TcpClient;
        public NetworkStream Stream;
        public DateTime ConnectedAt;
        public DateTime LastPong;
        public string Path;
    }
}
