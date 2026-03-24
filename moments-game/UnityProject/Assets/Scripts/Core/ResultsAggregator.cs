using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Results aggregator — collects round scores, computes placements,
/// updates cumulative session scores, and drives the Results and Podium scenes.
/// Lives in the Bootstrap (persistent) scene.
/// </summary>
public class ResultsAggregator : MonoBehaviour
{
    public static ResultsAggregator Instance { get; private set; }

    [System.Serializable]
    public class RoundResult
    {
        public string gameId;
        public string gameName;
        public Dictionary<string, int> roundScores;
        public Dictionary<string, int> placements; // 1 = 1st place
    }

    public List<RoundResult> RoundHistory { get; } = new();
    public int TotalRoundsPlayed => RoundHistory.Count;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Call at the end of each mini-game round.
    /// Updates SessionStateManager player session scores and computes placements.
    /// </summary>
    public RoundResult RecordRound(string gameId, string gameName, Dictionary<string, int> roundScores)
    {
        var placements = ComputePlacements(roundScores);
        var result = new RoundResult
        {
            gameId = gameId,
            gameName = gameName,
            roundScores = roundScores,
            placements = placements
        };
        RoundHistory.Add(result);

        // Add round scores to cumulative session score
        var players = SessionStateManager.Instance?.Players;
        if (players != null)
        {
            foreach (var player in players)
            {
                if (roundScores.TryGetValue(player.playerId, out var pts))
                {
                    player.sessionScore += pts;
                    player.currentRoundScore = pts;
                }

                if (placements.TryGetValue(player.playerId, out var place))
                    player.placement = place;
            }
        }

        Debug.Log($"[Results] Round {TotalRoundsPlayed}: {gameName} complete.");
        LogPlacements(result);

        return result;
    }

    private Dictionary<string, int> ComputePlacements(Dictionary<string, int> scores)
    {
        var sorted = new List<KeyValuePair<string, int>>(scores);
        sorted.Sort((a, b) => b.Value.CompareTo(a.Value)); // Descending

        var placements = new Dictionary<string, int>();
        for (int i = 0; i < sorted.Count; i++)
        {
            // Handle ties: same score = same placement
            if (i > 0 && sorted[i].Value == sorted[i - 1].Value)
                placements[sorted[i].Key] = placements[sorted[i - 1].Key];
            else
                placements[sorted[i].Key] = i + 1;
        }
        return placements;
    }

    /// <summary>
    /// Get final session standings ordered by cumulative score.
    /// </summary>
    public List<PlayerData> GetFinalStandings()
    {
        var players = new List<PlayerData>(SessionStateManager.Instance?.Players ?? new());
        players.Sort((a, b) => b.sessionScore.CompareTo(a.sessionScore));

        // Assign final placement
        for (int i = 0; i < players.Count; i++)
            players[i].placement = i + 1;

        return players;
    }

    public void Reset()
    {
        RoundHistory.Clear();
        var players = SessionStateManager.Instance?.Players;
        if (players != null)
            foreach (var p in players)
            {
                p.sessionScore = 0;
                p.currentRoundScore = 0;
                p.placement = 0;
            }
    }

    private void LogPlacements(RoundResult result)
    {
        foreach (var kv in result.placements)
            Debug.Log($"  #{kv.Value} → {kv.Key}: {result.roundScores.GetValueOrDefault(kv.Key)} pts");
    }
}
