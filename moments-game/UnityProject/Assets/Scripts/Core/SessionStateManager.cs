using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// TV-authoritative session state manager.
/// Lives in the persistent bootstrap scene and survives scene loads.
/// Single source of truth for all player and game session state.
/// </summary>
public class SessionStateManager : MonoBehaviour
{
    public static SessionStateManager Instance { get; private set; }

    [Header("Room Config")]
    public int maxPlayers = 8;
    public float reconnectGraceSeconds = 15f;

    // ── Public State ───────────────────────────────────────────────────────
    public string CurrentRoomToken { get; private set; }
    public SessionPhase CurrentPhase { get; private set; }
    public string ActiveMiniGameId { get; private set; }
    public int RoundNumber { get; private set; }
    public int PlayerCount => _players.Count;
    public bool CanJoin => CurrentPhase == SessionPhase.Lobby && _players.Count < maxPlayers;
    public List<PlayerData> Players => _players;

    // Backwards compat alias
    public string RoomToken => CurrentRoomToken;

    // ── Player colors: 8 distinct vibrant colors ───────────────────────────
    private static readonly Color[] PlayerColors = {
        new(0f,   0.749f, 1f),      // P0: Byte blue
        new(1f,   0.843f, 0f),      // P1: Nova gold
        new(1f,   0.078f, 0.576f),  // P2: Pop pink
        new(0.498f, 1f, 0f),        // P3: Striker lime
        new(1f,   0.42f, 0f),       // P4: Sizzle orange
        new(0f,   1f,    1f),       // P5: Orbit cyan
        new(0.608f, 0.349f, 0.714f),// P6: Shade purple
        new(0.824f, 0.412f, 0.118f) // P7: Dusty brown
    };

    private readonly List<PlayerData> _players = new();

    public enum SessionPhase
    {
        Attract,
        Lobby,
        CharacterSelect,
        Countdown,
        InGame,
        Results,
        Podium
    }

    // ── Events ─────────────────────────────────────────────────────────────
    public event Action<SessionPhase> OnStateChanged;
    public event Action<PlayerData>   OnPlayerJoined;
    public event Action<PlayerData>   OnPlayerReady;
    public event Action<string>       OnPlayerDisconnectedById;

    // Legacy event signature compatibility
    public event Action<PlayerData>   OnPlayerDisconnected;

