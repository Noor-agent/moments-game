using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// TV-authoritative session state manager.
/// Lives in the persistent bootstrap scene and survives scene loads.
/// </summary>
public class SessionStateManager : MonoBehaviour
{
    public static SessionStateManager Instance { get; private set; }

    [Header("Room Config")]
    public int maxPlayers = 8;
    public float reconnectGraceSeconds = 15f;

    public string RoomToken { get; private set; }
    public LobbyState CurrentState { get; private set; }
    public List<PlayerData> Players { get; private set; } = new();

    public enum LobbyState
    {
        Attract,
        WaitingForPlayers,
        CharacterSelect,
        ReadyCheck,
        Countdown,
        InGame,
        Results,
        Podium,
        ReconnectGrace
    }

    public event Action<LobbyState> OnStateChanged;
    public event Action<PlayerData> OnPlayerJoined;
    public event Action<PlayerData> OnPlayerReady;
    public event Action<PlayerData> OnPlayerDisconnected;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        RoomToken = GenerateToken();
        ChangeState(LobbyState.Attract);
    }

    public string GenerateToken()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new System.Random();
        return new string(new char[] {
            chars[rng.Next(chars.Length)], chars[rng.Next(chars.Length)],
            chars[rng.Next(chars.Length)], chars[rng.Next(chars.Length)],
            chars[rng.Next(chars.Length)], chars[rng.Next(chars.Length)]
        });
    }

    public void ChangeState(LobbyState newState)
    {
        CurrentState = newState;
        OnStateChanged?.Invoke(newState);
        Debug.Log($"[Session] State → {newState}");
    }

    public PlayerData AddPlayer(string playerId, string nickname)
    {
        if (Players.Count >= maxPlayers) return null;
        var slotIndex = GetNextFreeSlot();
        if (slotIndex < 0) return null;

        var player = new PlayerData
        {
            playerId = playerId,
            nickname = nickname,
            playerSlot = slotIndex,
            isConnected = true,
            reconnectToken = Guid.NewGuid().ToString("N").Substring(0, 12)
        };

        Players.Add(player);
        OnPlayerJoined?.Invoke(player);
        return player;
    }

    public void SetPlayerHero(string playerId, string heroId)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;
        // Validate not already taken by another player
        bool taken = Players.Exists(p => p.playerId != playerId && p.heroId == heroId && p.isConnected);
        if (!taken) player.heroId = heroId;
    }

    public void SetPlayerReady(string playerId, bool ready)
    {
        var player = GetPlayer(playerId);
        if (player == null) return;
        player.isReady = ready;
        OnPlayerReady?.Invoke(player);

        // Auto-advance if all connected players ready
        if (CurrentState == LobbyState.CharacterSelect && AllPlayersReady())
            ChangeState(LobbyState.Countdown);
    }

    public bool AllPlayersReady()
        => Players.TrueForAll(p => !p.isConnected || p.isReady);

    public PlayerData GetPlayer(string playerId)
        => Players.Find(p => p.playerId == playerId);

    public PlayerData GetPlayerByReconnectToken(string token)
        => Players.Find(p => p.reconnectToken == token);

    private int GetNextFreeSlot()
    {
        var usedSlots = new HashSet<int>(Players.ConvertAll(p => p.playerSlot));
        for (int i = 0; i < maxPlayers; i++)
            if (!usedSlots.Contains(i)) return i;
        return -1;
    }
}
