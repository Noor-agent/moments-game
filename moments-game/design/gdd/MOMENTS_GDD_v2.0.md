# MOMENTS — Game Design Document v2.0
## TV-as-Server Party Platform | Unity 6 | URP

---

## 1. Executive Vision

**Moments** is a couch-first multiplayer party game where the TV hosts the entire game session and players join instantly by scanning a QR code on their phones. No app download. No controllers. No setup friction.

Inspired by the chaotic, bite-sized joy of **Crash Bash (PS1)** — fast mini-game arenas, exaggerated 3D characters, and social energy that fills a room — Moments brings that experience into the modern living room with zero hardware requirements.

> **Tagline:** *"Every game is a moment worth remembering."*

**Session Length:** 15–30 minutes  
**Players:** 2–8  
**Platform:** Smart TV / PC display (host) + iOS/Android browsers (controllers)  
**Engine:** Unity 6, URP

---

## 2. Experience Pillars

| Pillar | Description |
|--------|-------------|
| ⚡ Instant Social Entry | Any player joins in under 10 seconds via QR scan — no app, no account |
| 👁️ Readable Chaos | At peak action, the audience understands who's winning, who got hit, what the objective is |
| 🎲 Short-Form Variety | Each mini-game uses a different input verb — no two games feel the same |
| 🦸 Character Attachment | 8 pre-designed heroes carry player identity across all games and results screens |
| 📺 TV-First Spectacle | The television is the center of gravity — hero shots, podiums, and reactions feel cinematic |

---

## 3. Session Loop

```
[TV Boot]
    │
    ▼
[Attract Screen]
  ┌─ Animated mascots
  ├─ Large QR code + room code
  └─ Controller preview loops
    │
    ▼
[Lobby / QR Join]
  ┌─ Players scan QR → phone opens web controller
  ├─ Enter nickname (auto-generate option)
  ├─ Browse 8-hero carousel → hold to lock
  ├─ TV shows live player cards (avatar, color, ready ring)
  └─ Host can set playlist: Random / Vote / Curated
    │
    ▼
[Mini-Game Intro]
  ┌─ 6–8 second rules card on TV
  ├─ Phone shows ONLY controls needed for this game
  └─ Announcer stinger + arena fly-in
    │
    ▼
[Mini-Game Play] ◄────────────────────┐
  ┌─ 60–120 seconds per round          │
  ├─ TV: game state, HUD, camera drama │
  ├─ Phone: minimal controls + haptics │
  └─ Elimination / objective complete  │
    │                                  │
    ▼                                  │
[Round Results]                        │
  ┌─ Replay highlight shot             │
  ├─ Medal animations                  │
  ├─ Running standings                 │
  └─ Next game card → vote or auto ───┘
    │
    ▼
[Final Podium]
  ┌─ 1st/2nd/3rd hero animations
  ├─ Confetti + announcer
  ├─ Trophy moment for winner
  └─ Rematch / New Session
```

---

## 4. Character Roster — The 8 Heroes

### Visual Style: Premium Toy-Like 3D
- Oversized heads (1.5× body ratio), simplified torso, strong hands
- Bold silhouette accessories readable at TV distance (2–5m)
- Toy-like URP materials: clean roughness, soft rim lighting, broad value separation
- Common rig shared across all 8 heroes (efficient Unity production)

### Hero Roster

| # | Name | Archetype | Color | Signature Prop | One-Line Fantasy |
|---|------|-----------|-------|---------------|-----------------|
| 1 | **Byte** | Arcade Kid | Electric Blue | Game controller backpack | The fastest button-masher in the galaxy |
| 2 | **Nova** | Young Inventor | Golden Yellow | Holographic goggles | Built her first gadget at age 5 |
| 3 | **Orbit** | Mini Astronaut | White + Cyan | Jetpack | Lost in space, winning everywhere else |
| 4 | **Striker** | Street Footballer | Neon Green | Soccer ball on chain | Never met a goal he couldn't score |
| 5 | **Sizzle** | Party Chef | Fiery Orange | Giant spatula | Turns every arena into a cook-off |
| 6 | **Shade** | Purple Ninja | Deep Violet | Smoke bomb | Appears from nowhere, wins from nowhere |
| 7 | **Dusty** | Cowboy Adventurer | Warm Brown | Lasso | Rides into every game like it's the Wild West |
| 8 | **Pop** | Roller Skater | Hot Pink | Rollerskates + headphones | Always first to the party, first to win |

