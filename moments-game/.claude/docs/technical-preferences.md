# Technical Preferences

<!-- Configured by: CCGS /setup-engine (Unity 6 + URP) -->
<!-- Last updated: 2026-03-25 -->
<!-- All agents reference this file for project-specific standards and conventions. -->

## Engine & Language

- **Engine**: Unity 6 (6000.0.x LTS)
- **Language**: C# 10 (.NET Standard 2.1)
- **Rendering**: Universal Render Pipeline (URP) — Forward+ renderer
- **Physics**: Unity Physics (Rigidbody-based), no DOTS physics for launch
- **Asset Loading**: Unity Addressables 2.x (mini-game scenes + character prefabs)
- **Camera**: Cinemachine 3.x (TV camera rigs per arena)
- **Localization**: Unity Localization package (EN/ES/FR/PT/ZH)
- **Networking**: WebSocketSharp (TV WebSocket server) + browser WebSocket (phone clients)
- **UI Framework**: Unity UI (uGUI) for TV HUD; plain HTML/JS for phone controller

## Naming Conventions

- **Classes**: PascalCase (`SessionStateManager`, `PolarPushGame`)
- **Private fields**: camelCase with underscore prefix (`_players`, `_dashCooldown`)
- **Public properties**: PascalCase (`CurrentState`, `RoomToken`)
- **Events**: PascalCase starting with "On" (`OnPlayerJoined`, `OnRoundComplete`)
- **ScriptableObjects**: Suffix with type (`CharacterDefinition`, `MiniGameDefinition`)
- **Scene files**: kebab-case matching scene purpose (`lobby.unity`, `polar-push.unity`)
- **Prefabs**: PascalCase matching script (`PlayerController.prefab`, `CannonBall.prefab`)
- **Constants**: SCREAMING_SNAKE_CASE (`MAX_PLAYERS`, `DASH_FORCE`)

## Performance Budgets

- **Target Framerate**: 60fps on TV host (PC/TV), 60fps on phone browser
- **Frame Budget**: 16.7ms total (TV host Unity player)
- **Draw Calls**: ≤ 150 per frame in gameplay (URP batching + GPU instancing)
- **Memory Ceiling**: 2GB RAM on TV host, 100MB on phone browser
- **Texture Budget**: 512×512 WebP for phone portrait assets, 1024×1024 for TV
- **Audio**: Maximum 16 simultaneous audio sources

## Path-Scoped Standards (sync with .claude/rules/)

- `src/gameplay/**` — Data-driven values (ScriptableObject refs), delta time, no UI refs
- `src/core/**` — Zero hot-path GC allocations, thread safety, stable public API
- `src/networking/**` — Server-authoritative only; phones send intent, never state
- `src/ui/**` — No game state ownership; subscribe to events only

## Testing

- **Framework**: Unity Test Framework (Edit Mode + Play Mode)
- **Minimum Coverage**: 70% for src/core/, 50% for src/gameplay/
- **Required Tests**: SessionStateManager state machine, ControllerGateway message parsing, MiniGameBase lifecycle, scoring calculations

## Forbidden Patterns

- `GameObject.Find()` in gameplay hot paths (use injected references)
- Hard-coded player colors or hero IDs (always reference CharacterDefinition assets)
- Phone clients owning or resolving game state (TV is always authoritative)
- `UnityEngine.Random` in networked game code (use seeded deterministic RNG)
- Blocking main thread for network I/O (all WebSocket on background thread)

## Allowed Libraries / Addons

- **WebSocketSharp** (or `websocket-sharp-unity`) — TV WebSocket server
- **Unity Addressables 2.x** — scene + asset streaming
- **Cinemachine 3.x** — TV camera rigs
- **Unity Localization** — multilingual prompts
- **ZXing.Net** (or equivalent) — QR code generation on TV attract screen
- **DOTween** (optional) — UI animation library for TV results screens

## Architecture Decisions Log

- ADR-001: TV-as-server authority model (see docs/architecture/ADR-001-tv-authority.md)
- ADR-002: WebSocket transport over Mirror/Netcode (see docs/architecture/ADR-002-websocket.md)
- ADR-003: Addressables for mini-game streaming (see docs/architecture/ADR-003-addressables.md)
