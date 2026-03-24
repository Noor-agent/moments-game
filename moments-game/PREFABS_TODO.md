# Prefabs — Art Director Notes

## Status Key
- ✅ Script-complete (logic wired, placeholder mesh in runtime)
- 🎨 Needs 3D art / particle assets before final build
- 🔊 Needs audio clip assignment in AudioManager inspector

---

## Character Prefabs (✅ script / 🎨 mesh+rig)

All 8 heroes share one base rig. Hero-specific mesh + material are swapped at runtime
via `CharacterDefinition.prefab3D` reference.

| Prefab | Script | Mesh | Portrait |
|---|---|---|---|
| Hero_Byte.prefab   | ✅ CharacterVisuals | 🎨 poly mesh | 🎨 512px sprite |
| Hero_Nova.prefab   | ✅ | 🎨 | 🎨 |
| Hero_Orbit.prefab  | ✅ | 🎨 | 🎨 |
| Hero_Striker.prefab| ✅ | 🎨 | 🎨 |
| Hero_Sizzle.prefab | ✅ | 🎨 | 🎨 |
| Hero_Shade.prefab  | ✅ | 🎨 | 🎨 |
| Hero_Dusty.prefab  | ✅ | 🎨 | 🎨 |
| Hero_Pop.prefab    | ✅ | 🎨 | 🎨 |

**Quick stand-in:** Spawn a colored capsule + CapsuleCollider at runtime —
`CharacterVisuals.Initialize()` applies MaterialPropertyBlock rim color to any Renderer.

---

## Arena Prefabs

### PolarPush ✅
- `HexIceTile.prefab` — mesh + IceSurface shader → 🎨 swap cube for real hex mesh
- `IceEdge.prefab` — edge glow strip → 🎨 glow material needs IceEdgeGlow.shader assigned

### ColorClash ✅
- `ColorTile.prefab` — cube placeholder → 🎨 swap for subdivided plane + TilePaint.shader
- Paint bomb VFX → 🎨 PaintSplat particle burst

### TankBattle ✅
- `Cannonball.prefab` — sphere + TrailRenderer → 🎨 trail material needs emissive orange
- Cover block prefab → 🎨 destructible sandstone mesh with fracture

### WaveRider ✅
- Ocean plane uses OceanWater.shader ✅ — needs texture assets:
  - `_NormalMapA`, `_NormalMapB` (water normal maps)
  - `_FoamTex` (foam texture)

### BumperBlitz ✅
- RotatingBumper → 🎨 bumper car mesh (cylinder placeholder works)
- Boost pad → 🎨 neon arrow decal + point light

### BlinkShot ✅
- `ShotPrefab.prefab` — sphere + trail → 🎨 trail-cyan material emissive
- Wall obstacle → 🎨 neon-lit concrete block

### GravityGrab ✅
- Orb tiers (White/Blue/Gold) — sphere primitives in runtime → 🎨 glowing sphere materials
- Space platform → 🎨 ring mesh with emission

---

## VFX Prefabs (Particle Systems)

Assigned to VFXManager SerializedField `vfxEntries[]`.

| VFXType | Prefab | Status |
|---|---|---|
| HitSpark    | VFX_HitSpark.prefab    | ✅ structure, 🎨 material |
| DashTrail   | VFX_DashTrail.prefab   | 🎨 create |
| ScoreBurst  | VFX_ScoreBurst.prefab  | 🎨 create |
| Confetti    | VFX_Confetti.prefab    | 🎨 create |
| Explosion   | VFX_Explosion.prefab   | 🎨 create |
| PaintSplat  | VFX_PaintSplat.prefab  | 🎨 create |
| IceCrack    | VFX_IceCrack.prefab    | 🎨 create |
| ElimFlash   | VFX_ElimFlash.prefab   | 🎨 create |
| JoinPing    | VFX_JoinPing.prefab    | 🎨 create |

**Quick stand-in:** VFXManager gracefully logs a warning if pool is empty — game still runs.

---

## UI Prefabs (Canvas)

| Prefab | Status |
|---|---|
| LobbyPlayerCard.prefab  | 🎨 needs Image + TMP_Text refs wired in prefab inspector |
| ResultCard.prefab       | 🎨 needs Image + TMP_Text refs |
| QRCodeDisplay.prefab    | ✅ QRCodeDisplay.cs + ZXing (add via UPM or DLL) |

---

## Audio Clips

Assign in `AudioManager` inspector → `sfxEntries[]` dictionary:

| id | Clip | Notes |
|---|---|---|
| countdown   | countdown_beep.wav  | Short tick, ~0.2s |
| game_start  | game_start.wav      | 0.5s stinger |
| dash        | swoosh.wav          | Very short |
| hit         | hit.wav             | Punchy thud |
| pickup      | pickup.wav          | Bright chirp |
| win         | win_fanfare.wav     | 1–2s |
| elimination | elimination.wav     | Descending tone |
| ice_crack   | ice_crack.wav       | Brittle snap |
| shield_break| shield_break.wav    | Glass shatter |

Music loops: `musicLobby`, `musicPolarPush`, `musicResults`, `musicPodium` — assign in inspector.

---

## What's Fully Code-Complete (no art needed to run)

- All 7 mini-game gameplay scripts ✅
- WebSocket server (RFC 6455) ✅
- Phone controller HTML (all 7 layouts) ✅
- Session state machine ✅
- Results + podium flow ✅
- Addressables scene loading ✅
- VFX pool (graceful fallback) ✅
- Haptic routing to phones ✅

The game will **run end-to-end** with Unity primitive placeholders.
Art pass is required before release.
