# MOMENTS — Art Direction Bible v1.0
## 3D Visual Style Guide | Unity 6 URP

---

## Visual Identity

**Target:** Premium toy-like 3D party game  
**References:** Crash Bash (PS1 energy) + Stumble Guys (modern accessibility) + Sackboy (material quality)  
**Core feeling:** Vibrant, rounded, readable, celebratory

---

## Character Style System

### Proportion Rules
```
Head:   40% of total body height
        Eyes: 35% of head height, wide-set
        Mouth: expressive, exaggerated shapes for win/lose

Torso:  Short and barrel-shaped
        Arms: stubby, large hands (30% of arm length)
        Legs: short, strong thighs, small feet

Scale:  Characters are ~1.5m tall in Unity units
        Heads are oversized relative to realistic anatomy
```

### Silhouette Test
Every hero must be immediately identifiable as a black silhouette at 512×512px:
- Byte: game controller backpack jutting from shoulder
- Nova: oversized holographic goggles on forehead
- Orbit: round helmet with antenna
- Striker: ball chained to belt
- Sizzle: enormous chef hat
- Shade: ninja wrap + smoke trails
- Dusty: wide cowboy hat brim
- Pop: rollerskates + headphone halo

### Material Spec (URP Lit)
```
Surface:    Lit (Simple Lit for background characters)
Smoothness: 0.35 – 0.55 (soft, toy-like, not plastic-shiny)
Metallic:   0.0 – 0.15 (minor only on props like Orbit's helmet)
Normal:     Subtle cloth/skin normals, 0.4 strength
Emission:   Off by default; triggered on emote/win (glow pulse)
Rim light:  Custom shader graph, player-color tinted, 0.6 intensity
```

### Color Per Hero
| Hero    | Primary         | Accent          | Skin Tone       |
|---------|-----------------|-----------------|-----------------|
| Byte    | #00BFFF (cyan)  | #0044FF (blue)  | Warm medium     |
| Nova    | #FFD700 (gold)  | #FF8C00 (amber) | Light warm      |
| Orbit   | #00FFFF (cyan)  | #FFFFFF (white) | Cool light      |
| Striker | #7FFF00 (lime)  | #006400 (dark)  | Deep warm       |
| Sizzle  | #FF6B00 (fire)  | #FF2200 (red)   | Medium warm     |
| Shade   | #9B59B6 (purple)| #2C0066 (dark)  | Cool medium     |
| Dusty   | #D2691E (brown) | #8B4513 (saddle)| Warm tan        |
| Pop     | #FF1493 (pink)  | #FF69B4 (light) | Light neutral   |

### Rig Specification (shared across all 8 heroes)
```
Total bones: 25 maximum
Spine:      Root → Hips → Spine01 → Spine02 → Neck → Head
Arms:       Clavicle → UpperArm → ForeArm → Hand → 3 fingers (no ring/pinky separate)
Legs:       UpperLeg → LowerLeg → Foot → Toe
Prop bone:  Extra bone per hero for signature prop (backpack, hat, etc.)
```

### Required Animation States (shared vocabulary)
- Idle (looping, gentle weight shift)
- Walk (blended with joystick input)
- Run (fast version)
- Dash (burst forward, anticipation + recovery)
- Jump (if mini-game requires)
- Hit (ragdoll-adjacent, impact direction-aware)
- Eliminated (fall + bounce + ragdoll settle)
- Win (hero-specific, 4-second loop)
- Lose (head down, shuffling)
- Emote A/B/C (hero-specific personality)

---

## Arena Style System

### Shared Rules
1. Every arena is a "playable diorama" — self-contained 3D stage with clear viewport from TV camera
2. One dominant color + one accent + white/neutral
3. One signature animated set piece per arena
4. Play area: 30m × 30m maximum
5. TV camera: fixed or Cinemachine dolly with subtle drift (no motion sickness)
6. All read-critical elements (tiles, hazards, powerups) must have icon/color double-coding

---

## Arena Design Sheets

### 🏔️ POLAR PUSH
```
Dominant hue:    Ice blue (#A8D8EA)
Accent:          White/snow (#F0F8FF)
Platform:        Hexagonal ice geometry, 20m diameter
Background:      Starfield night sky with animated aurora borealis
Set piece:       Edge tiles crack and fall procedurally (rigid body)
Mascot:          Polar bear referee on floating ice chunk to the side
Lighting:        Cold directional light (6500K), soft blue fill, 
                 warm rim from aurora
Ice shader:      Subsurface scattering approx, voronoi crack overlay,
                 refraction layer via URP distortion feature
VFX needed:      Ice crack spawn, tile fall dissolve, splash impact,
                 dash trail (ice particles), elimination splash
Camera:          Slightly above (30° angle), zooms out as players spread
```

### 🎨 COLOR CLASH
```
Dominant hue:    Varies per player (multi-color floor)
Platform:        8×8 tile grid floating above cloud layer
Background:      Bright blue sky, big fluffy scrolling clouds
Set piece:       Confetti cannons at each corner fire when a player takes lead
Lighting:        Bright daylight (5500K), strong top light for tile readability
Tile material:   URP Lit with color lerp animation (0.1s transition per paint)
VFX needed:      Paint splat (radial, inherits player color), 
                 paint bomb explosion, tile ownership trails
Camera:          Directly above (top-down, 90° or 80°), slight orbital during wait
```

