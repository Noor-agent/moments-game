using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads and unloads mini-game scenes via Addressables.
/// Lives in the Bootstrap scene (persistent). Handles the scene lifecycle:
///   Lobby → [pre-warm mini-game] → Results → next mini-game → Podium
/// </summary>
public class MiniGameLoader : MonoBehaviour
{
    public static MiniGameLoader Instance { get; private set; }

    [Header("Scene Addresses")]
    [SerializeField] private string attractSceneAddress = "Scenes/Attract";
    [SerializeField] private string lobbySceneAddress = "Scenes/Lobby";
    [SerializeField] private string resultsSceneAddress = "Scenes/Results";
    [SerializeField] private string podiumSceneAddress = "Scenes/Podium";

    private SceneInstance _currentMiniGameScene;
    private SceneInstance _currentShellScene;
    private bool _miniGameLoaded;
    private AsyncOperationHandle<SceneInstance> _preWarmHandle;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Boot into attract screen
        _ = LoadShellScene(attractSceneAddress);
    }

    // ── Shell Scenes (Attract / Lobby / Results / Podium) ────────────────────

    public async Awaitable LoadShellScene(string address)
    {
        if (_currentShellScene.Scene.IsValid())
            await Addressables.UnloadSceneAsync(_currentShellScene).Task;

        var handle = Addressables.LoadSceneAsync(address, LoadSceneMode.Additive);
        _currentShellScene = await handle.Task;

        Debug.Log($"[Loader] Loaded shell scene: {address}");
    }

    public Awaitable LoadLobby() => LoadShellScene(lobbySceneAddress);
    public Awaitable LoadResults() => LoadShellScene(resultsSceneAddress);
    public Awaitable LoadPodium() => LoadShellScene(podiumSceneAddress);

    // ── Mini-Game Loading ─────────────────────────────────────────────────────

    /// <summary>
    /// Pre-warm the mini-game scene during the lobby countdown so it's instant on game start.
    /// Call this when countdown begins (e.g., 3 seconds before game starts).
    /// </summary>
    public void PreWarmMiniGame(MiniGameDefinition def)
    {
        if (string.IsNullOrEmpty(def.sceneAddress)) return;
        Debug.Log($"[Loader] Pre-warming: {def.sceneAddress}");
        _preWarmHandle = Addressables.LoadSceneAsync(def.sceneAddress, LoadSceneMode.Additive, activateOnLoad: false);
    }

    /// <summary>
    /// Activate a pre-warmed mini-game scene (instant) or load fresh (slight delay).
    /// </summary>
    public IEnumerator ActivateMiniGame(MiniGameDefinition def, System.Collections.Generic.List<PlayerData> players)
    {
        // Unload previous mini-game if any
        if (_miniGameLoaded)
        {
            yield return Addressables.UnloadSceneAsync(_currentMiniGameScene);
            _miniGameLoaded = false;
        }

        // Unload lobby shell
        if (_currentShellScene.Scene.IsValid())
        {
            yield return Addressables.UnloadSceneAsync(_currentShellScene);
        }

        // Activate pre-warmed scene or load fresh
        if (_preWarmHandle.IsValid() && _preWarmHandle.IsDone)
        {
            yield return _preWarmHandle.Result.ActivateAsync();
            _currentMiniGameScene = _preWarmHandle.Result;
        }
        else
        {
            var handle = Addressables.LoadSceneAsync(def.sceneAddress, LoadSceneMode.Additive);
            yield return handle;
            _currentMiniGameScene = handle.Result;
        }

        _miniGameLoaded = true;
        Debug.Log($"[Loader] Mini-game active: {def.displayName}");

        // Find the MiniGameBase in the loaded scene and initialize
        var game = FindObjectOfType<MiniGameBase>();
        if (game != null)
        {
            game.Setup(players);
            yield return new WaitForSeconds(0.5f); // Brief settle
            game.StartGame();
        }
        else
        {
            Debug.LogError($"[Loader] No MiniGameBase found in scene: {def.sceneAddress}");
        }
    }

    public IEnumerator UnloadCurrentMiniGame()
    {
        if (!_miniGameLoaded) yield break;
        yield return Addressables.UnloadSceneAsync(_currentMiniGameScene);
        _miniGameLoaded = false;
    }
}
