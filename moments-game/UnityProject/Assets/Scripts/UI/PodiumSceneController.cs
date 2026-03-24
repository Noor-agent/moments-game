using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// TV Podium scene — final celebration screen.
/// Shows 1st/2nd/3rd place on podium with confetti, trophy, and rematch option.
/// </summary>
public class PodiumSceneController : MonoBehaviour
{
    [Header("Podium Slots")]
    [SerializeField] private PodiumSlot firstPlaceSlot;
    [SerializeField] private PodiumSlot secondPlaceSlot;
    [SerializeField] private PodiumSlot thirdPlaceSlot;

    [Header("Effects")]
    [SerializeField] private ParticleSystem confettiSystem;
    [SerializeField] private AudioSource fanfareAudio;
    [SerializeField] private AudioClip podiumTheme;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI sessionSummaryText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button newSessionButton;
    [SerializeField] private float revealDelay = 1.5f;

    private void Start()
    {
        var standings = ResultsAggregator.Instance?.GetFinalStandings() ?? new();

        // Send podium placement haptics to all phones
        foreach (var player in standings)
            ControllerGateway.Instance?.SendHapticToPlayer(player.playerId, "win");

        // Broadcast podium state to phone displays
        BroadcastPodiumState(standings);

        StartCoroutine(RevealPodium(standings));

        if (rematchButton != null)
            rematchButton.onClick.AddListener(OnRematch);
        if (newSessionButton != null)
            newSessionButton.onClick.AddListener(OnNewSession);
    }

    private IEnumerator RevealPodium(List<PlayerData> standings)
    {
        // Reveal in reverse order: 3rd → 2nd → 1st (building suspense)
        yield return new WaitForSeconds(revealDelay);

        if (standings.Count >= 3)
        {
            thirdPlaceSlot?.Reveal(standings[2]);
            yield return new WaitForSeconds(revealDelay);
        }

        if (standings.Count >= 2)
        {
            secondPlaceSlot?.Reveal(standings[1]);
            yield return new WaitForSeconds(revealDelay);
        }

        if (standings.Count >= 1)
        {
            firstPlaceSlot?.Reveal(standings[0]);
            confettiSystem?.Play();
            if (fanfareAudio != null && podiumTheme != null)
                fanfareAudio.PlayOneShot(podiumTheme);
        }

        // Session summary
        if (sessionSummaryText != null && ResultsAggregator.Instance != null)
            sessionSummaryText.text = $"{ResultsAggregator.Instance.TotalRoundsPlayed} rounds played • Thanks for playing!";
    }

    private void BroadcastPodiumState(List<PlayerData> standings)
    {
        for (int i = 0; i < standings.Count; i++)
        {
            string message = i switch
            {
                0 => "You won! \U0001f3c6",
                1 => "2nd place! \U0001f948",
                2 => "3rd place! \U0001f949",
                _ => "Thanks for playing!"
            };
            // Use UICommandMsg via ControllerGateway to avoid System.Text.Json
            ControllerGateway.Instance?.SendUICommand(standings[i].playerId, "podium",
                $"{i + 1}|{message}");
        }
    }

    private void OnRematch()
    {
        ResultsAggregator.Instance?.Reset();
        SessionStateManager.Instance?.ChangeState(SessionStateManager.LobbyState.CharacterSelect);
        _ = MiniGameLoader.Instance?.LoadLobby();
    }

    private void OnNewSession()
    {
        ResultsAggregator.Instance?.Reset();
        SessionStateManager.Instance?.Players.Clear();
        SessionStateManager.Instance?.ChangeState(SessionStateManager.LobbyState.Attract);
        _ = MiniGameLoader.Instance?.LoadShellScene("Scenes/Attract");
    }
}

[System.Serializable]
public class PodiumSlot : MonoBehaviour
{
    [SerializeField] private Image heroImage;
    [SerializeField] private Image colorRing;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private GameObject trophyObject; // Only on 1st place slot
    [SerializeField] private Animator heroAnimator;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    public void Reveal(PlayerData player)
    {
        gameObject.SetActive(true);

        if (colorRing != null) colorRing.color = player.playerColor;
        if (nicknameText != null) nicknameText.text = player.nickname;
        if (scoreText != null) scoreText.text = $"{player.sessionScore} pts";
        if (player.characterDef?.portraitSpriteLarge != null && heroImage != null)
            heroImage.sprite = player.characterDef.portraitSpriteLarge;

        trophyObject?.SetActive(true);
        heroAnimator?.SetTrigger("Win");

        // Fade in
        if (canvasGroup != null)
            StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            canvasGroup.alpha = t;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
