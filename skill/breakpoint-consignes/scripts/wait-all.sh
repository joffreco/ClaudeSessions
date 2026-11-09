#!/usr/bin/env bash
# usage: wait-all.sh <consignes_tab_dir> [glob='bp-*']
# Wait on every open consigne matching <glob> (consigne wait per file), print the first one that ends,
# prefixed by its id, then stop the other waits. Launch with Bash run_in_background; relaunch after each wake.
dir=${1:?consignes_tab_dir}; glob=${2:-bp-*}
shopt -s nullglob
files=("$dir"/$glob.md)
[ ${#files[@]} -gt 0 ] || { echo "no open consigne matches $dir/$glob.md" >&2; exit 1; }
for f in "${files[@]}"; do
  id=$(basename "$f" .md)
  ( consigne wait "$f" | sed "s/^/[$id] /" ) &
done
wait -n
sleep 1
pkill -f "consigne wait $dir/" 2>/dev/null
true