### CharacterDefinition ScriptableObject
```csharp
[CreateAssetMenu(fileName = "CharacterDef", menuName = "Moments/CharacterDefinition")]
public class CharacterDefinition : ScriptableObject
{
    public string heroId;           // "byte", "nova", etc.
    public string displayName;
    public string fantasyLine;      // One-liner shown on character card
    public Color primaryColor;
    public Color accentColor;
    public Sprite portraitSprite;   // 512×512 WebP, optimized for phone
    public Sprite tvPortraitSprite; // 1024×1024 for TV lineup
    public GameObject prefab3D;     // Shared rig, unique mesh/materials
    public AudioClip[] voicePack;   // Join, ready, win, lose, emote
    public AnimationClip[] emotes;  // 3 emotes per hero
    public string colorTag;         // CSS color for phone controller frame
    public bool isAvailable;
}
```

---

## 5. Mini-Game Lineup (Launch: 7 games)

### Crash Bash-Inspired Design Philosophy
Each arena is a "playable diorama" — one dominant color script, one signature animated set piece, one clear objective readable in seconds.

---

### 🏔️ Mini-Game 1: POLAR PUSH
**Verb:** Movement + Dash  
**Objective:** Last player standing on the ice platform wins  
**Duration:** 90 seconds max  
**Players:** 2–8

**Arena:** Arctic glacier platform, cracking ice edges, aurora borealis sky. Ice chunks break away as players get pushed off. Polar bear mascot on the side reacts to near-misses.

**Mechanics:**
- Move with left thumb joystick, dash with right tap button
- Physical character bodies push others toward edges
- Ice tiles crack and fall after taking impacts — arena shrinks over time
- Dash cooldown: 2 seconds. Dash-chain combos create extra knockback

**Phone Controller:**
```
[  MOVE JOYSTICK  ] [DASH]
```

**Crash Bash Parallel:** Crate Crush / Polar Push arenas — simple boundary, physical chaos

**3D Design Spec:**
- Hexagonal ice platform, 20m diameter
- URP ice shader: subsurface scattering, refraction layer, crack decals
- Crack VFX: procedural ice fracture particle system
- Falling tile: physics rigid body + dissolve shader
- Sky: animated aurora with volumetric clouds

---

### 🎨 Mini-Game 2: COLOR CLASH
**Verb:** Territory Capture  
**Objective:** Own the most floor tiles when time runs out  
**Duration:** 60 seconds  
**Players:** 2–8

**Arena:** A floating candy-colored grid platform above the clouds. Each tile flashes the last player's color who ran over it. Confetti cannons at corners fire whenever a player takes a lead.

**Mechanics:**
- Run over tiles to paint them your color
- Run over enemy tiles to steal them
- Power-up: "Paint Bomb" — explodes in your color in a 3-tile radius
- TV scoreboard shows live tile count per player as a pie chart

**Phone Controller:**
```
[  MOVE JOYSTICK  ]
```

**Crash Bash Parallel:** Color Crash — territory capture with clear visual feedback

**3D Design Spec:**
- 8×8 grid of flat tile panels, each 2m × 2m
- Tile material: URP lit shader with color lerp animation (0.1s transition)
- Paint explosion VFX: radial splat particle with color inheritance
- Platform floats above stylized cloud layer (scrolling texture + billboard clouds)
- TV HUD: real-time pie chart overlay per player color

---

### ⚔️ Mini-Game 3: TANK BATTLE
**Verb:** Steering + Fire  
**Objective:** Eliminate all other tanks. Last tank wins.  
**Duration:** 120 seconds  
**Players:** 2–6

**Arena:** A top-down desert canyon with destructible cover blocks. Cactus mascots cheer from the sidelines. Dust devils reveal hidden power-up crates.

**Mechanics:**
- Left stick: tank throttle (forward/backward)
- Right stick or tilt: turret aim
- Fire button: shoots cannonball (3-second reload)
- Ricochet off metal walls — advanced players use bank shots
- Respawn once with half HP; second elimination = out

**Phone Controller:**
```
[DRIVE JOYSTICK] [AIM JOYSTICK] [FIRE]
```

**3D Design Spec:**
- Isometric arena, 30m × 30m, viewed from fixed 45° TV camera
- Tank models: chunky toy-like proportions, player color on hull
- Destructible cover: breakable mesh with URP shader dissolve on impact
- Cannonball trail: particle ribbon + impact crater decal
- Dust VFX: volumetric dust particle on movement

---

### 🌊 Mini-Game 4: WAVE RIDER
**Verb:** Balancing + Timing  
**Objective:** Stay on your surfboard the longest as waves get bigger  
**Duration:** 90 seconds  
**Players:** 2–8

