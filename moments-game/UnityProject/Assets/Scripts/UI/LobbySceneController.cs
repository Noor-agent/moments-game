using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// TV Lobby scene controller.
/// Displays: QR code, player cards (avatar/name/hero/ready state), host controls.
/// Subscribes to SessionStateManager events for real-time updates.
/// </summary>
public class LobbySceneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QRCodeDisplay qrCodeDisplay;
    [SerializeField] private Transform playerCardsContainer;
    [SerializeField] private GameObject playerCardPrefab;
    [SerializeField] private TextMeshProUGUI waitingText;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI playerCountText;

    [Header("Mascots")]
    [SerializeField] private Animator[] lobbyMascots;

    private readonly List<LobbyPlayerCard> _cards = new();
    private SessionStateManager _session;

    private void Start()
    {
        _session = SessionStateManager.Instance;
        if (_session == null) { Debug.LogError("[Lobby] No SessionStateManager found!"); return; }

        _session.OnPlayerJoined += OnPlayerJoined;
        _session.OnPlayerReady += OnPlayerReady;
        _session.OnPlayerDisconnected += OnPlayerDisconnected;

        RefreshAllCards();

        if (startButton != null)
            startButton.onClick.AddListener(OnHostStartPressed);

        // Refresh QR
        qrCodeDisplay?.RefreshQR();

        // Animate mascots
        foreach (var m in lobbyMascots)
            m?.SetTrigger("Idle");
    }

    private void OnPlayerJoined(PlayerData player)
    {
        RefreshAllCards();
        UpdateWaitingText();
    }

    private void OnPlayerReady(PlayerData player)
    {
        UpdateCard(player);
        CheckAutoStart();
    }

    private void OnPlayerDisconnected(PlayerData player)
    {
        RefreshAllCards();
        UpdateWaitingText();
    }

    private void RefreshAllCards()
    {
        // Clear existing
        foreach (Transform child in playerCardsContainer)
            Destroy(child.gameObject);
        _cards.Clear();

        // Rebuild
        var players = _session.Players;
        foreach (var p in players)
        {
            var cardObj = Instantiate(playerCardPrefab, playerCardsContainer);
            var card = cardObj.GetComponent<LobbyPlayerCard>();
            if (card != null)
            {
                card.Setup(p);
                _cards.Add(card);
            }
        }

        UpdateWaitingText();
    }

    private void UpdateCard(PlayerData player)
    {
        var card = _cards.Find(c => c.PlayerId == player.playerId);
        card?.Refresh(player);
    }

    private void UpdateWaitingText()
    {
        int count = _session.Players.Count;
        if (waitingText != null)
            waitingText.text = count == 0
                ? "Scan the QR code to join!"
                : $"{count} player{(count != 1 ? "s" : "")} in the room";

        if (playerCountText != null)
            playerCountText.text = $"{count}/8";
    }

    private void CheckAutoStart()
    {
        if (_session.Players.Count >= 2 && _session.AllPlayersReady())
            StartCoroutine(CountdownToStart());
    }

    private IEnumerator CountdownToStart()
    {
        // Pre-warm the first mini-game (placeholder — hook to game selector when ready)
        // MiniGameLoader.Instance?.PreWarmMiniGame(firstGame);

        for (int i = 3; i >= 1; i--)
        {
            ControllerGateway.Instance?.BroadcastCountdown(i);
            AudioManager.Instance?.OnCountdown();
            yield return new WaitForSeconds(1f);
        }

        _session.ChangeState(SessionStateManager.LobbyState.InGame);
        // MiniGameLoader handles scene transition via SessionStateManager.OnStateChanged
    }

    private void OnHostStartPressed()
    {
        if (_session.Players.Count < 1) return;
        StartCoroutine(CountdownToStart());
    }

    private void OnDestroy()
    {
        if (_session != null)
        {
            _session.OnPlayerJoined -= OnPlayerJoined;
            _session.OnPlayerReady -= OnPlayerReady;
            _session.OnPlayerDisconnected -= OnPlayerDisconnected;
        }
    }
}

/// <summary>
/// Individual player card on the TV lobby screen.
/// Shows: hero portrait, player color frame, nickname, ready ring.
/// </summary>
public class LobbyPlayerCard : MonoBehaviour
{
    [SerializeField] private Image colorFrame;
    [SerializeField] private Image heroBust;
    [SerializeField] private TextMeshProUGUI nicknameText;
    [SerializeField] private GameObject readyRing;
    [SerializeField] private TextMeshProUGUI heroNameText;
    [SerializeField] private GameObject disconnectedOverlay;

    public string PlayerId { get; private set; }

    public void Setup(PlayerData player)
    {
        PlayerId = player.playerId;
        Refresh(player);
    }

    public void Refresh(PlayerData player)
    {
        if (colorFrame != null) colorFrame.color = player.playerColor;
        if (nicknameText != null) nicknameText.text = player.nickname;
        if (readyRing != null) readyRing.SetActive(player.isReady);
        if (disconnectedOverlay != null) disconnectedOverlay.SetActive(!player.isConnected);

        if (player.characterDef != null)
        {
            if (heroBust != null) heroBust.sprite = player.characterDef.portraitSprite;
            if (heroNameText != null) heroNameText.text = player.characterDef.displayName;
        }
        else
        {
            if (heroNameText != null) heroNameText.text = "Choosing...";
        }
    }
}
