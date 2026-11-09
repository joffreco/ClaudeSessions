#!/usr/bin/env bash
# usage: debug-start.sh <container> <repo_dir> [process_pattern='node .*src/index.ts']
# Print the debug process start time and the newest change time among the repo's modified files,
# then OK if the process started after the last change, RESTART otherwise.
set -euo pipefail
ctr=${1:?container}; repo=${2:?repo_dir}; pat=${3:-node .*src/index.ts}
elapsed=$(docker exec "$ctr" bash -lc "ps -eo etimes,args | grep -E '$pat' | grep -v grep | head -1" | awk '{print $1}')
[ -n "$elapsed" ] || { echo "no debug process matching '$pat' in $ctr"; exit 1; }
start_s=$(( $(date +%s) - elapsed ))
last_s=$(cd "$repo" && git diff --name-only HEAD | while read -r f; do [ -f "$f" ] && stat -c %Y "$f"; done | sort -n | tail -1)
echo "debug started: $(date -d "@$start_s" '+%F %T')"
echo "last change:   $(date -d "@${last_s:-0}" '+%F %T')"
[ "${last_s:-0}" -le "$start_s" ] && echo OK || echo RESTART