**Arena:** Tropical ocean beach, giant progressive waves, crowd of mascots on shore cheering. Coconuts fly in as obstacles.

**Mechanics:**
- Tilt phone left/right to balance on surfboard
- Tap to duck under obstacles
- Waves increase in size every 15 seconds
- Fall off the board = eliminated. Last surfer wins.

**Phone Controller:**
```
[← TILT →] [DUCK TAP]
```

**3D Design Spec:**
- Wave: animated sine-blend mesh with URP water shader (normal maps, foam at crests)
- Character: ragdoll physics on surfboard with balance spring constraint
- Obstacle coconuts: physics objects with bezier spawn paths
- Shore crowd: LOD billboard mascots with reaction animations

---

### 🏎️ Mini-Game 5: BUMPER BLITZ
**Verb:** Racing + Bumping  
**Objective:** Complete 3 laps first — but bumping opponents slows their car  
**Duration:** 90 seconds  
**Players:** 2–6

**Arena:** Candy-colored circular kart track with bumper obstacles, ramps, and a giant spinning wheel decoration in the center. Confetti explosions at finish line.

**Mechanics:**
- Left stick: steer; auto-accelerate (no brake needed)
- Bump button: activates side-shield that slams into adjacent karts
- Boost pickup: speed burst for 2 seconds
- First to 3 laps wins; if time runs out, most laps wins

**Phone Controller:**
```
[STEER JOYSTICK] [BUMP]
```

**3D Design Spec:**
- URP stylized kart: player color livery, oversized wheels, spring suspension
- Track: looping candy-stripe road with painted lane markings
- Boost strip: glowing arrow decal + motion blur VFX on pickup
- Camera: Cinemachine follow cam with cinematic FOV crush on boost

---

### 🎯 Mini-Game 6: BLINK SHOT
**Verb:** Aiming + Timing  
**Objective:** Hit the most targets before time runs out  
**Duration:** 60 seconds  
**Players:** 2–8

**Arena:** Retro carnival shooting gallery floating in a starfield. Targets pop up in waves. Giant neon scoreboard tower in the center. Targets are goofy mascot silhouettes.

**Mechanics:**
- Slide thumb on phone screen to aim reticle on TV
- Tap to fire (ammo unlimited but has 0.5s cooldown)
- Targets move and appear in sequences — harder targets worth more
- "Golden Target" worth 5× points appears randomly for 1.5 seconds

**Phone Controller:**
```
[AIM PAD (full screen drag)] [FIRE tap]
```

**3D Design Spec:**
- Targets: stylized pop-up silhouettes with hit reaction shatter VFX
- Aim indicator: player-colored laser dot on TV with subtle lag for fun challenge
- Scoreboard tower: dynamic number flip animation per player
- Star/neon environment: bloom post-processing, emissive materials

---

### 🔮 Mini-Game 7: GRAVITY GRAB
**Verb:** Puzzle + Quick Thinking  
**Objective:** Grab falling objects matching your assigned color — wrong color = penalty  
**Duration:** 60 seconds  
**Players:** 2–8

**Arena:** Zero-gravity space station interior. Colored orbs and junk float down in chaotic patterns. Mission control mascot calls out bonus objectives.

**Mechanics:**
- Swipe left/right on phone to move character
- Tap to grab orb when in range
- Right color = +1 point, wrong color = -1 point, "Rainbow Orb" = +5
- Speed increases every 15 seconds

**Phone Controller:**
```
[← SWIPE →] [GRAB tap]
```

**3D Design Spec:**
- Zero-G character movement: floating with inertia, gentle rotation
- Orbs: emissive glass material with player-color inner glow
- Space station: modular segment design, URP metallic/roughness, floating debris
- Gravity lines: particle ribbon trails on all falling objects

---

## 6. TV-as-Server Architecture

### Authority Model
```
┌─────────────────────────────────────────────┐
│              TV HOST (Unity)                 │
│  ┌──────────────┐  ┌──────────────────────┐ │
│  │SessionState  │  │  MiniGame Simulation  │ │
│  │Manager       │  │  (authoritative)      │ │
│  └──────────────┘  └──────────────────────┘ │
│  ┌──────────────┐  ┌──────────────────────┐ │
│  │PlayerRegistry│  │  ResultsAggregator   │ │
│  └──────────────┘  └──────────────────────┘ │
│  ┌────────────────────────────────────────┐ │
│  │     ControllerGateway (WebSocket)      │ │
│  └────────────────────────────────────────┘ │
└─────────────────────────────────────────────┘
              ▲ ▼  WebSocket messages
    ┌─────────┴─────────┐
    │  PHONE CLIENTS     │
    │  (web controller)  │
    │  Send intent only: │
    │  move / tap / dash │
    │  aim / vote / ready│
    └────────────────────┘
```

