# Claude Code Game Studios — Quick Reference

Based on [Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios) by Donchitos (MIT License).

## Studio Hierarchy

```
Tier 1 — Directors (Opus)
  creative-director   technical-director   producer

Tier 2 — Department Leads (Sonnet)
  game-designer       lead-programmer      art-director
  audio-director      narrative-director   qa-lead
  release-manager     localization-lead

Tier 3 — Specialists (Sonnet/Haiku)
  gameplay-programmer  engine-programmer    ai-programmer
  network-programmer   tools-programmer     ui-programmer
  systems-designer     level-designer       economy-designer
  technical-artist     sound-designer       writer
  world-builder        ux-designer          prototyper
  performance-analyst  devops-engineer      analytics-engineer
  security-engineer    qa-tester            accessibility-specialist
  live-ops-designer    community-manager
```

### Engine Specialists

| Engine | Lead | Sub-Specialists |
|--------|------|-----------------|
| Godot 4 | godot-specialist | GDScript, Shaders, GDExtension |
| Unity | unity-specialist | DOTS/ECS, Shaders/VFX, Addressables, UI Toolkit |
| Unreal 5 | unreal-specialist | GAS, Blueprints, Replication, UMG/CommonUI |

## All 37 Slash Commands

**Reviews**: `/design-review` `/code-review` `/balance-check` `/asset-audit` `/scope-check` `/perf-profile` `/tech-debt`

**Production**: `/sprint-plan` `/milestone-review` `/estimate` `/retrospective` `/bug-report`

**Project**: `/start` `/project-stage-detect` `/reverse-document` `/gate-check` `/map-systems` `/design-system`

**Release**: `/release-checklist` `/launch-checklist` `/changelog` `/patch-notes` `/hotfix`

**Creative**: `/brainstorm` `/playtest-report` `/prototype` `/onboard` `/localize`

**Team Orchestration**: `/team-combat` `/team-narrative` `/team-ui` `/team-release` `/team-polish` `/team-audio` `/team-level`

## Coordination Rules

1. **Vertical delegation** — Directors → Leads → Specialists
2. **Horizontal consultation** — Same-tier agents consult but don't bind cross-domain
3. **Conflict resolution** — Escalate to shared parent
4. **Change propagation** — Cross-department via `producer`
5. **Domain boundaries** — No cross-domain file modifications without delegation

## Collaboration Protocol

Every task: **Question → Options → Decision → Draft → Approval**

- Agents ask before proposing
- Present 2-4 options with pros/cons
- User always decides
- Show drafts before finalizing
- Nothing written without sign-off

## Onboarding Paths

| Starting Point | Recommended Path |
|---|---|
| No idea | `/start` → `/brainstorm` → `/setup-engine` → `/map-systems` → `/prototype` → `/sprint-plan` |
| Vague idea | `/brainstorm [hint]` → `/setup-engine` → `/map-systems` → `/design-system` → `/sprint-plan` |
| Clear concept | `/setup-engine` → `/map-systems` → `/design-system` → `/architecture-decision` → `/sprint-plan` |
| Existing work | `/project-stage-detect` → `/gate-check` → `/sprint-plan` |

## 8 Automated Hooks

| Hook | Trigger | Purpose |
|------|---------|---------|
| session-start.sh | Session open | Load sprint context, recent git activity |
| detect-gaps.sh | Session open | Detect fresh projects, missing docs |
| validate-commit.sh | git commit | Check hardcoded values, TODO format, JSON, design docs |
| validate-push.sh | git push | Warn on protected branch pushes |
| validate-assets.sh | File writes in assets/ | Naming conventions, JSON structure |
| pre-compact.sh | Context compression | Preserve session progress |
| session-stop.sh | Session close | Log accomplishments |
| log-agent.sh | Agent spawned | Audit trail of subagent invocations |

## 11 Path-Scoped Rules

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

## Design Theory Foundations

- **MDA Framework** — Mechanics, Dynamics, Aesthetics
- **Self-Determination Theory** — Autonomy, Competence, Relatedness
- **Flow State Design** — Challenge-skill balance
- **Bartle Player Types** — Audience targeting
- **Verification-Driven Development** — Tests first