    // ── Lifecycle ──────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        CurrentRoomToken = GenerateRoomToken();
        ChangePhase(SessionPhase.Attract);
    }

    // ── Player Management ──────────────────────────────────────────────────

    /// <summary>Register a new player from a phone join request.</summary>
    public PlayerData RegisterPlayer(string nickname, string heroId, int requestedSlot)
    {
        if (!CanJoin) return null;

        int slot = GetFreeSlot(requestedSlot);
        if (slot < 0) return null;

        // Check hero isn't taken
        bool heroTaken = _players.Exists(p => p.heroId == heroId && p.isConnected);
        if (heroTaken) heroId = GetAvailableHero();

        var player = new PlayerData
        {
            playerId      = Guid.NewGuid().ToString("N")[..8],
            nickname      = SanitizeNickname(nickname),
            heroId        = heroId,
            slot          = slot,
            playerSlot    = slot,
            playerColor   = PlayerColors[slot % PlayerColors.Length],
            isConnected   = true,
            isReady       = false,
            reconnectToken = Guid.NewGuid().ToString("N")[..12],
            totalScore    = 0
        };

        _players.Add(player);
        Debug.Log($"[Session] Player registered: {player.nickname} (slot={slot}, hero={heroId})");
        OnPlayerJoined?.Invoke(player);

        // Auto-advance Attract → Lobby on first join
        if (CurrentPhase == SessionPhase.Attract)
            ChangePhase(SessionPhase.Lobby);

        return player;
    }

    public void SetPlayerHero(string playerId, string heroId)
    {
        var p = GetPlayer(playerId);
        if (p == null) return;
        bool taken = _players.Exists(x => x.playerId != playerId && x.heroId == heroId && x.isConnected);
        if (!taken) p.heroId = heroId;
    }

    public void SetPlayerReady(string playerId, bool ready)
    {
        var p = GetPlayer(playerId);
        if (p == null) return;
        p.isReady = ready;
        OnPlayerReady?.Invoke(p);
        if (CurrentPhase == SessionPhase.CharacterSelect && AllPlayersReady())
            ChangePhase(SessionPhase.Countdown);
    }

    public void OnPlayerDisconnected(string playerId)
    {
        var p = GetPlayer(playerId);
        if (p == null) return;
        p.isConnected = false;
        p.isReady = false;
        OnPlayerDisconnectedById?.Invoke(playerId);
        OnPlayerDisconnected?.Invoke(p);
        Debug.Log($"[Session] Player disconnected: {p.nickname}");
    }

    public void OnPlayerReconnected(string playerId)
    {
        var p = GetPlayer(playerId);
        if (p != null) p.isConnected = true;
    }

    // ── Score Management ───────────────────────────────────────────────────

    public void AddScore(string playerId, int points)
    {
        var p = GetPlayer(playerId);
        if (p != null) p.totalScore += points;
    }

    public List<PlayerData> GetRankedPlayers()
    {
        var ranked = new List<PlayerData>(_players);
        ranked.Sort((a, b) => b.totalScore.CompareTo(a.totalScore));
        return ranked;
    }

    // ── Phase Management ───────────────────────────────────────────────────

    public void ChangePhase(SessionPhase phase)
    {
        CurrentPhase = phase;
        OnStateChanged?.Invoke(phase);
        Debug.Log($"[Session] Phase → {phase}");
    }

    // Legacy compatibility
    public void ChangeState(SessionPhase newState) => ChangePhase(newState);

    public void StartMiniGame(string gameId)
    {
        ActiveMiniGameId = gameId;
        RoundNumber++;
        ChangePhase(SessionPhase.InGame);
    }

    public void EndMiniGame()
    {
        ActiveMiniGameId = "";
        ChangePhase(SessionPhase.Results);
    }

    // ── Queries ────────────────────────────────────────────────────────────

    public PlayerData GetPlayer(string playerId)
        => _players.Find(p => p.playerId == playerId);

    public PlayerData GetPlayerByReconnectToken(string token)
        => _players.Find(p => p.reconnectToken == token);

    public bool AllPlayersReady()
        => _players.TrueForAll(p => !p.isConnected || p.isReady);

    public List<PlayerData> GetConnectedPlayers()
        => _players.FindAll(p => p.isConnected);

    // ── Utilities ──────────────────────────────────────────────────────────

    private int GetFreeSlot(int preferred)
    {
        var used = new HashSet<int>(_players.ConvertAll(p => p.slot));
        if (preferred >= 0 && preferred < maxPlayers && !used.Contains(preferred))
            return preferred;
        for (int i = 0; i < maxPlayers; i++)
            if (!used.Contains(i)) return i;
        return -1;
    }

    private string GetAvailableHero()
    {
        var allHeroes = new[] { "byte", "nova", "orbit", "striker", "sizzle", "shade", "dusty", "pop" };
        var taken = new HashSet<string>(_players.ConvertAll(p => p.heroId));
        foreach (var h in allHeroes)
            if (!taken.Contains(h)) return h;
        return "byte";
    }

    private string SanitizeNickname(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "Player";
        raw = raw.Trim();
        if (raw.Length > 12) raw = raw[..12];
        return raw;
    }

    public string GenerateToken() => GenerateRoomToken();

    private string GenerateRoomToken()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new System.Random();
        var sb = new System.Text.StringBuilder(6);
        for (int i = 0; i < 6; i++) sb.Append(chars[rng.Next(chars.Length)]);
        return sb.ToString();
    }
}