### Core System Modules

```csharp
// SessionStateManager.cs
public class SessionStateManager : MonoBehaviour
{
    public static SessionStateManager Instance;
    
    public string RoomToken { get; private set; }
    public LobbyState CurrentLobbyState { get; private set; }
    public List<PlayerData> ConnectedPlayers { get; private set; }
    
    public enum LobbyState
    {
        WaitingForPlayers,
        CharacterSelect,
        ReadyCheck,
        Countdown,
        InGame,
        Results,
        Podium,
        ReconnectGrace
    }
    
    public string GenerateRoomToken() 
    { 
        return System.Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper();
    }
}

// PlayerData.cs
[System.Serializable]
public class PlayerData
{
    public string playerId;        // Assigned on join
    public string nickname;
    public string heroId;          // From CharacterDefinition
    public Color playerColor;
    public int playerSlot;         // 0-7
    public bool isReady;
    public bool isConnected;
    public int sessionScore;
    public int placement;
    public string reconnectToken;
}

// MiniGameDefinition.cs
[CreateAssetMenu(fileName = "MiniGameDef", menuName = "Moments/MiniGameDefinition")]
public class MiniGameDefinition : ScriptableObject
{
    public string gameId;
    public string displayName;
    public string rulesLine;       // Short description for rules card
    public Sprite thumbnail;
    public int durationSeconds;
    public int maxPlayers;
    public ControllerLayout controllerLayout;
    public string sceneAddress;    // Addressables key
    public ScoringMode scoringMode;
    public AudioClip signatureLoop;
}
```

---

## 7. QR Join Flow — Technical Spec

### URL Structure
```
https://moments.game/join/{ROOM_TOKEN}?slot={PLAYER_SLOT}&session={SESSION_ID}
```

### Join Sequence
```
TV generates room token → encodes QR → displays on attract screen
Player scans QR → phone opens /join/{TOKEN}
Server validates token → assigns player slot → opens WebSocket connection
Phone sends: { type: "join", nickname: "...", heroHover: "byte" }
TV broadcasts: { type: "playerJoined", slot: 2, nickname: "...", color: "#00BFFF" }
Phone sends: { type: "heroLock", heroId: "nova" }
TV validates (not taken) → broadcasts: { type: "heroLocked", slot: 2, heroId: "nova" }
Phone sends: { type: "ready" }
TV broadcasts: { type: "playerReady", slot: 2 }
All ready → TV broadcasts: { type: "startCountdown", seconds: 3 }
```

### Reconnect Flow
```
If disconnect detected → TV marks slot as "reconnecting" (15s grace window)
Phone opens same QR URL → server detects matching reconnect token
Server restores slot, score, and character → broadcasts reconnect event
```

---

## 8. Phone Controller Layouts Per Mini-Game

| Mini-Game | Left Zone | Right Zone | Special |
|-----------|-----------|------------|---------|
| Polar Push | Move Joystick | Dash Button | - |
| Color Clash | Move Joystick | - | - |
| Tank Battle | Drive Joystick | Aim Joystick | Fire Button |
| Wave Rider | Tilt Balance | Duck Tap | - |
| Bumper Blitz | Steer Joystick | Bump Button | - |
| Blink Shot | Full-screen aim drag | Fire Tap | - |
| Gravity Grab | Swipe Left/Right | Grab Tap | - |

