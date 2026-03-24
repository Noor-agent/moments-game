using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Color Clash — Mini-game #2.
/// Paint floor tiles your color by running over them. Most tiles at time's end wins.
/// Proves: territory capture + TV floor-state readability.
/// </summary>
public class ColorClashGame : MiniGameBase
{
    [Header("Color Clash Config")]
    [SerializeField] private float moveSpeed = 9f;
    [SerializeField] private int gridWidth = 8;
    [SerializeField] private int gridHeight = 8;
    [SerializeField] private float tileSize = 2f;
    [SerializeField] private float paintBombRadius = 3f;
    [SerializeField] private float paintBombCooldown = 15f;

    [Header("References")]
    [SerializeField] private ColorTile[] tileGrid; // 8×8 = 64 tiles assigned in Inspector

    private readonly Dictionary<string, Vector2> _moveInputs = new();
    private readonly Dictionary<string, float> _bombCooldowns = new();
    private readonly Dictionary<string, Rigidbody> _rigidbodies = new();

    private int[] _tileOwners; // Index = tile index, value = playerSlot (-1 = unclaimed)

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        _tileOwners = new int[gridWidth * gridHeight];
        for (int i = 0; i < _tileOwners.Length; i++) _tileOwners[i] = -1;

        foreach (var p in players)
        {
            _moveInputs[p.playerId] = Vector2.zero;
            _bombCooldowns[p.playerId] = 0f;
        }
    }

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying || input.type != "input") return;
        _moveInputs[playerId] = new Vector2(input.moveX, input.moveY);

        if (input.actionPressed && _bombCooldowns.TryGetValue(playerId, out float cd) && cd <= 0f)
            TriggerPaintBomb(playerId);
    }

    private void TriggerPaintBomb(string playerId)
    {
        _bombCooldowns[playerId] = paintBombCooldown;
        ControllerGateway.Instance?.SendHaptic(playerId, "pickup");

        var player = activePlayers.Find(p => p.playerId == playerId);
        if (player == null) return;

        if (_rigidbodies.TryGetValue(playerId, out var rb))
        {
            // Paint all tiles within radius
            var pos = rb.position;
            foreach (var tile in tileGrid)
            {
                if (Vector3.Distance(tile.transform.position, pos) <= paintBombRadius)
                    PaintTile(tile, player);
            }
        }
    }

    protected override void Update()
    {
        base.Update();
        if (!isPlaying) return;

        foreach (var player in activePlayers)
        {
            var pid = player.playerId;
            if (_rigidbodies.TryGetValue(pid, out var rb))
            {
                var input = _moveInputs.TryGetValue(pid, out var v) ? v : Vector2.zero;
                rb.linearVelocity = new Vector3(input.x * moveSpeed, 0, input.y * moveSpeed);

                // Paint tile under player
                PaintTileUnder(rb.position, player);
            }

            // Bomb cooldown
            if (_bombCooldowns.ContainsKey(pid))
                _bombCooldowns[pid] = Mathf.Max(0f, _bombCooldowns[pid] - Time.deltaTime);
        }
    }

    private void PaintTileUnder(Vector3 position, PlayerData player)
    {
        int col = Mathf.FloorToInt((position.x + (gridWidth * tileSize / 2f)) / tileSize);
        int row = Mathf.FloorToInt((position.z + (gridHeight * tileSize / 2f)) / tileSize);
        if (col < 0 || col >= gridWidth || row < 0 || row >= gridHeight) return;

        int idx = row * gridWidth + col;
        if (_tileOwners[idx] != player.playerSlot)
        {
            _tileOwners[idx] = player.playerSlot;
            tileGrid[idx].SetColor(player.playerColor);
        }
    }

    private void PaintTile(ColorTile tile, PlayerData player)
    {
        tile.SetColor(player.playerColor);
        VFXManager.Instance?.Play(VFXManager.VFXType.PaintSplat, tile.transform.position + Vector3.up * 0.1f);
    }

    protected override void CalculateFinalScores()
    {
        // Count tiles per player — also push score preview to phones in last 10s of game
        var tileCounts = new Dictionary<int, int>();
        foreach (var slot in _tileOwners)
            if (slot >= 0)
                tileCounts.TryGetValue(slot, out _);

        int topTiles = 0;
        foreach (var p in activePlayers)
        {
            int tiles = 0;
            foreach (var slot in _tileOwners)
                if (slot == p.playerSlot) tiles++;
            if (tiles > topTiles) topTiles = tiles;
        }

        foreach (var p in activePlayers)
        {
            int tiles = 0;
            foreach (var slot in _tileOwners)
                if (slot == p.playerSlot) tiles++;

            int pts = tiles * 12;
            AddScore(p.playerId, pts);

            // Territory lead bonus
            if (tiles == topTiles && topTiles > 0)
                AddScore(p.playerId, 200);

            ControllerGateway.Instance?.SendUICommand(p.playerId, "show_feedback",
                $"{tiles} tiles · {pts + (tiles == topTiles ? 200 : 0)} pts");
            Debug.Log($"[ColorClash] {p.nickname}: {tiles} tiles = {pts} pts");
        }
    }

    protected override void OnGameStart()
    {
        Debug.Log("[ColorClash] GO!");
        ControllerGateway.Instance?.BroadcastHaptic("game_start");
    }

    protected override void OnGameEnd()
    {
        Debug.Log("[ColorClash] Time's up!");
        if (tileGrid != null)
            foreach (var t in tileGrid)
                t?.FreezeColor();
        VFXManager.Instance?.Confetti(Vector3.up * 2f);
    }
}

/// <summary>
/// Individual tile component. Animates color transition on paint using MaterialPropertyBlock
/// (zero material instances created — safe for 64 tiles at 60fps).
/// </summary>
public class ColorTile : MonoBehaviour
{
    [SerializeField] private Renderer tileRenderer;
    [SerializeField] private float transitionSpeed = 10f;

    private Color _currentColor = Color.white;
    private Color _targetColor  = Color.white;
    private bool  _frozen;

    private MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int BlendFactor = Shader.PropertyToID("_BlendFactor");

    private void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        if (tileRenderer == null) tileRenderer = GetComponent<Renderer>();
        // Set initial white
        tileRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorId, Color.white);
        tileRenderer.SetPropertyBlock(_mpb);
    }

    public void SetColor(Color color)
    {
        if (_frozen) return;
        _targetColor = color;
    }

    /// <summary>Called at game end — lock the displayed color in place.</summary>
    public void FreezeColor()
    {
        _frozen = true;
    }

    private void Update()
    {
        if (_frozen) return;

        _currentColor = Color.Lerp(_currentColor, _targetColor, Time.deltaTime * transitionSpeed);

        tileRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(BaseColorId, _currentColor);
        // TilePaint shader drives emission pulse via BlendFactor; 1 = fully painted
        float blend = 1f - Color.Distance(_currentColor, _targetColor) / 1.732f; // sqrt(3) max dist
        _mpb.SetFloat(BlendFactor, Mathf.Clamp01(blend));
        tileRenderer.SetPropertyBlock(_mpb);
    }
}
