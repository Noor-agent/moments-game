using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Blink Shot — Mini-game #7.
/// Fast-paced top-down arena shooter. Tap phone screen to aim + shoot.
/// "Blink" teleports you to your shot's landing point — offensive repositioning.
///
/// Mechanics:
///   - Touch phone to set aim direction (touchX, touchY = screen tap, normalized -1..1)
///   - actionPressed = fire shot + blink to target (if no wall in between)
///   - Shots travel at high speed; instant hit if no blink
///   - action2Pressed = shield (2s block, 10s cooldown) — absorbs one hit
///   - 3 HP each; eliminated when HP = 0
///   - Round ends when one player remains (or time expires — most HP wins)
///
/// Scoring:
///   - Hit dealt:         +100 pts
///   - Eliminated enemy:  +400 pts
///   - Shield absorb:     +75 pts (successful block)
///   - Winner (survivor): +500 pts
///
/// TV-authoritative. Phones send: touchX, touchY, actionPressed, action2Pressed.
/// </summary>
public class BlinkShotGame : MiniGameBase
{
    // ── Inspector Config ──────────────────────────────────────────────────
    [Header("Shot Config")]
    [SerializeField] private float shotSpeed         = 28f;
    [SerializeField] private float shotLifetime      = 1.2f;
    [SerializeField] private float fireRate          = 1.8f;  // Shots/sec
    [SerializeField] private float blinkDistance     = 14f;   // Max teleport range
    [SerializeField] private LayerMask wallLayer;

    [Header("Shield")]
    [SerializeField] private float shieldDuration    = 2f;
    [SerializeField] private float shieldCooldown    = 10f;

    [Header("Player")]
    [SerializeField] private int   startHP           = 3;
    [SerializeField] private float eliminationRadius = 0.4f;  // Hitbox radius

