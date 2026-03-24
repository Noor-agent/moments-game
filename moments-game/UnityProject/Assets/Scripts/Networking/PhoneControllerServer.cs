using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Lightweight HTTP server that serves phone-controller.html to devices on the local network.
/// Runs on a background thread. Unity main thread is never blocked.
/// Usage: attach to the Bootstrap scene root GameObject.
/// </summary>
public class PhoneControllerServer : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private int httpPort = 8080;
    [SerializeField] private TextAsset phoneControllerHtml; // Drag phone-controller.html here

    public string ServerUrl { get; private set; }
    public string LocalIP { get; private set; }

    private HttpListener _listener;
    private Thread _listenerThread;
    private bool _running;

    private void Start()
    {
        LocalIP = GetLocalIPAddress();
        ServerUrl = $"http://{LocalIP}:{httpPort}/";

        StartServer();
        Debug.Log($"[PhoneServer] Serving controller at {ServerUrl}");
    }

    private void StartServer()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://*:{httpPort}/");
        _listener.Start();
        _running = true;

        _listenerThread = new Thread(ListenLoop) { IsBackground = true };
        _listenerThread.Start();
    }

    private void ListenLoop()
    {
        while (_running)
        {
            try
            {
                var context = _listener.GetContext();
                HandleRequest(context);
            }
            catch (Exception e)
            {
                if (_running) Debug.LogWarning($"[PhoneServer] {e.Message}");
            }
        }
    }

    private void HandleRequest(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            string htmlContent;

            if (phoneControllerHtml != null)
            {
                htmlContent = phoneControllerHtml.text;
            }
            else
            {
                // Fallback: read from StreamingAssets
                string path = System.IO.Path.Combine(Application.streamingAssetsPath, "phone-controller.html");
                htmlContent = System.IO.File.Exists(path)
                    ? System.IO.File.ReadAllText(path)
                    : "<h1>Controller not found. Please assign phone-controller.html to PhoneControllerServer.</h1>";
            }

            // Inject the WebSocket URL so the phone auto-connects
            string wsUrl = $"ws://{LocalIP}:{8765}/join";
            htmlContent = htmlContent.Replace("%%WS_URL%%", wsUrl);
            htmlContent = htmlContent.Replace("%%ROOM_TOKEN%%", SessionStateManager.Instance?.RoomToken ?? "DEMO");

            byte[] bytes = Encoding.UTF8.GetBytes(htmlContent);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = bytes.Length;
            response.AddHeader("Cache-Control", "no-cache");
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception e)
        {
            Debug.LogError($"[PhoneServer] Request error: {e}");
            response.StatusCode = 500;
        }
        finally
        {
            response.OutputStream.Close();
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    private void OnDestroy()
    {
        _running = false;
        _listener?.Stop();
        _listenerThread?.Join(500);
    }
}
