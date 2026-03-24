---
name: game-studio
description: "Scaffold and manage a full AI game development studio using Claude Code Game Studios (48 agents, 37 skills, 8 hooks, 11 rules, 29 templates). Use when the user wants to: (1) Set up a new game development project with a structured studio framework, (2) Create a game from scratch with AI-assisted design, programming, art direction, and production workflows, (3) Organize an existing game project with professional studio structure, (4) Use specialized game dev agents (creative-director, gameplay-programmer, level-designer, etc.), or (5) Run game dev workflows like brainstorm, sprint-plan, code-review, design-review, prototype. Triggers: 'game studio', 'game development', 'make a game', 'game project', 'claude code game studios', 'CCGS'."
---

# Claude Code Game Studios

Set up a complete AI game development studio: 48 specialized agents in a 3-tier hierarchy (Directors → Leads → Specialists), 37 workflow skills, 8 automated hooks, 11 path-scoped coding rules, and 29 document templates.

Based on [Claude-Code-Game-Studios](https://github.com/Donchitos/Claude-Code-Game-Studios) by Donchitos (MIT License).

## Setup

Run the scaffolder to set up the framework in the target project directory:

```bash
python3 scripts/setup_game_studio.py --target-dir <project-path>
```

This clones the latest framework from GitHub and creates:
- `.claude/` — 48 agents, 37 skills, 8 hooks, 11 rules, docs, templates
- `CLAUDE.md` — Master configuration
- `src/`, `assets/`, `design/`, `docs/`, `tests/`, `tools/`, `prototypes/`, `production/` — Project directories

After setup, guide the user to open Claude Code in the project and run `/start`.

## Post-Setup Guidance

See [references/quick-reference.md](references/quick-reference.md) for the full studio hierarchy, all 37 commands, hooks, rules, and onboarding paths.

### Route user by situation

| User says | Route to |
|-----------|----------|
| "No idea what to build" | `/start` or `/brainstorm open` |
| "I have a vague idea" | `/brainstorm [their hint]` |
| "I know what I want" | `/setup-engine [engine] [version]` → `/map-systems` |
| "I have existing code" | `/project-stage-detect` → `/gate-check` |

### Key concepts

1. **Agents are specialized** — 48 agents with defined responsibilities, tools, and delegation paths. Pick the right agent for the job.
2. **Collaborative, not autonomous** — Agents ask, present options, wait for decisions. Nothing written without approval.
3. **Hooks run automatically** — Commit validation, session state, asset checks, gap detection via shell scripts.
4. **Rules are path-scoped** — Standards enforced by file location (e.g., `src/gameplay/` requires data-driven values).
5. **Engine-agnostic** — Supports Godot 4, Unity, Unreal 5 with dedicated specialist agents.
