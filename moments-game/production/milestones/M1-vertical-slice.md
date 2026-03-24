# Milestone 1 — Vertical Slice

**Target Date**: 2026-04-08  
**Owner**: producer  
**Stage gate**: Pre-Alpha → Alpha

## Goal

A complete, playable demonstration of the Moments core loop:

> TV shows QR code → player scans on phone → joins lobby → picks a hero → game starts → plays Polar Push → sees results

## Exit Criteria

- [ ] Unity 6 project boots and runs at 60fps on host machine
- [ ] Phone controller served via local HTTP — accessible by QR scan
- [ ] Player join funnel ≤ 10 seconds from scan to ready state
- [ ] Character select: 8 hero slots, lock state synced to TV
- [ ] Polar Push: 2–4 players, full elimination loop, score shown in results
- [ ] No crash on full session (attract → join → play → results)
- [ ] Tested on iOS Safari + Android Chrome

## Success Metrics

| Metric | Target |
|--------|--------|
| Join funnel time | < 10 seconds |
| Session crash rate | 0 in 10 test runs |
| TV framerate | ≥ 60fps |
| Phone input latency | < 80ms (local WiFi) |

## Contents

- Sprint 1 (2026-03-25 → 2026-04-08): all Must Have tasks

## Next Milestone

Milestone 2 — Core Alpha (4 mini-games, all heroes, session playlist)
