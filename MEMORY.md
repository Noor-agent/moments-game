# MEMORY.md — Noor's Long-Term Memory

## Identity
- **My name:** Noor
- **Role:** Gaming development expert
- **Boss's name:** Boss (GMT+8 timezone)
- **Vibe:** Sharp, direct, technical — no fluff

---

## Skill: Claude Code Game Studios (CCGS)

**Loaded from:** `Gaming studios .zip` → extracted to `gaming-studios-skill/game-studio/`
**Source repo:** https://github.com/Donchitos/Claude-Code-Game-Studios (MIT License, by Donchitos)

### What it does
Scaffolds a full AI-assisted game development studio with:
- **48 agents** in a 3-tier hierarchy (Directors → Leads → Specialists)
- **37 workflow skills** (slash commands)
- **8 automated hooks** (session-start, commit validation, asset checks, etc.)
- **11 path-scoped coding rules**
- **29 document templates**

### Setup command
```bash
python3 gaming-studios-skill/game-studio/scripts/setup_game_studio.py --target-dir <project-path>
```
This clones the latest framework from GitHub and sets up `.claude/`, `CLAUDE.md`, and all project directories.

### Agent Tiers
| Tier | Model | Roles |
|------|-------|-------|
| 1 — Directors | Opus | creative-director, technical-director, producer |
| 2 — Department Leads | Sonnet | game-designer, lead-programmer, art-director, audio-director, narrative-director, qa-lead, release-manager, localization-lead |
| 3 — Specialists | Sonnet/Haiku | gameplay-programmer, engine-programmer, ai-programmer, network-programmer, tools-programmer, ui-programmer, systems-designer, level-designer, economy-designer, technical-artist, sound-designer, writer, world-builder, ux-designer, prototyper, performance-analyst, devops-engineer, analytics-engineer, security-engineer, qa-tester, accessibility-specialist, live-ops-designer, community-manager |

### Engine Specialists
- **Godot 4:** godot-specialist (GDScript, Shaders, GDExtension)
- **Unity:** unity-specialist (DOTS/ECS, Shaders/VFX, Addressables, UI Toolkit)
- **Unreal 5:** unreal-specialist (GAS, Blueprints, Replication, UMG/CommonUI)

### Key Slash Commands (37 total)
- **Reviews:** `/design-review` `/code-review` `/balance-check` `/asset-audit` `/scope-check` `/perf-profile` `/tech-debt`
- **Production:** `/sprint-plan` `/milestone-review` `/estimate` `/retrospective` `/bug-report`
- **Project:** `/start` `/project-stage-detect` `/reverse-document` `/gate-check` `/map-systems` `/design-system`
- **Release:** `/release-checklist` `/launch-checklist` `/changelog` `/patch-notes` `/hotfix`
- **Creative:** `/brainstorm` `/playtest-report` `/prototype` `/onboard` `/localize`
- **Team Orchestration:** `/team-combat` `/team-narrative` `/team-ui` `/team-release` `/team-polish` `/team-audio` `/team-level`

### Onboarding Paths
| Starting Point | Path |
|---|---|
| No idea | `/start` → `/brainstorm` → `/setup-engine` → `/map-systems` → `/prototype` → `/sprint-plan` |
| Vague idea | `/brainstorm [hint]` → `/setup-engine` → `/map-systems` → `/design-system` → `/sprint-plan` |
| Clear concept | `/setup-engine` → `/map-systems` → `/design-system` → `/architecture-decision` → `/sprint-plan` |
| Existing work | `/project-stage-detect` → `/gate-check` → `/sprint-plan` |

### Collaboration Protocol
Every task follows: **Question → Options → Decision → Draft → Approval**
- Agents always ask before proposing
- Present 2-4 options with pros/cons
- User always decides — nothing written without sign-off

### Design Theory Foundations
- MDA Framework (Mechanics, Dynamics, Aesthetics)
- Self-Determination Theory (Autonomy, Competence, Relatedness)
- Flow State Design (challenge-skill balance)
- Bartle Player Types (audience targeting)
- Verification-Driven Development (tests first)

### Automated Hooks
| Hook | Trigger |
|------|---------|
| session-start.sh | Session open — loads sprint context |
| detect-gaps.sh | Session open — detects missing docs |
| validate-commit.sh | git commit — checks hardcoded values, TODOs, JSON |
| validate-push.sh | git push — warns on protected branch |
| validate-assets.sh | File writes in assets/ — naming + JSON structure |
| pre-compact.sh | Context compression — preserves session progress |
| session-stop.sh | Session close — logs accomplishments |
| log-agent.sh | Agent spawned — audit trail |

### Path-Scoped Rules
| Path | Enforces |
|------|----------|
| src/gameplay/** | Data-driven values, delta time, no UI refs |
| src/core/** | Zero hot-path allocations, thread safety, API stability |
| src/ai/** | Performance budgets, debuggability, data-driven params |
| src/networking/** | Server-authoritative, versioned messages, security |
| src/ui/** | No game state ownership, localization-ready, accessibility |
| design/gdd/** | 8 required sections, formula format, edge cases |
| tests/** | Test naming, coverage requirements, fixture patterns |
| prototypes/** | Relaxed standards, README required, hypothesis documented |
| assets/data/** | JSON validity, naming conventions |
| src/shaders/** | Performance comments, LOD variants |
| design/narrative/** | Character voice consistency, lore references |

---

## Last Daily Summary
_(none yet)_
