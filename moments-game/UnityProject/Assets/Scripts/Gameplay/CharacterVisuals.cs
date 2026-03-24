using UnityEngine;

/// <summary>
/// Runtime helper attached to every spawned character prefab.
/// Sets player color on the CharacterLit shader rim, name label, and color frame.
/// Handles win glow pulse animation.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class CharacterVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Renderer characterRenderer;
    [SerializeField] private TMPro.TextMeshPro nameLabel;
    [SerializeField] private ParticleSystem dashTrailVFX;
    [SerializeField] private ParticleSystem eliminationVFX;
    [SerializeField] private ParticleSystem winGlowVFX;

    [Header("Win Pulse")]
    [SerializeField] private float winPulseDuration = 4f;
    [SerializeField] private AnimationCurve winGlowCurve;

    private MaterialPropertyBlock _propertyBlock;
    private static readonly int RimColorId      = Shader.PropertyToID("_RimColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionStrId   = Shader.PropertyToID("_EmissionStrength");

    private Color _playerColor;
    private bool  _isWinPulsing;

    /// <summary>The player's assigned colour. Used by VFX systems for tinting.</summary>
    public Color PlayerColor => _playerColor;

    private void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        if (characterRenderer == null)
            characterRenderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Call once after spawning to bind player data.
    /// </summary>
    public void Initialize(PlayerData player, CharacterDefinition def)
    {
        _playerColor = player.playerColor;
        Color accentColor = def != null ? def.accentColor : _playerColor * 1.5f;

        // Build procedural hero geometry
        if (def != null && !string.IsNullOrEmpty(def.heroId))
        {
            CharacterArtBuilder.Build(
                def.heroId,
                gameObject,
                _playerColor,
                accentColor,
                characterRenderer?.sharedMaterial);
        }

        // Set rim color via MaterialPropertyBlock on main renderer (no material instancing)
        if (characterRenderer != null)
        {
            _propertyBlock.SetColor(RimColorId, _playerColor);
            _propertyBlock.SetFloat(EmissionStrId, 0f);
            characterRenderer.SetPropertyBlock(_propertyBlock);
        }

        // Name label above head
        if (nameLabel != null)
        {
            nameLabel.text  = player.nickname;
            nameLabel.color = _playerColor;
        }

        // Dash trail particle color
        if (dashTrailVFX != null)
        {
            var main = dashTrailVFX.main;
            main.startColor = new ParticleSystem.MinMaxGradient(_playerColor);
        }
    }

    // ── Dash Trail ────────────────────────────────────────────────────────────

    public void OnDashStart()  => dashTrailVFX?.Play();
    public void OnDashEnd()    => dashTrailVFX?.Stop();

    // ── Elimination ───────────────────────────────────────────────────────────

    public void OnEliminated()
    {
        eliminationVFX?.Play();
        // Greyscale the character material
        _propertyBlock.SetColor(RimColorId, Color.grey);
        _propertyBlock.SetFloat(EmissionStrId, 0f);
        characterRenderer.SetPropertyBlock(_propertyBlock);

        if (nameLabel != null) nameLabel.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        // Disable collider so eliminated player doesn't block others
        var col = GetComponentInChildren<Collider>();
        if (col != null) col.enabled = false;
    }

    // ── Win Pulse ─────────────────────────────────────────────────────────────

    public void OnWin()
    {
        winGlowVFX?.Play();
        if (!_isWinPulsing)
            StartCoroutine(WinPulseCoroutine());
    }

    private System.Collections.IEnumerator WinPulseCoroutine()
    {
        _isWinPulsing = true;
        float elapsed = 0f;

        while (elapsed < winPulseDuration)
        {
            float t = winGlowCurve.Evaluate(elapsed / winPulseDuration);
            _propertyBlock.SetFloat(EmissionStrId, t * 2.5f);
            _propertyBlock.SetColor(EmissionColorId, _playerColor * t);
            characterRenderer.SetPropertyBlock(_propertyBlock);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _propertyBlock.SetFloat(EmissionStrId, 0f);
        characterRenderer.SetPropertyBlock(_propertyBlock);
        _isWinPulsing = false;
    }

    // ── Hit Flash ─────────────────────────────────────────────────────────────

    public void OnHit()
    {
        StopAllCoroutines();
        StartCoroutine(HitFlashCoroutine());
    }

    private System.Collections.IEnumerator HitFlashCoroutine()
    {
        _propertyBlock.SetColor(EmissionColorId, Color.white);
        _propertyBlock.SetFloat(EmissionStrId, 3f);
        characterRenderer.SetPropertyBlock(_propertyBlock);
        yield return new WaitForSeconds(0.08f);
        _propertyBlock.SetFloat(EmissionStrId, 0f);
        characterRenderer.SetPropertyBlock(_propertyBlock);
    }
}
