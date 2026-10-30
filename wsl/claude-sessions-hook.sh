#!/bin/bash
# SessionStart hook: record which session runs in each ClaudeSessions tab, so the app follows /clear,
# /resume and forks. Writes ~/.claude/session-tabs/<tab_id>.json on every SessionStart of a tab's own
# claude; the app, not SessionEnd, removes the record. Silent, always exits 0.
# Input (stdin JSON): { session_id, transcript_path, cwd, hook_event_name, source }
# A session counts as a tab when CLAUDE_SESSIONS_TAB holds a UUID and the nearest ancestor named claude
# has the PID in CLAUDE_SESSIONS_TAB_PID (both set by claude-sessions-tab): a background copy or a
# nested claude inherits the variables but not the PID, and is ignored.

tabs="${CLAUDE_SESSION_TABS:-$HOME/.claude/session-tabs}"
uuid='^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$'
input=$(cat)
field() { printf '%s' "$input" | jq -r --arg k "$1" '.[$k] // ""' 2>/dev/null; }

tab_session() {
  [[ "$CLAUDE_SESSIONS_TAB" =~ $uuid ]] && [[ "$CLAUDE_SESSIONS_TAB_PID" =~ ^[0-9]+$ ]] || return 1
  local pid=$PPID depth=0 comm
  while [ "$pid" -gt 1 ] && [ "$depth" -lt 64 ]; do
    comm=$(cat "/proc/$pid/comm" 2>/dev/null) || return 1
    if [ "$comm" = "claude" ]; then
      [ "$pid" = "$CLAUDE_SESSIONS_TAB_PID" ]
      return
    fi
    pid=$(awk '/^PPid:/ { print $2 }' "/proc/$pid/status" 2>/dev/null)
    [[ "$pid" =~ ^[0-9]+$ ]] || return 1
    depth=$((depth + 1))
  done
  return 1
}

[ "$(field hook_event_name)" = "SessionStart" ] || exit 0
[ "$CLAUDE_CODE_ENTRYPOINT" = "cli" ] || exit 0
[[ "$(field session_id)" =~ ^[A-Za-z0-9-]+$ ]] || exit 0
tab_session || exit 0

mkdir -p "$tabs"
tmp=$(mktemp "$tabs/.tmp.XXXXXX") || exit 0
if printf '%s' "$input" | jq --arg tab "$CLAUDE_SESSIONS_TAB" '{tab_id: $tab, session_id, cwd, transcript_path}' > "$tmp" 2>/dev/null; then
  mv -f "$tmp" "$tabs/$CLAUDE_SESSIONS_TAB.json"
else
  rm -f "$tmp"
fi
exit 0
