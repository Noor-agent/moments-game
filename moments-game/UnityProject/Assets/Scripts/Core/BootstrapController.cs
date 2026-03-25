using UnityEngine;

/// <summary>
/// Bootstrap scene root controller.
/// This is the ONLY scene that is always loaded (persistent).
/// It initializes all singleton systems and then loads the Attract scene.
///
/// Bootstrap scene hierarchy:
///   [Bootstrap Root]
///     ├── SessionStateManager
///     ├── MomentsWebSocketServer
///     ├── ControllerGateway
///     ├── PhoneControllerServer
///     ├── MiniGameLoader
///     ├── ResultsAggregator
///     └── MainThreadFlusher
/// </summary>
public class BootstrapController : MonoBehaviour
{
    [Header("System Prefabs (drag in Inspector)")]
    [SerializeField] private GameObject sessionStateManagerPrefab;
    [SerializeField] private GameObject webSocketServerPrefab;
    [SerializeField] private GameObject controllerGatewayPrefab;
    [SerializeField] private GameObject phoneServerPrefab;
    [SerializeField] private GameObject miniGameLoaderPrefab;
    [SerializeField] private GameObject resultsAggregatorPrefab;

    [Header("Config")]
    [SerializeField] private bool autoStartSystems = true;
    [SerializeField] private bool logStartupInfo = true;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        if (autoStartSystems)
            InitializeSystems();
    }

    private void InitializeSystems()
    {
        // Spawn in dependency order
        EnsureSystem<SessionStateManager>(sessionStateManagerPrefab, "SessionStateManager");
        EnsureSystem<MomentsWebSocketServer>(webSocketServerPrefab, "WebSocketServer");
        EnsureSystem<ControllerGateway>(controllerGatewayPrefab, "ControllerGateway");
        EnsureSystem<PhoneControllerServer>(phoneServerPrefab, "PhoneServer");
        EnsureSystem<MiniGameLoader>(miniGameLoaderPrefab, "MiniGameLoader");
        EnsureSystem<ResultsAggregator>(resultsAggregatorPrefab, "ResultsAggregator");

        // Art / audio / VFX singletons — created bare if no prefab assigned
        EnsureSystem<VFXManager>(null, "VFXManager");
        EnsureSystem<AudioManager>(null, "AudioManager");
        EnsureSystem<PostProcessingManager>(null, "PostProcessingManager");
        EnsureSystem<MainThreadFlusher>(null, "MainThreadFlusher");

        if (logStartupInfo) LogStartupInfo();
    }

    private T EnsureSystem<T>(GameObject prefab, string name) where T : Component
    {
        // Check if already exists (scene might have them placed directly)
        var existing = FindObjectOfType<T>();
        if (existing != null) return existing;

        if (prefab != null)
        {
            var go = Instantiate(prefab);
            go.name = name;
            DontDestroyOnLoad(go);
            return go.GetComponent<T>();
        }

        // Create bare GameObject with component if no prefab assigned
        var bareGo = new GameObject(name);
        DontDestroyOnLoad(bareGo);
        return bareGo.AddComponent<T>();
    }

    private void LogStartupInfo()
    {
        var session = SessionStateManager.Instance;
        var wsServer = MomentsWebSocketServer.Instance;
        var phoneServer = FindObjectOfType<PhoneControllerServer>();

        Debug.Log("╔══════════════════════════════════════╗");
        Debug.Log("║         MOMENTS — TV HOST             ║");
        Debug.Log("╠══════════════════════════════════════╣");
        Debug.Log($"║  Room Token: {session?.RoomToken ?? "N/A"}                  ║");
        Debug.Log($"║  WebSocket:  ws://[local-ip]:8765     ║");
        Debug.Log($"║  Phone URL:  http://[local-ip]:8080   ║");
        Debug.Log("║  Players:    0/8 connected            ║");
        Debug.Log("╚══════════════════════════════════════╝");
    }
}

/// <summary>
/// Flushes the UnityMainThreadDispatcher queue each frame.
// MainThreadFlusher is defined in MainThreadFlusher.cs