**Controller Shell Rules:**
- Background: black (#0A0A0A) — battery-safe OLED
- Player color frame: 8px border around full screen
- Player avatar: top center, 48×48px
- Font: 32px minimum for any visible text
- Haptic pulses: join, ready, hit, pickup, eliminate, podium

---

## 9. Art Direction — 3D Style Guide

### Visual Target: Premium Toy-Like 3D

**Inspired by:** Crash Bash's chaotic readability + modern toy-like 3D (think Stumble Guys meets Sackboy)

### Character Art Rules
1. **Proportion:** Head = 40% of total height, limbs short and rounded
2. **Silhouette:** Every hero readable as a distinct shape from 5m away
3. **Materials:** URP Lit shader, roughness 0.3–0.6, metallic 0–0.3, clean gradient lighting
4. **Rim lighting:** Soft warm rim to separate from background
5. **Turnaround:** Front / 3/4 / Side / Back required before modeling
6. **Rig:** One shared skeleton: 25 bones max, compatible with all mini-game animations

### Arena Art Rules
1. **Color Script:** Every arena has ONE dominant hue + ONE accent + white/neutral
   - Polar Push: Icy blue (#A8D8EA) + white
   - Color Clash: Multi-color tiles on cloud-white platform
   - Tank Battle: Sandy tan (#E8C887) + terracotta
   - Wave Rider: Ocean turquoise (#00CED1) + sunset orange
   - Bumper Blitz: Candy red + candy yellow
   - Blink Shot: Deep navy (#0D0D2B) + neon pink
   - Gravity Grab: Space black + electric purple
2. **Scale:** Each arena fits a 30m × 30m play area max
3. **Set piece:** Every arena has ONE signature animated element (cracking ice, confetti cannon, spinning wheel, waves, etc.)
4. **VFX library (shared):** hit spark, explosion ring, tile paint splash, dash trail, score burst, elimination flash, podium confetti

### URP Shader Specs
```
Character shader:
  - Base: Lit/Simple Lit
  - Smoothness: 0.45 average
  - Normal maps: 0.5 strength
  - Emission: 0 (emotes trigger glow via script)
  - Rim: custom shader graph, player color tint

Ice shader (Polar Push):
  - Base + subsurface scattering approximation
  - Refraction layer via distortion
  - Crack decal projector system
  - Albedo: ice texture + procedural voronoi overlay

Water shader (Wave Rider):
  - Scrolling normal maps (2 layers, 45° offset)
  - Foam mask at wave crests
  - Depth fog tint
  - Vertex displacement: sine wave + Perlin noise
```

---

## 10. Audio Design

### Announcer System
- Male/female announcer voice options (select in host settings)
- Lines needed: join confirm, ready, countdown 3-2-1-GO, win, lose, tie, podium
- Stingers per mini-game (5-second intro hit)

### Music Structure
- **Attract loop:** Upbeat 120BPM, 4/4, repeating 32-bar loop
- **Lobby loop:** Lighter variation, less percussion
- **Per mini-game signature loop:** 60–90 second looping track, distinct per arena
- **Results sting:** 5-second fanfare
- **Podium theme:** 30-second celebration piece

### SFX Library
| Event | Sound |
|-------|-------|
| Player joins | Ascending chime |
| Character lock | Satisfying click + pop |
| Ready up | Friendly bell + TV flash |
| Game start | Orchestral hit + crowd cheer |
| Player hit | Rubber boing + impact bass |
| Elimination | Descending whistle |
| Win | Fanfare burst |
| Lose | Sad trombone riff |
| Podium | Crowd roar + trophy clang |

---

## 11. Production Roadmap

### Milestone 1 — Vertical Slice (6 weeks)
- ✅ TV lobby + QR join + phone web controller
- ✅ Character select (8 heroes, placeholder models)
- ✅ 1 polished mini-game: **Polar Push**
- ✅ Score screen + reconnect flow
- ✅ Art style proof: 1 hero (Byte) + 1 arena fully textured

### Milestone 2 — Core Alpha (10 weeks)
- 4 mini-games: Polar Push, Color Clash, Tank Battle, Wave Rider
- All 8 hero models + rigs + portraits
- Session playlist (random + vote)
- Analytics hooks
- Party settings (duplicates, round count, elimination mode)

### Milestone 3 — Content Beta (8 weeks)
- 7 mini-games (add Bumper Blitz, Blink Shot, Gravity Grab)
- Full audio pass (announcer, music, SFX)
- Balancing + playtesting pass
- Accessibility (subtitles, colorblind palette option)
- Localization (EN, ES, FR, PT, ZH)

### Milestone 4 — Launch (4 weeks)
- Performance optimization (60fps TV, 60fps phone at 1080p)
- Device QA matrix (iOS/Android browsers, Smart TVs, PC)
- Marketing capture + trailer
- Storefront assets
- Live ops hooks (seasonal skins, event mini-games)

---

## 12. First Build Scope

**3 Mini-Games to Prove Everything:**

| Mini-Game | Proves |
|-----------|--------|
| Polar Push | Movement + dash + physics knockback |
| Color Clash | Territory capture + TV floor readability |
| Tank Battle | Steering + aim + twin-stick combat |

**Week 1 Deliverables:**
1. Unity 6 project with URP configured
2. WebSocket server running on TV host
3. Phone web controller (HTML/JS) serving /join/{TOKEN}
4. QR code generation on TV attract screen
5. Player join → character select → ready state sync (placeholder art ok)
6. Polar Push: playable prototype, one arena, physics movement, 2-player test

---

*Document generated by Noor | Claude Code Game Studios | Moments v2.0*
