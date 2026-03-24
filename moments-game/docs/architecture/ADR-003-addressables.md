# ADR-003: Unity Addressables for Mini-Game Scene Streaming

**Date**: 2026-03-25  
**Status**: Accepted  
**Agents**: technical-director, unity-addressables-specialist

## Context

Moments has 7 mini-game arenas, each as a full Unity scene with unique geometry, shaders, audio, and VFX. We need a strategy for loading/unloading these without long initial load times.

## Options Considered

**Option A: All scenes in build + LoadScene()**
- All scenes always in the build
- Pros: Simplest setup
- Cons: Long initial load, all assets in memory simultaneously, bad for performance

**Option B: AssetBundles (manual)**
- Manual bundle authoring and loading
- Pros: Fine-grained control
- Cons: Complex tooling, manual dependency tracking, essentially deprecated in favor of Addressables

**Option C: Unity Addressables 2.x (chosen)**
- Each mini-game scene is an Addressable asset group
- TV host loads mini-game by Addressable key at runtime
- Pros: Streaming load, memory management, supports remote hosting for future DLC, good tooling
- Cons: Slightly more setup than direct scene loading

## Decision

**Option C — Unity Addressables** is the project standard for mini-game scenes.

Architecture:
- **Bootstrap scene** (always loaded): persistent host of `SessionStateManager`, `ControllerGateway`, `PlayerRegistry`
- **Shell scene** (TV presentation): Attract, Lobby, Results, Podium — loaded as Addressables
- **Mini-game scenes** (per game): Loaded additively by `MiniGameLoader` using `MiniGameDefinition.sceneAddress`

Each `MiniGameDefinition` ScriptableObject holds an Addressables key. `MiniGameLoader` calls `Addressables.LoadSceneAsync(definition.sceneAddress, LoadSceneMode.Additive)`.

## Loading Strategy

```
Boot → Load Bootstrap scene (persistent)
     → Load Attract scene (Addressable)
     → Player joins → Load Lobby scene (Addressable)
     → Game starts → Unload Lobby, Load MiniGame scene (Addressable, additive)
     → Round ends → Unload MiniGame, Load Results scene (Addressable)
     → Next game → Load next MiniGame scene (Addressable)
     → Session ends → Load Podium scene (Addressable)
```

Pre-warm: During Lobby countdown, begin async loading the first mini-game scene so it's ready when game starts.

## Consequences

- Character prefabs + audio bundles also managed as Addressables
- QA must test load/unload cycles for memory leaks between mini-games
- `MiniGameDefinition.sceneAddress` field maps to Addressables catalog key
- Remote hosting of content bundles enables future live-ops DLC drops

## References

- `src/core/MiniGameDefinition.cs` — `sceneAddress` field
- `docs/engine-reference/unity/plugins/addressables.md`