### ⚔️ TANK BATTLE
```
Dominant hue:    Sandy tan (#E8C887)
Accent:          Terracotta (#C4642A), dusty green cacti
Arena:           Desert canyon, 30×30m play area, low cover blocks
Background:      Sunset sky (orange-red gradient), distant mesas
Set piece:       Dust devil spawns at random intervals revealing power-up crates
Lighting:        Warm directional (3200K sunset), strong shadow contrast
Cover:           Modular destructible blocks with URP dissolve on impact
Tank model:      Chunky toy proportions, player color on hull, rubber wheels
Cannonball:      Smooth sphere with ribbon trail + impact crater decal
VFX needed:      Dust trail on tank movement, cannonball ribbon trail,
                 explosion ring (sand burst), cover dissolve, barrel smoke
Camera:          Isometric 45°, static with subtle shake on explosions
```

### 🌊 WAVE RIDER
```
Dominant hue:    Ocean turquoise (#00CED1)
Accent:          Sunset orange (#FF7F50), white wave crests
Arena:           Open ocean, infinite-scrolling wave geometry
Background:      Tropical island visible in distance, colorful sunset
Set piece:       Waves increase in height every 15 seconds (visual escalation)
Lighting:        Warm sunset (3800K), sparkle highlights on water
Water shader:    2-layer scrolling normal maps (45° offset), foam at crests,
                 depth tint, vertex displacement via sine + Perlin noise
Surfboard:       Character ragdoll with balance spring constraint + board physics
Obstacles:       Coconut projectiles on bezier spawn paths
VFX needed:      Splash on fall, wave foam particle, board wipe trail,
                 elimination splash, crowd reaction on shore
Camera:          Side-scrolling view, slight push-in as waves grow
```

### 🏎️ BUMPER BLITZ
```
Dominant hue:    Candy red (#FF2244)
Accent:          Candy yellow (#FFD600), white lane markings
Track:           Circular looping kart track, pastel candy-stripe road
Center feature:  Giant spinning candy wheel decoration (non-interactive)
Set piece:       Finish line confetti explosion on each lap completion
Lighting:        Festival bright (5000K), rim lights on karts = player color
Kart model:      Oversized toy kart, spring suspension, player color livery
VFX needed:      Boost trail, bump impact ring, confetti on lap,
                 wheel smoke on drift, boost pickup glow pulse
Camera:          Cinemachine follow cam per player? No — TV shows all.
                 Wide-angle follow cam showing most players + track
```

### 🎯 BLINK SHOT
```
Dominant hue:    Deep navy (#0D0D2B)
Accent:          Neon pink (#FF2D78), neon yellow (#FFE600)
Arena:           Retro carnival shooting gallery, floating in starfield
Targets:         Pop-up goofy mascot silhouettes on spring arms
Set piece:       Giant neon scoreboard tower at center, numbers flip live
Lighting:        Dark scene, heavy bloom on neons, emissive materials
VFX needed:      Target shatter particles (colored per target type),
                 golden target glow pulse, miss smoke puff, score burst
Camera:          Fixed front-facing, gallery arranged in 3 rows at distance
```

### 🔮 GRAVITY GRAB
```
Dominant hue:    Space black (#0A0008)
Accent:          Electric purple (#8A00FF), cyan (#00FFF5)
Arena:           Zero-gravity space station interior, modular segments
Orbs:            Emissive glass spheres, color-coded per player color + rainbow
Set piece:       Mission control mascot in window, reacts to play events
Lighting:        Point lights from orbs, cool fill, dramatic shadows
Zero-G movement: Character floats with inertia, gentle rotation on input
VFX needed:      Orb trail ribbons, grab burst, wrong-color flash (red),
                 rainbow orb sparkle trail, speed-up visual pulse effect
Camera:          Wide-angle interior view, slight dutch angle for space feel
```

---

## VFX Library (Shared Across All Games)

| Effect | Description | Priority |
|--------|-------------|----------|
| Hit spark | Player-color burst on collision | P0 |
| Elimination flash | Screen-edge red flash + character dissolve | P0 |
| Dash trail | Particle ribbon inheriting player color | P0 |
| Score burst | +N number pops up and floats | P0 |
| Podium confetti | Layered paper + star particles | P0 |
| Join flash | Green ring on TV lobby card | P1 |
| Ready flash | Animated ring completion on lobby card | P1 |
| Tile paint splash | Radial paint explosion (Color Clash) | P1 |
| Ice crack | Fracture decal + physics tile fall | P1 |
| Explosion ring | Shockwave disk + debris burst | P1 |
| Power-up glow | Pulsing emissive pickup aura | P1 |

---

## TV HUD Spec

### Typography Scale (all sizes reference 1920×1080 TV)
```
Player names:     48px, Bold, player color
Score numbers:    72px, ExtraBold, white
Timer:            96px, Black, white (center top)
Announcements:    64px, ExtraBold, animated scale pop
Rules card:       40px, SemiBold, dark overlay
Mini subtitle:    28px, Regular, 70% opacity
```

### Color Rules
- Never use pure black text on dark backgrounds
- Score increasing: flash white → player color
- Score decreasing: flash red
- Timer < 10 seconds: turn red + scale pulse
- Eliminated player card: greyscale + skull icon

---

## Phone Controller UI Spec

### Battery-Safe Shell
- Background: `#0A0A0A` (OLED black, lowest power)
- Player color frame: 8px solid border, full screen perimeter
- All interactive zones: minimum 88×88px (Apple HIG minimum)
- Font: system-ui, bold, minimum 32px for any active element

### Joystick Design
- Zone diameter: 180px
- Knob diameter: 72px
- Knob color: player color at 85% opacity
- No dead zone shown visually (keep it clean)
- Max displacement: 54px from center

---

*Art Direction Bible v1.0 | Moments | Noor (Gaming Development Expert)*
