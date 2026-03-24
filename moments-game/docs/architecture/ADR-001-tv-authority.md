# ADR-001: TV-as-Server Authority Model

**Date**: 2026-03-25  
**Status**: Accepted  
**Agents**: technical-director, lead-programmer, network-programmer

## Context

Moments is a couch party game where 2–8 players connect from their phones to a shared game session displayed on a TV. We need to decide where game state lives and how it is synchronized.

## Options Considered

**Option A: Peer-to-peer (all devices equal)**
- Each phone runs simulation and syncs state with others
- Pros: No single host required
- Cons: Complex conflict resolution, cheat-prone, high bandwidth, complex reconnect

**Option B: Phone-as-server**
- One player's phone hosts the session
- Pros: No dedicated TV hardware needed
- Cons: Host's phone is a gameplay bottleneck, phone goes to sleep mid-game, UX split

**Option C: TV-as-server (chosen)**
- TV/PC display runs Unity and owns all game state
- Phones connect via WebSocket and send intent only (move vectors, button taps)
- TV validates, resolves, and broadcasts state
- Pros: Authoritative truth, cheat-resistant, reconnect-friendly, phones stay lightweight
- Cons: Requires TV to be a capable host device (PC, Smart TV, or streaming device)

## Decision

**Option C — TV-as-server** is the project standard.

The TV host is the sole authority for:
- Room token generation and player slot assignment
- Character availability and lock state
- Mini-game simulation and collision resolution
- Score aggregation and leaderboard state
- Scene orchestration and game flow

Phones are read-only clients for game state. They send:
- Join / nickname / hero hover / hero lock / ready
- Per-frame input intent: moveX, moveY, aimX, aimY, buttons
- Reconnect tokens

## Consequences

- All gameplay logic lives in Unity on the TV host
- ControllerGateway must run a WebSocket server on the TV
- Phone web controller is a zero-simulation display layer
- Reconnect is straightforward: TV preserves slot for grace window, phone re-authenticates with token
- Cheating requires physical access to the TV host — acceptable for local multiplayer

## References

- `src/core/SessionStateManager.cs`
- `src/networking/ControllerGateway.cs`
- `src/ui/phone-controller.html`
