#!/usr/bin/env python3
"""
Claude Code Game Studios — Project Scaffolder
Based on https://github.com/Donchitos/Claude-Code-Game-Studios (MIT License)

Clones the game studio framework and sets up the project structure.
Usage: python3 setup_game_studio.py [--target-dir DIR]
"""

import argparse
import os
import shutil
import subprocess
import sys
import tempfile

REPO_URL = "https://github.com/Donchitos/Claude-Code-Game-Studios.git"

def run(cmd, cwd=None):
    result = subprocess.run(cmd, shell=True, cwd=cwd, capture_output=True, text=True)
    if result.returncode != 0:
        print(f"Error running: {cmd}", file=sys.stderr)
        print(result.stderr, file=sys.stderr)
        sys.exit(1)
    return result.stdout.strip()

def scaffold(target_dir):
    target = os.path.abspath(target_dir)
    print(f"\n=== Claude Code Game Studios — Setup ===\n")
    print(f"Target: {target}\n")

    # Clone to temp dir
    with tempfile.TemporaryDirectory() as tmp:
        clone_dir = os.path.join(tmp, "ccgs")
        print("Cloning repository...")
        run(f"git clone --depth 1 {REPO_URL} {clone_dir}")

        # Files and dirs to copy from the repo
        items_to_copy = [
            ".claude",
            "CLAUDE.md",
        ]

        # Dirs to create (empty project structure)
        dirs_to_create = [
            "src/core", "src/gameplay", "src/ai", "src/networking", "src/ui", "src/tools",
            "assets/art", "assets/audio", "assets/vfx", "assets/shaders", "assets/data",
            "design/gdd", "design/narrative", "design/levels", "design/balance",
            "docs/architecture", "docs/api", "docs/postmortems",
            "docs/engine-reference/godot", "docs/engine-reference/unity",
            "docs/engine-reference/unreal",
            "tests/unit", "tests/integration", "tests/performance", "tests/playtest",
            "tools/ci", "tools/build", "tools/asset-pipeline",
            "prototypes",
            "production/sprints", "production/milestones", "production/releases",
            "production/session-state", "production/session-logs",
        ]

        # Copy framework files
        for item in items_to_copy:
            src = os.path.join(clone_dir, item)
            dst = os.path.join(target, item)
            if os.path.isdir(src):
                if os.path.exists(dst):
                    shutil.rmtree(dst)
                shutil.copytree(src, dst)
                print(f"  Copied: {item}/")
            elif os.path.isfile(src):
                os.makedirs(os.path.dirname(dst), exist_ok=True)
                shutil.copy2(src, dst)
                print(f"  Copied: {item}")

        # Copy additional docs
        for item in ["docs/engine-reference", "docs/COLLABORATIVE-DESIGN-PRINCIPLE.md"]:
            src = os.path.join(clone_dir, item)
            dst = os.path.join(target, item)
            if os.path.exists(src):
                if os.path.isdir(src):
                    if os.path.exists(dst):
                        shutil.rmtree(dst)
                    shutil.copytree(src, dst)
                else:
                    os.makedirs(os.path.dirname(dst), exist_ok=True)
                    shutil.copy2(src, dst)
                print(f"  Copied: {item}")

        # Create empty project dirs
        for d in dirs_to_create:
            dp = os.path.join(target, d)
            os.makedirs(dp, exist_ok=True)
            # Add .gitkeep for empty dirs
            gitkeep = os.path.join(dp, ".gitkeep")
            if not os.listdir(dp):
                open(gitkeep, "w").close()

        # Copy gitignore if not exists
        gitignore_src = os.path.join(clone_dir, ".gitignore")
        gitignore_dst = os.path.join(target, ".gitignore")
        if os.path.exists(gitignore_src) and not os.path.exists(gitignore_dst):
            shutil.copy2(gitignore_src, gitignore_dst)
            print("  Copied: .gitignore")

        # Make hooks executable
        hooks_dir = os.path.join(target, ".claude/hooks")
        if os.path.exists(hooks_dir):
            for f in os.listdir(hooks_dir):
                if f.endswith(".sh"):
                    os.chmod(os.path.join(hooks_dir, f), 0o755)

    # Count what was created
    agents = len([f for f in os.listdir(os.path.join(target, ".claude/agents")) if f.endswith(".md")])
    skills = len([d for d in os.listdir(os.path.join(target, ".claude/skills")) if os.path.isdir(os.path.join(target, ".claude/skills", d))])
    hooks = len([f for f in os.listdir(os.path.join(target, ".claude/hooks")) if f.endswith(".sh")])
    rules = len([f for f in os.listdir(os.path.join(target, ".claude/rules")) if f.endswith(".md")])

    print(f"\n=== Setup Complete ===")
    print(f"  {agents} agents | {skills} skills | {hooks} hooks | {rules} rules")
    print(f"\nNext steps:")
    print(f"  1. Open Claude Code in the project directory")
    print(f"  2. Run /start to begin guided onboarding")
    print(f"  3. Or run /brainstorm to ideate a game concept from scratch")

def main():
    parser = argparse.ArgumentParser(description="Set up Claude Code Game Studios framework")
    parser.add_argument("--target-dir", default=".", help="Target directory (default: current)")
    args = parser.parse_args()
    scaffold(args.target_dir)

if __name__ == "__main__":
    main()
