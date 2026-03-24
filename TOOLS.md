# TOOLS.md - Local Notes & CLI Reference

## OpenClaw CLI Reference

When you need to run openclaw commands, use these EXACT commands. Do NOT guess command names.

### Channel Management
```bash
# List configured channels
openclaw channels list

# Add Telegram bot
openclaw channels add --channel telegram --token <BOT_TOKEN>

# Add WhatsApp (interactive QR)
openclaw channels login --channel whatsapp

# Check channel status
openclaw channels status
openclaw channels status --probe

# Remove a channel
openclaw channels remove --channel <name>
```

### Config Management
```bash
# Set a config value
openclaw config set <dot.path> <value>

# Get a config value
openclaw config get <dot.path>

# Show full config
openclaw config show
```

### Other Useful Commands
```bash
# Show gateway status
openclaw status

# Run doctor checks
openclaw doctor
openclaw doctor --fix

# Plugin management
openclaw plugins list
openclaw plugins install <name>

# Restart gateway
sudo systemctl restart openclaw-gateway

# View gateway logs
journalctl -u openclaw-gateway --no-pager -n 100
tail -f /tmp/openclaw-1000/openclaw-$(date +%Y-%m-%d).log
```

### CRITICAL Rules for exec Tool
- The `command` parameter is REQUIRED. Never call exec with empty arguments `{}`.
- If you don't know the right command, tell the user instead of guessing.
- Do NOT invent openclaw subcommands. Use `openclaw --help` or `openclaw <cmd> --help` to discover commands.
- If a command fails, do NOT blindly retry the same command. Analyze the error first.

## Environment
- OS: Ubuntu Linux
- Node: v22
- OpenClaw: installed globally at /opt/mulerun/npm-global/
- Workspace: /home/mulerun/.openclaw/workspace
- User: mulerun (service runs as mulerun)

---
Add whatever helps you do your job. This is your cheat sheet.
