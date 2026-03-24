using UnityEngine;

/// <summary>
/// Attaches to the Bootstrap root and calls UnityMainThreadDispatcher.Flush() each frame.
/// Required for any background thread (WebSocket receive, HTTP listener) to dispatch
/// Unity API calls safely onto the main thread.
///
/// Place this on the same GameObject as MomentsWebSocketServer and ControllerGateway
/// in the Bootstrap scene so it persists for the full session.
/// </summary>
public class MainThreadFlusher : MonoBehaviour
{
    private void Update()
    {
        UnityMainThreadDispatcher.Flush();
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Moments/Debug/Main Thread Queue Size")]
    private static void LogQueueSize()
    {
        UnityEngine.Debug.Log($"[MainThread] Pending actions: {UnityMainThreadDispatcher.PendingCount}");
    }
#endif
}
