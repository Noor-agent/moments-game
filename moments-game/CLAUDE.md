# MOMENTS — Claude Code Game Studios Master Config

## Project Identity
- **Game:** Moments
- **Genre:** Couch party game — TV as server, phones as controllers
- **Engine:** Unity 6, URP, Addressables
- **Architecture:** TV host (authoritative server) + phone web controllers (lightweight clients)
- **Inspiration:** Crash Bash (PS1) — fast, chaotic mini-game arenas with strong character identity
- **Visual Style:** Premium stylized 3D — toy-like, rounded, vibrant, readable from couch distance

## Design Pillars
1. **Instant Social Entry** — any player joins in under 10 seconds via QR code
2. **Readable Chaos** — audience always knows who's winning and what's happening
3. **Short-Form Variety** — each mini-game uses a different verb / input pattern
4. **Character Attachment** — 8 pre-designed heroes with strong personality
5. **TV-First Spectacle** — television is the center of gravity; phone is utility

## Agent Hierarchy for This Project
- **Creative Director:** Overall vision, style, character roster approval
- **Technical Director:** Unity 6 architecture, WebSocket stack, Addressables pipeline
- **Producer:** Sprint planning, scope, roadmap
- **Game Designer:** Mini-game rules, balance, flow
- **Lead Programmer:** Core systems, networking, session state
- **Art Director:** 3D style guide, character turnarounds, arena themes
- **UI Programmer:** TV HUD, phone controller layouts, QR join flow
- **Unity Specialist:** Engine-specific implementation (URP shaders, Cinemachine, Addressables)

## Technology Stack
- Unity 6 + URP (Universal Render Pipeline)
- WebSocketSharp (or Mirror/Netcode for phone ↔ TV communication)
- Unity Addressables (mini-game scene streaming)
- Cinemachine (dynamic TV cameras)
- Unity Localization (multilingual prompts)
- Phone controller: Lightweight responsive web app (no app install required)

## Studio Conventions
- All gameplay state is authoritative on TV host — phones send intent only
- Data-driven via ScriptableObjects (MiniGameDefinition, CharacterDefinition, etc.)
- Path-scoped rules enforced (see .claude/rules/)
- Collaboration protocol: Question → Options → Decision → Draft → Approval
