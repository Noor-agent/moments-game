using UnityEngine;

/// <summary>
/// Character definition asset. One per hero. Lives in Resources/Characters/.
/// Referenced by TV lobby, character select, mini-game spawners, and results screen.
/// </summary>
[CreateAssetMenu(fileName = "CharacterDef_", menuName = "Moments/Character Definition")]
public class CharacterDefinition : ScriptableObject
{
    [Header("Identity")]
    public string heroId;           // "byte", "nova", "orbit", "striker", "sizzle", "shade", "dusty", "pop"
    public string displayName;
    [TextArea(1, 2)]
    public string fantasyLine;      // One-liner for character card
    public bool isAvailable = true;

    [Header("Visuals")]
    public Color primaryColor;
    public Color accentColor;
    public string colorHex;         // CSS hex for phone controller frame
    public Sprite portraitSprite;       // 512×512, phone character card
    public Sprite portraitSpriteLarge;  // 1024×1024, TV lineup
    public Sprite iconSprite;           // 128×128, HUD/scoreboard
    public GameObject prefab3D;         // Shared rig, hero-specific mesh + materials
    public string emoji;                // Fallback for phone when portrait not loaded

    [Header("Audio")]
    public AudioClip[] voicePack;       // [0]=Join, [1]=Ready, [2]=Win, [3]=Lose, [4-6]=Emotes
    public AudioClip signatureStinger;  // Short sound on character lock-in

    [Header("Animation")]
    public AnimationClip idleClip;
    public AnimationClip[] emoteClips;  // 3 emotes per hero
    public string emoteNames;           // Comma-separated display names

    [Header("Character Select")]
    public string colorTag;         // For UI theming
    public RuntimeAnimatorController animatorController;
}
