// WebSocketSharp stub for Unity editor compilation.
// In production: replace Assets/Plugins/WebSocketSharp/ with the actual
// websocket-sharp.dll from NuGet or the websocket-sharp GitHub release.
//
// Download: https://github.com/sta/websocket-sharp/releases
// Or via NuGet: Install-Package WebSocketSharp
// Then place websocket-sharp.dll in Assets/Plugins/WebSocketSharp/
//
// This file provides the minimal interface so all C# scripts compile
// without the DLL present. Remove this file once the real DLL is installed.

#if !WEBSOCKETSHARP_INSTALLED
namespace WebSocketSharp.Server
{
    public class WebSocketServer
    {
        public WebSocketServer(int port) { Port = port; }
        public int Port { get; }
        public bool IsListening { get; private set; }
        public void Start() { IsListening = true; UnityEngine.Debug.LogWarning("[WSS] WebSocketSharp DLL not installed. Using stub. Download from https://github.com/sta/websocket-sharp"); }
        public void Stop() { IsListening = false; }
        public void AddWebSocketService<T>(string path) where T : WebSocketBehavior, new() { }
        public WebSocketServiceManager WebSocketServices { get; } = new WebSocketServiceManager();
    }
    public class WebSocketServiceManager
    {
        public void Broadcast(string data) { }
        public void BroadcastAsync(string data, System.Action<bool> completed) { completed?.Invoke(false); }
    }
    public class WebSocketBehavior
    {
        protected WebSocketSharp.WebSocket Context_WebSocket { get; }
        protected virtual void OnOpen() { }
        protected virtual void OnClose(WebSocketSharp.CloseEventArgs e) { }
        protected virtual void OnMessage(WebSocketSharp.MessageEventArgs e) { }
        protected virtual void OnError(WebSocketSharp.ErrorEventArgs e) { }
        protected void Send(string data) { }
    }
}
namespace WebSocketSharp
{
    public class WebSocket : System.IDisposable
    {
        public WebSocket(string url) { }
        public void Connect() { }
        public void Close() { }
        public void Send(string data) { }
        public void Dispose() { }
    }
    public class CloseEventArgs : System.EventArgs
    {
        public ushort Code { get; }
        public string Reason { get; }
        public bool WasClean { get; }
    }
    public class MessageEventArgs : System.EventArgs
    {
        public string Data { get; }
        public byte[] RawData { get; }
        public bool IsText { get; }
        public bool IsBinary { get; }
    }
    public class ErrorEventArgs : System.EventArgs
    {
        public System.Exception Exception { get; }
        public string Message { get; }
    }
}
#endif
