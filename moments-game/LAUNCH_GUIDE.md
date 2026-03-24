# 🎮 How to Launch Moments — Beginner's Guide

This guide walks you from zero to a running game, step by step.
No prior Unity experience needed.

---

## What You Need First

| Tool | Where to Get It | Notes |
|------|----------------|-------|
| **Unity Hub** | unityhub.com/download | Free download, installer |
| **Unity 6 LTS** | Installed through Unity Hub | Version `6000.0.x` |
| A computer + **TV/monitor** | — | TV is the game screen |
| A **phone** on the same WiFi | — | This is the game controller |

---

## Step 1 — Install Unity Hub & Unity 6

1. Download and install **Unity Hub** from [unityhub.com](https://unityhub.com/download)
2. Open Unity Hub → click **Installs** in the left sidebar
3. Click **Install Editor** → find **Unity 6 (LTS)** → click Install
4. When it asks about modules, add:
   - ✅ **Windows Build Support** (or Mac, depending on your OS)
   - ✅ **Android Build Support** (if you want to build a TV APK later)
   - Everything else is optional for now
5. Wait for installation to finish (can take 10–20 min)

---

## Step 2 — Open the Project

1. In Unity Hub → click **Projects** → **Add** → **Add project from disk**
2. Navigate to this folder:
   ```
   moments-game/UnityProject/
   ```
3. Click **Add Project**
4. It should show **Unity 6** next to the project — click to open
5. **First open takes 3–10 minutes** — Unity is importing all assets. Normal.

> ⚠️ If Unity asks "Do you want to upgrade the project?" → click **Confirm**

---

## Step 3 — Fix the Graphics (URP Setup)

When the project opens, materials might look **pink/magenta** — that's normal and easy to fix:

1. In Unity's top menu → **Edit → Project Settings**
2. Click **Graphics** in the left list
3. Look for **Scriptable Render Pipeline Settings** — if it says "None":
   - Click the small circle/dot next to it
   - Search for `MomentsURPPipelineAsset` and select it
4. Close Project Settings
5. Materials should now look correct (colored, not pink)

---

## Step 4 — Set Up the Scene List

1. Top menu → **File → Build Settings**
2. You'll see a list called "Scenes In Build" — it may be empty
3. Click **Add Open Scenes** won't work here — instead, drag scenes manually:
   - In the **Project panel** (bottom of screen), navigate to `Assets/Scenes/`
   - Drag these scenes into the "Scenes In Build" list **in this order**:

   | Drag Order | Scene File |
   |-----------|-----------|
   | 1st | `Assets/Scenes/Bootstrap.unity` |
   | 2nd | `Assets/Scenes/Attract.unity` |
   | 3rd | `Assets/Scenes/Lobby.unity` |
   | 4th | `Assets/Scenes/Results.unity` |
   | 5th | `Assets/Scenes/Podium.unity` |
   | 6th | `Assets/Scenes/MiniGames/PolarPush.unity` |
   | 7th | `Assets/Scenes/MiniGames/ColorClash.unity` |
   | 8th | `Assets/Scenes/MiniGames/TankBattle.unity` |
   | 9th | `Assets/Scenes/MiniGames/WaveRider.unity` |
   | 10th | `Assets/Scenes/MiniGames/BumperBlitz.unity` |
   | 11th | `Assets/Scenes/MiniGames/BlinkShot.unity` |
   | 12th | `Assets/Scenes/MiniGames/GravityGrab.unity` |

4. Click **Close**

---

## Step 5 — Open the Bootstrap Scene

This is the scene that starts the game:

1. In the **Project panel**, navigate to `Assets/Scenes/`
2. Double-click **Bootstrap.unity** to open it
3. You should see a mostly-empty scene in the Hierarchy panel on the left

---

## Step 6 — Press Play ▶

1. At the top of the Unity window, press the **▶ Play button**
2. The game will start in the **Game view** (a tab near the top)
3. It will show the **Attract screen** — a title card cycling through colors

> If you get errors in red at the bottom — paste them and I'll fix them.

---

## Step 7 — Connect Your Phone

Once the attract screen is running:

1. Find your computer's **local IP address**:
   - **Mac/Linux:** Open Terminal → type `ifconfig | grep "inet "` → look for something like `192.168.1.x`
   - **Windows:** Open Command Prompt → type `ipconfig` → look for **IPv4 Address**

2. On your phone's browser, go to:
   ```
   http://[YOUR-IP]:8080
   ```
   Example: `http://192.168.1.42:8080`

3. The phone controller page loads — enter a nickname, pick a hero, tap **Join**

4. The TV (Unity Game view) should show your player appearing in the lobby

---

## Step 8 — Start a Game

1. Once 1–8 players have joined on their phones
2. One player taps **Ready** — when all are ready, the countdown starts
3. The first mini-game loads automatically (**Polar Push** by default)
4. Players use their phone as a joystick — tilt/drag to move, buttons to dash

---

## 🔥 Quick Troubleshooting

| Problem | Fix |
|---------|-----|
| **Pink/magenta materials** | Step 3 — assign the URP asset in Graphics Settings |
| **Console errors on Play** | Scroll to the first red error, paste it here |
| **Phone can't reach the game** | Make sure phone + computer are on the **same WiFi network** |
| **Port 8080 already in use** | In Unity, find `PhoneControllerServer.cs` → change `httpPort = 8080` to `8081` |
| **QR code shows checkerboard** | Normal for now — just type the URL manually on the phone |
| **Black screen on Play** | Make sure Bootstrap.unity is the open scene when you press Play |
| **"DLL not found" error** | Restart Unity, then try Play again |

---

## 📁 What the Key Files Do (FYI)

```
moments-game/
└── UnityProject/
    └── Assets/
        ├── Scenes/
        │   ├── Bootstrap.unity     ← START HERE — opens first, always running
        │   ├── Attract.unity       ← Title screen with QR code
        │   ├── Lobby.unity         ← Players join here
        │   ├── Results.unity       ← Scoreboard after each game
        │   ├── Podium.unity        ← Final winner celebration
        │   └── MiniGames/          ← The 7 actual game arenas
        ├── Scripts/
        │   ├── Core/               ← Session, scores, player data
        │   ├── Gameplay/           ← Each mini-game's logic
        │   ├── Networking/         ← WebSocket server, phone communication
        │   └── UI/                 ← Lobby/Results/Podium screens
        └── StreamingAssets/
            └── phone-controller.html  ← The phone browser controller
```

---

## 🎯 The 3-Minute Launch Summary

```
1. Unity Hub → Open Project → moments-game/UnityProject/
2. Edit → Project Settings → Graphics → assign MomentsURPPipelineAsset
3. File → Build Settings → drag all 12 scenes in order
4. Double-click Bootstrap.unity
5. Press ▶ Play
6. Phone browser → http://[your-ip]:8080
7. Join and play
```

---

*Moments v0.1 — if anything breaks, paste the red error message and it'll be fixed.*
