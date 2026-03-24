# MOMENTS — Unity Project Setup Guide
## For developers opening this project for the first time

---

## Step 1: Open the Project

1. Open **Unity Hub**
2. Click **Add → Add project from disk**
3. Navigate to: `moments-game/UnityProject/`
4. Select the folder and open with **Unity 6 (6000.0.x LTS)**

> ⚠️ Unity will import packages on first open (may take 2–5 minutes)

---

## Step 2: Configure URP

After Unity finishes importing:

1. Go to **Edit → Project Settings → Graphics**
2. Click the **Scriptable Render Pipeline Settings** field
3. Assign: `Assets/Settings/MomentsURPPipelineAsset`
4. Go to **Edit → Project Settings → Quality**
5. For each quality level, assign the same URP asset

Alternatively run: **Window → Rendering → Render Pipeline Converter** → Convert Built-in to URP

---

## Step 3: Set Up Scenes in Build Settings

Go to **File → Build Settings → Add Open Scenes** and add in this order:

| # | Scene | Path |
|---|-------|------|
| 0 | Bootstrap | `Assets/Scenes/Bootstrap.unity` |
| 1 | Attract | `Assets/Scenes/Attract.unity` |
| 2 | Lobby | `Assets/Scenes/Lobby.unity` |
| 3 | Results | `Assets/Scenes/Results.unity` |
| 4 | Podium | `Assets/Scenes/Podium.unity` |
| 5 | PolarPush | `Assets/Scenes/MiniGames/PolarPush.unity` |
| 6 | ColorClash | `Assets/Scenes/MiniGames/ColorClash.unity` |
| 7 | TankBattle | `Assets/Scenes/MiniGames/TankBattle.unity` |

> Scene files need to be created — see Step 5.

---

## Step 4: Configure Addressables

1. Open **Window → Asset Management → Addressables → Groups**
2. Create groups:
   - `Shell-Scenes` → Add: Attract, Lobby, Results, Podium scenes
   - `MiniGame-PolarPush` → Add: PolarPush scene
   - `MiniGame-ColorClash` → Add: ColorClash scene  
   - `MiniGame-TankBattle` → Add: TankBattle scene
   - `Characters` → Add: all CharacterDefinition assets
   - `MiniGameDefs` → Add: all MiniGameDefinition assets
3. Set Addressable keys to match `MiniGameDefinition.sceneAddress` values:
   - `"Scenes/PolarPush"`, `"Scenes/ColorClash"`, `"Scenes/TankBattle"`

---

## Step 5: Create Scenes

### Bootstrap.unity
Create a new scene. Add an empty GameObject named `[Bootstrap Root]` with:
- `BootstrapController` component
- `MainThreadFlusher` component

Add child GameObjects:
- `SessionStateManager` → add `SessionStateManager.cs`
- `WebSocketServer` → add `MomentsWebSocketServer.cs`
- `ControllerGateway` → add `ControllerGateway.cs`
- `PhoneServer` → add `PhoneControllerServer.cs`
- `MiniGameLoader` → add `MiniGameLoader.cs`
- `ResultsAggregator` → add `ResultsAggregator.cs`

Remove the default Main Camera (Bootstrap has no camera).

### Lobby.unity
Create a new scene with:
- Canvas (Screen Space - Overlay, 1920×1080 reference)
  - QR Code panel (top-right corner, 200×200px RawImage)
  - Player cards grid (center)
  - Room code text
  - Start button
- Add `LobbySceneController` to root
- Add `QRCodeDisplay` to QR code panel

### PolarPush.unity (Mini-game arena)
- Hex platform (use `IcePlatformManager` to generate at runtime)
- Directional Light (cold, 6500K)
- Main Camera at 30° angle looking down at platform
- Cinemachine Virtual Camera for cinematic moments
- Add `PolarPushGame` component to a root GameManager object

---

## Step 6: Install ZXing.Net for QR Codes

Option A (recommended): Via NuGet for Unity
1. Install `NuGetForUnity` package
2. Search for `ZXing.Net` and install

Option B: Manual DLL
1. Download `ZXing.Net.0.16.x.zip` from GitHub
2. Extract `zxing.unity.dll`
3. Place in `Assets/Plugins/`
4. Add `#define ZXING_AVAILABLE` to `QRCodeDisplay.cs`

---

## Step 7: Test the Full Loop

1. Press **Play** in Unity
2. Open a browser on the same network to `http://[your-ip]:8080`
3. Enter a nickname and pick a hero
4. TV should show the player joining in the lobby
5. Press Start → Polar Push should load

### Checking your local IP
```bash
# macOS/Linux
ifconfig | grep "inet " | grep -v 127.0.0.1

# Windows
ipconfig | findstr "IPv4"
```

---

## Project Structure

```
UnityProject/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/           ← SessionStateManager, PlayerData, etc.
│   │   ├── Gameplay/       ← MiniGameBase, PolarPushGame, etc.
│   │   ├── Networking/     ← WebSocket server, HTTP server, ControllerGateway
│   │   ├── UI/             ← Lobby, Results, Podium controllers
│   │   └── Audio/          ← (to be added)
│   ├── Scenes/
│   │   ├── Bootstrap.unity
│   │   ├── Attract.unity
│   │   ├── Lobby.unity
│   │   ├── Results.unity
│   │   ├── Podium.unity
│   │   └── MiniGames/
│   │       ├── PolarPush.unity
│   │       ├── ColorClash.unity
│   │       └── TankBattle.unity
│   ├── StreamingAssets/
│   │   └── phone-controller.html    ← Served to phone browsers
│   ├── ScriptableObjects/
│   │   ├── Characters/     ← CharacterDefinition assets (×8)
│   │   └── MiniGames/      ← MiniGameDefinition assets (×7)
│   ├── Settings/
│   │   └── MomentsURPPipelineAsset.asset
│   └── Prefabs/
│       ├── Characters/
│       ├── Arena/
│       └── UI/
├── Packages/
│   └── manifest.json
└── ProjectSettings/
    ├── ProjectSettings.asset
    ├── QualitySettings.asset
    └── GraphicsSettings.asset
```

---

## Common Issues

| Issue | Fix |
|-------|-----|
| `SessionStateManager` null errors | Ensure Bootstrap scene is index 0 in Build Settings |
| WebSocket port 8765 in use | Change `wsPort` in `MomentsWebSocketServer.cs` |
| Phone can't reach TV | Both must be on same WiFi network; check firewall allows port 8080/8765 |
| QR code shows checkerboard | ZXing.Net not installed — see Step 6 |
| URP pink materials | Assign URP asset in Graphics Settings — see Step 2 |

---

*Moments v0.1 — Sprint 1 Vertical Slice Target*
