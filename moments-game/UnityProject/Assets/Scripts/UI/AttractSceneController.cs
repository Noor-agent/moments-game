using UnityEngine;
using System.Collections;
using TMPro;

/// <summary>
/// Attract scene controller.
/// Animates the title, pulses the QR code, and cycles hero preview silhouettes.
/// Transitions to Lobby as soon as the first player scans.
/// </summary>
public class AttractSceneController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI taglineText;
    [SerializeField] private TextMeshProUGUI scanPromptText;
    [SerializeField] private QRCodeDisplay qrDisplay;
    [SerializeField] private CanvasGroup uiGroup;

    [Header("Animation")]
    [SerializeField] private float titlePulsePeriod = 3f;
    [SerializeField] private float scanPromptBlinkPeriod = 1.2f;

    [Header("Hero Preview")]
    [SerializeField] private Transform heroShowcaseRoot;
    [SerializeField] private CharacterDefinition[] heroRoster;  // Drag all 8 CharDefs here
    [SerializeField] private float heroRotateInterval = 2.5f;

    private int _currentHeroIndex;
    private SessionStateManager _session;

    // Player color cycling for the title text
    private readonly Color[] _titleColors = {
        new(0f, 0.75f, 1f),    // Byte blue
        new(1f, 0.84f, 0f),    // Nova gold
        new(0f, 1f, 1f),       // Orbit cyan
        new(0.5f, 1f, 0f),     // Striker lime
        new(1f, 0.42f, 0f),    // Sizzle orange
        new(0.61f, 0.35f, 0.71f), // Shade purple
        new(0.82f, 0.41f, 0.12f), // Dusty brown
        new(1f, 0.08f, 0.58f),  // Pop pink
    };

    private void Start()
    {
        _session = SessionStateManager.Instance;
        if (_session != null)
            _session.OnPlayerJoined += OnFirstPlayerJoined;

        qrDisplay?.RefreshQR();

        StartCoroutine(TitleColorCycle());
        StartCoroutine(ScanPromptBlink());
        if (heroShowcaseRoot != null && heroRoster?.Length > 0)
            StartCoroutine(HeroRotation());
    }

    private void OnFirstPlayerJoined(PlayerData player)
    {
        // Transition to Lobby as soon as someone scans
        StartCoroutine(TransitionToLobby());
    }

    private IEnumerator TransitionToLobby()
    {
        // Fade out attract
        if (uiGroup != null)
        {
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime * 2f;
                uiGroup.alpha = t;
                yield return null;
            }
        }

        // Load lobby scene (fire and forget from coroutine)
        MiniGameLoader.Instance?.LoadLobby();
        yield return null;
    }

    private IEnumerator TitleColorCycle()
    {
        int colorIndex = 0;
        while (true)
        {
            if (titleText != null)
            {
                Color targetColor = _titleColors[colorIndex % _titleColors.Length];
                float elapsed = 0f;
                Color startColor = titleText.color;
                while (elapsed < titlePulsePeriod)
                {
                    elapsed += Time.deltaTime;
                    titleText.color = Color.Lerp(startColor, targetColor, elapsed / titlePulsePeriod);
                    yield return null;
                }
                colorIndex++;
            }
            else yield return new WaitForSeconds(titlePulsePeriod);
        }
    }

    private IEnumerator ScanPromptBlink()
    {
        while (true)
        {
            yield return new WaitForSeconds(scanPromptBlinkPeriod);
            if (scanPromptText != null)
            {
                float a = scanPromptText.alpha;
                scanPromptText.alpha = (a > 0.5f) ? 0.3f : 1f;
            }
        }
    }

    private IEnumerator HeroRotation()
    {
        while (true)
        {
            yield return new WaitForSeconds(heroRotateInterval);
            _currentHeroIndex = (_currentHeroIndex + 1) % heroRoster.Length;
            // Spawn/swap hero preview (implementation: instantiate prefab, fade in/out)
            // In production: use object pool and lerp scale/alpha
        }
    }

    private void OnDestroy()
    {
        if (_session != null) _session.OnPlayerJoined -= OnFirstPlayerJoined;
    }
}