    [Header("References")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private GameObject  shotPrefab;   // Small glowing sphere prefab

    // ── Per-player runtime ────────────────────────────────────────────────
    private class ShooterState
    {
        public Transform        body;
        public CharacterVisuals visuals;
        public PlayerData       playerData;

        // Input
        public float   aimX;
        public float   aimY;
        public bool    fireRequested;
        public bool    shieldRequested;

        // State
        public int     hp;
        public bool    isEliminated;
        public bool    isShielded;
        public float   fireCooldown;
        public float   shieldCooldownTimer;
        public float   shieldTimer;
    }

    private readonly Dictionary<string, ShooterState> _shooters = new();
    private readonly List<ActiveShot>                  _shots    = new();
    private int _aliveCount;

    // ── Active shot ───────────────────────────────────────────────────────
    private class ActiveShot
    {
        public GameObject obj;
        public Rigidbody  rb;
        public string     ownerId;
        public float      lifetime;
        public bool       isBlinkShot;
        public Vector3    targetPos;
    }

    // ── Setup ─────────────────────────────────────────────────────────────

    public override void Setup(List<PlayerData> players)
    {
        base.Setup(players);
        _shooters.Clear();
        _shots.Clear();
        _aliveCount = players.Count;

        for (int i = 0; i < players.Count; i++)
        {
            var p     = players[i];
            var spawn = spawnPoints[i % spawnPoints.Length];

            var go = new GameObject($"Shooter_{p.nickname}");
            go.transform.position = spawn.position;

            var col = go.AddComponent<SphereCollider>();
            col.radius  = eliminationRadius;
            col.isTrigger = false;

            var visuals = go.GetComponent<CharacterVisuals>();
            visuals?.Initialize(p, p.characterDef);

            _shooters[p.playerId] = new ShooterState
            {
                body       = go.transform,
                visuals    = visuals,
                playerData = p,
                hp         = startHP,
            };
        }
    }

    // ── Input ─────────────────────────────────────────────────────────────

    protected override void HandleInput(string playerId, InputMessage input)
    {
        if (!isPlaying) return;
        if (!_shooters.TryGetValue(playerId, out var s) || s.isEliminated) return;

        // Touch-to-aim (normalized screen coords → world aim vector)
        s.aimX = input.touchX;
        s.aimY = input.touchY;

        if (input.actionPressed)  s.fireRequested   = true;
        if (input.action2Pressed) s.shieldRequested = true;
    }

    // ── Update ────────────────────────────────────────────────────────────

    protected override void Update()
    {
        base.Update();
        if (!isPlaying) return;

        // Tick shooters
        foreach (var kv in _shooters)
        {
            var pid = kv.Key;
            var s   = kv.Value;
            if (s.isEliminated) continue;

            // Cooldowns
            s.fireCooldown        = Mathf.Max(0, s.fireCooldown        - Time.deltaTime);
            s.shieldCooldownTimer = Mathf.Max(0, s.shieldCooldownTimer - Time.deltaTime);
            if (s.isShielded)
            {
                s.shieldTimer -= Time.deltaTime;
                if (s.shieldTimer <= 0) DeactivateShield(pid, s);
            }

            // Fire
            if (s.fireRequested && s.fireCooldown <= 0f)
            {
                FireShot(pid, s);
                s.fireCooldown  = 1f / fireRate;
                s.fireRequested = false;
            }

            // Shield
            if (s.shieldRequested && !s.isShielded && s.shieldCooldownTimer <= 0f)
            {
                ActivateShield(pid, s);
                s.shieldRequested = false;
            }
        }

        // Tick shots
        TickShots();
    }

    // ── Shot Logic ────────────────────────────────────────────────────────

    private void FireShot(string playerId, ShooterState shooter)
    {
        var aimDir = new Vector3(shooter.aimX, 0, shooter.aimY).normalized;
        if (aimDir == Vector3.zero) aimDir = shooter.body.forward;

        // Blink destination (raycast to find landing point)
        var origin   = shooter.body.position + Vector3.up * 0.5f;
        bool hitWall = Physics.Raycast(origin, aimDir, out var hit, blinkDistance, wallLayer);
        var blinkTarget = hitWall ? hit.point - aimDir * 0.5f : origin + aimDir * blinkDistance;

        GameObject shotObj;
        if (shotPrefab != null)
            shotObj = Object.Instantiate(shotPrefab, origin, Quaternion.LookRotation(aimDir));
        else
        {
            shotObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shotObj.transform.localScale = Vector3.one * 0.25f;
            shotObj.transform.position   = origin;
        }

        var rb = shotObj.GetComponent<Rigidbody>();
        if (rb == null) rb = shotObj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.linearVelocity  = aimDir * shotSpeed;

        _shots.Add(new ActiveShot
        {
            obj          = shotObj,
            rb           = rb,
            ownerId      = playerId,
            lifetime     = shotLifetime,
            isBlinkShot  = true,
            targetPos    = blinkTarget,
        });

        // Orient shooter toward aim
        shooter.body.rotation = Quaternion.LookRotation(aimDir);

        VFXManager.Instance?.Play(VFXManager.VFXType.DashTrail, origin);
        ControllerGateway.Instance?.SendHaptic(playerId, "fire");
    }

    private void TickShots()
    {
        for (int i = _shots.Count - 1; i >= 0; i--)
        {
            var shot = _shots[i];
            shot.lifetime -= Time.deltaTime;

            if (shot.lifetime <= 0 || shot.obj == null)
            {
                if (shot.obj != null) Object.Destroy(shot.obj);
                _shots.RemoveAt(i);
                continue;
            }

            // Check hit against all other players
            foreach (var kv in _shooters)
            {
                var pid = kv.Key;
                var s   = kv.Value;
                if (pid == shot.ownerId || s.isEliminated) continue;

                float dist = Vector3.Distance(shot.obj.transform.position, s.body.position);
                if (dist < eliminationRadius + 0.5f)
                {
                    OnShotHit(shot, pid, s);
                    if (shot.obj != null) Object.Destroy(shot.obj);
                    _shots.RemoveAt(i);
                    break;
                }
            }
        }
    }

    private void OnShotHit(ActiveShot shot, string victimId, ShooterState victim)
    {
        if (victim.isShielded)
        {
            // Shield absorb
            AddScore(victimId, 75);
            DeactivateShield(victimId, victim);
            VFXManager.Instance?.Play(VFXManager.VFXType.HitSpark, victim.body.position);
            ControllerGateway.Instance?.SendHaptic(victimId, "shield_break");
            ControllerGateway.Instance?.SendUICommand(victimId, "show_feedback", "BLOCKED!");
            return;
        }

        victim.hp--;
        victim.visuals?.OnHit();
        VFXManager.Instance?.HitSpark(victim.body.position, victim.playerData.playerColor);
        ControllerGateway.Instance?.SendHaptic(victimId, "hit");
        ControllerGateway.Instance?.SendUICommand(victimId, "show_hp", victim.hp.ToString());

        AddScore(shot.ownerId, 100);

        // Blink shooter to target
        if (shot.isBlinkShot && _shooters.TryGetValue(shot.ownerId, out var attacker))
        {
            attacker.body.position = shot.targetPos;
            VFXManager.Instance?.Play(VFXManager.VFXType.ScoreBurst, shot.targetPos);
            ControllerGateway.Instance?.SendHaptic(shot.ownerId, "pickup");
        }

        if (victim.hp <= 0)
        {
            AddScore(shot.ownerId, 400);
            StartCoroutine(EliminateShooter(victimId, victim));
        }
    }

    // ── Shield ────────────────────────────────────────────────────────────

    private void ActivateShield(string playerId, ShooterState s)
    {
        s.isShielded         = true;
        s.shieldTimer        = shieldDuration;
        s.shieldCooldownTimer = shieldCooldown;

        s.visuals?.OnDashStart(); // Reuse dash glow as shield visual
        ControllerGateway.Instance?.SendHaptic(playerId, "pickup");
        ControllerGateway.Instance?.SendUICommand(playerId, "show_feedback", "🛡 SHIELD");
    }

    private void DeactivateShield(string playerId, ShooterState s)
    {
        s.isShielded  = false;
        s.shieldTimer = 0f;
        s.visuals?.OnDashEnd();
    }

    // ── Elimination ───────────────────────────────────────────────────────

    private IEnumerator EliminateShooter(string playerId, ShooterState s)
    {
        s.isEliminated = true;
        _aliveCount--;

        s.visuals?.OnEliminated();
        VFXManager.Instance?.ElimFlash(s.body.position, s.playerData.playerColor);
        ControllerGateway.Instance?.SendHaptic(playerId, "eliminated");
        ControllerGateway.Instance?.SendUICommand(playerId, "show_eliminated", "OUT!");

        base.EliminatePlayer(playerId);

        yield return new WaitForSeconds(0.5f);
        if (_aliveCount <= 1) EndGame();
    }

    // ── Final Scores ──────────────────────────────────────────────────────

    protected override void CalculateFinalScores()
    {
        foreach (var kv in _shooters)
        {
            var s = kv.Value;
            if (!s.isEliminated)
            {
                AddScore(kv.Key, 500);
                s.visuals?.OnWin();
                VFXManager.Instance?.Confetti(s.body.position + Vector3.up * 2f);
                ControllerGateway.Instance?.SendHaptic(kv.Key, "win");
            }
            else
            {
                // HP-based consolation (time-limit end)
                AddScore(kv.Key, s.hp * 50);
            }
        }
    }

    protected override void OnGameStart()
    {
        Debug.Log("[BlinkShot] ▶ Blink and you'll miss it!");
        ControllerGateway.Instance?.BroadcastHaptic("game_start");
    }

    protected override void OnGameEnd()
    {
        // Clear remaining shots
        foreach (var s in _shots)
            if (s.obj != null) Object.Destroy(s.obj);
        _shots.Clear();

        Debug.Log("[BlinkShot] ■ Cease fire!");
    }
}
