#!/bin/bash
# Set up the WSL side of ClaudeSessions: link the tab launcher into ~/.local/bin, link the hook into
# ~/.claude/hooks, and register the hook for SessionStart in ~/.claude/settings.json (a copy of the
# previous file is kept as settings.json.bak). Running it again changes nothing.
set -euo pipefail

here=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
settings="$HOME/.claude/settings.json"
command='bash "$HOME/.claude/hooks/claude-sessions-hook.sh"'

command -v jq > /dev/null || { echo "jq is missing: sudo apt install jq" >&2; exit 1; }
command -v claude > /dev/null || echo "warning: claude is not on this shell's PATH; the launcher runs it from a login shell" >&2

mkdir -p "$HOME/.local/bin" "$HOME/.claude/hooks"
ln -sfn "$here/claude-sessions-tab" "$HOME/.local/bin/claude-sessions-tab"
ln -sfn "$here/claude-sessions-hook.sh" "$HOME/.claude/hooks/claude-sessions-hook.sh"

[ -f "$settings" ] || echo '{}' > "$settings"
if jq -e --arg c "$command" '[.hooks.SessionStart[]?.hooks[]?.command] | index($c)' "$settings" > /dev/null; then
  echo "hook already registered in $settings"
else
  cp "$settings" "$settings.bak"
  tmp=$(mktemp "$settings.XXXXXX")
  jq --arg c "$command" '.hooks.SessionStart += [{hooks: [{type: "command", command: $c, timeout: 10}]}]' "$settings" > "$tmp"
  mv -f "$tmp" "$settings"
  echo "hook registered in $settings (previous file: $settings.bak)"
fi
echo "launcher: $HOME/.local/bin/claude-sessions-tab -> $here/claude-sessions-tab"
echo "hook:     $HOME/.claude/hooks/claude-sessions-hook.sh -> $here/claude-sessions-hook.sh"
