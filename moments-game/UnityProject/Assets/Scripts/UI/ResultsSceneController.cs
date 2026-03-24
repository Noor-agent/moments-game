using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// TV Results scene controller.
/// Shows per-round scores, placements with medal animations, and running session totals.
/// Transitions to next mini-game or Podium after display time.
/// </summary>
public class ResultsSceneController : MonoBehaviour
{
    [Header("Results Display")]
    [SerializeField] private Transform resultsContainer;
    [SerializeField] private GameObject resultCardPrefab;
    [SerializeField] private TextMeshProUGUI gameNameText;
    [SerializeField] private TextMeshProUGUI roundNumberText;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 8f;
    [SerializeField] private float cardStaggerDelay = 0.3f;

    [Header("Medals")]
    [SerializeField] private GameObject goldMedalPrefab;
    [SerializeField] private GameObject silverMedalPrefab;
    [SerializeField] private GameObject bronzeMedalPrefab;

    [Header("Next Game")]
    [SerializeField] private TextMeshProUGUI nextGameText;
    [SerializeField] private Slider timerSlider;

    private void Start()
    {
        var aggregator = ResultsAggregator.Instance;
        var session = SessionStateManager.Instance;
        if (aggregator == null || session == null) return;

        var lastRound = aggregator.RoundHistory.Count > 0
            ? aggregator.RoundHistory[^1]
            : null;

        if (lastRound != null)
        {
            if (gameNameText != null) gameNameText.text = lastRound.gameName;
            if (roundNumberText != null) roundNumberText.text = $"Round {aggregator.TotalRoundsPlayed}";
        }

        StartCoroutine(ShowResultCards(session.Players, lastRound));
        StartCoroutine(TimerAndAdvance());
    }

    private IEnumerator ShowResultCards(List<PlayerData> players, ResultsAggregator.RoundResult round)
    {
        // Sort by placement in this round
        var sorted = new List<PlayerData>(players);
        sorted.Sort((a, b) =>
        {
            if (round != null && round.placements.TryGetValue(a.playerId, out var pA) &&
                round.placements.TryGetValue(b.playerId, out var pB))
                return pA.CompareTo(pB);
            return a.sessionScore.CompareTo(b.sessionScore);
        });

        foreach (var player in sorted)
        {
            var cardObj = Instantiate(resultCardPrefab, resultsContainer);
            var card = cardObj.GetComponent<ResultCard>();

            int roundScore = round?.roundScores.GetValueOrDefault(player.playerId) ?? 0;
            int placement = round?.placements.GetValueOrDefault(player.playerId) ?? 0;

            card?.Setup(player, roundScore, placement);
            yield return new WaitForSeconds(cardStaggerDelay);
        }
    }

    private IEnumerator TimerAndAdvance()
    {
        float elapsed = 0f;
        while (elapsed < displayDuration)
        {
            elapsed += Time.deltaTime;
            if (timerSlider != null) timerSlider.value = 1f - (elapsed / displayDuration);
            yield return null;
        }

        // Check if session is over (e.g., 5 rounds played) or advance to next game
        var aggregator = ResultsAggregator.Instance;
        bool sessionOver = aggregator != null && aggregator.TotalRoundsPlayed >= 5;

        if (sessionOver)
            await MiniGameLoader.Instance?.LoadPodium();
        else
            await MiniGameLoader.Instance?.LoadLobby(); // Back to lobby for next vote
    }
}

/// <summary>
/// Individual result card shown on the TV results screen.
/// </summary>
public class ResultCard : MonoBehaviour
{
    [SerializeField] private Image colorFrame;
    [SerializeField] private Image heroPortrait;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI roundScoreText;
    [SerializeField] private TextMeshProUGUI totalScoreText;
    [SerializeField] private TextMeshProUGUI placementText;
    [SerializeField] private Image medalImage;

    [SerializeField] private Sprite goldMedal;
    [SerializeField] private Sprite silverMedal;
    [SerializeField] private Sprite bronzeMedal;

    public void Setup(PlayerData player, int roundScore, int placement)
    {
        if (colorFrame != null) colorFrame.color = player.playerColor;
        if (nicknameText != null) nicknameText.text = player.nickname;
        if (roundScoreText != null) roundScoreText.text = $"+{roundScore}";
        if (totalScoreText != null) totalScoreText.text = player.sessionScore.ToString();

        if (placementText != null)
        {
            placementText.text = placement switch
            {
                1 => "🥇 1st",
                2 => "🥈 2nd",
                3 => "🥉 3rd",
                _ => $"#{placement}"
            };
        }

        if (medalImage != null)
        {
            medalImage.sprite = placement switch
            {
                1 => goldMedal,
                2 => silverMedal,
                3 => bronzeMedal,
                _ => null
            };
            medalImage.gameObject.SetActive(placement <= 3 && medalImage.sprite != null);
        }

        if (player.characterDef?.portraitSprite != null && heroPortrait != null)
            heroPortrait.sprite = player.characterDef.portraitSprite;
    }
}
