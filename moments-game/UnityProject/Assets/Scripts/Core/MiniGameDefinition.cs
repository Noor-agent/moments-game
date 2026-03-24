using UnityEngine;

/// <summary>
/// Mini-game definition asset. One per game mode.
/// Drives: scene loading, phone layout swap, scoring rules, timer.
/// Lives in Resources/MiniGames/.
/// </summary>
[CreateAssetMenu(fileName = "MiniGameDef_", menuName = "Moments/Mini-Game Definition")]
public class MiniGameDefinition : ScriptableObject
{
    [Header("Identity")]
    public string gameId;              // "polar-push", "color-clash", "tank-battle", etc.
    public string displayName;
    [TextArea(1, 2)]
    public string rulesLine;           // Shown on TV rules card (must fit 2 lines, large font)
    public Sprite thumbnail;           // Mini-game selection card image

    [Header("Gameplay")]
    public int durationSeconds = 90;
    [Range(2, 8)]
    public int minPlayers = 2;
    [Range(2, 8)]
    public int maxPlayers = 8;
    public ScoringMode scoringMode;
    public bool hasElimination = true;
    public int respawnCount = 0;

    [Header("Controller")]
    public ControllerLayout controllerLayout;  // Which phone UI layout to activate
    public bool usesAccelerometer = false;      // Wave Rider uses device tilt

    [Header("Addressables")]
    public string sceneAddress;        // Addressables key for the mini-game scene

    [Header("Audio")]
    public AudioClip signatureLoop;    // Background music loop for this arena
    public AudioClip introStinger;     // 2–3 second hit when game starts
    public AudioClip rulesVoiceover;   // Announcer reading the rules
}

public enum ScoringMode
{
    LastStanding,    // Polar Push, Tank Battle
    TileCount,       // Color Clash
    TimeSurvived,    // Wave Rider
    LapsFirst,       // Bumper Blitz
    HighScore,       // Blink Shot, Gravity Grab
}

public enum ControllerLayout
{
    MoveAndDash,
    MoveOnly,
    TankTwinStick,
    TiltAndDuck,
    SteerAndBump,
    AimAndFire,
    SwipeAndGrab,
}
