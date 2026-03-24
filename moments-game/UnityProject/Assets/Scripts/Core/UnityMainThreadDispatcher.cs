using System;
using System.Collections.Concurrent;
using UnityEngine;

/// <summary>
/// Static utility for dispatching actions onto Unity's main thread
/// from any background thread.
///
/// Usage (background thread):
///   UnityMainThreadDispatcher.Enqueue(() => Debug.Log("on main thread!"));
///
/// Flush is called every frame by MainThreadFlusher (on Bootstrap root).
/// </summary>
public static class UnityMainThreadDispatcher
{
    private static readonly ConcurrentQueue<Action> _queue = new();

    /// <summary>Enqueue an action to run on the main thread next frame.</summary>
    public static void Enqueue(Action action)
    {
        if (action == null) return;
        _queue.Enqueue(action);
    }

    /// <summary>
    /// Called every frame by MainThreadFlusher. Drains and executes all queued actions.
    /// </summary>
    public static void Flush()
    {
        // Cap iterations to avoid infinite loop if actions enqueue more actions
        int max = _queue.Count;
        for (int i = 0; i < max; i++)
        {
            if (!_queue.TryDequeue(out var action)) break;
            try { action(); }
            catch (Exception ex)
            {
                Debug.LogError($"[MainThread] Unhandled exception in dispatched action: {ex}");
            }
        }
    }

    public static int PendingCount => _queue.Count;
}
