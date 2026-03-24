# ADR-002: WebSocket Transport Over Mirror/Netcode

**Date**: 2026-03-25  
**Status**: Accepted  
**Agents**: technical-director, network-programmer

## Context

Moments needs real-time bidirectional communication between the Unity TV host and phone browser clients (HTML/JS). We need a transport protocol that works across this heterogeneous client mix.

## Options Considered

**Option A: Unity Netcode for GameObjects (NGO)**
- Unity's official multiplayer framework
- Pros: Tight Unity integration, state syncing built-in, good tooling
- Cons: Requires Unity client on all devices — phones would need a native app, not a browser

**Option B: Mirror Networking**
- Open-source Unity multiplayer framework
- Pros: Flexible transports, proven in production
- Cons: Same problem — requires Unity on all clients, no browser support without workarounds

**Option C: Raw WebSocket (chosen)**
- Unity TV runs WebSocketSharp server
- Phone browsers use native `WebSocket` API (supported in all modern mobile browsers)
- Custom JSON message protocol
- Pros: Universal browser support, no app install, lightweight phone client, full control
- Cons: We own the protocol; no built-in state sync framework

**Option D: WebRTC**
- P2P-capable, low latency
- Pros: Very low latency for input
- Cons: Complex signaling server, overkill for 2–8 player local LAN game

## Decision

**Option C — Raw WebSocket** is the project standard.

Rationale:
- Phone browser compatibility is non-negotiable (zero app install requirement)
- Local LAN latency is acceptable (< 5ms typical, WebSocket overhead negligible)
- Protocol is simple: JSON messages, intent-only from phones, state broadcasts from TV
- WebSocketSharp is a mature Unity-compatible library

## Protocol Spec

```
Phone → TV:  { type: "join|heroHover|heroLock|ready|input|reconnect", ...payload }
TV → Phone:  { type: "welcome|stateUpdate|countdown|layout|haptic|eliminated|podium|error", ...payload }
```

Input at 20fps (50ms polling interval) — sufficient for party game input latency.

## Consequences

- `ControllerGateway.cs` must start a WebSocketSharp server on boot
- All WebSocket I/O on background thread; Unity main thread receives queued events
- Phone controller is a pure HTML/JS file served from the TV host (local HTTP server or embedded)
- Need CORS-safe serving if using a local HTTP file server

## References

- `src/networking/ControllerGateway.cs`
- `src/ui/phone-controller.html`
