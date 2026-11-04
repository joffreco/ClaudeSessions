#!/bin/bash
# Build the Windows apps from WSL into publish/: restores the terminal control, then publishes ClaudeSessions
# and Consignes for win-x64 into the same folder.
# Usage: ./publish.sh
set -euo pipefail
root=$(cd "$(dirname "$0")" && pwd)
case "${1:-}" in
  "") ;;
  -h|--help) sed -n '2,4p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
  *) echo "publish: unknown option: $1" >&2; exit 2 ;;
esac
"$root/restore-terminal-control.sh"
for project in ClaudeSessions.Windows ClaudeSessions.Consignes; do
  dotnet publish "$root/src/$project" -c Release -r win-x64 --self-contained false -o "$root/publish" -nologo
done
for file in ClaudeSessions.exe Consignes.exe Markdig.dll Microsoft.Terminal.Wpf.dll Microsoft.Terminal.Control.dll conpty.dll x64/OpenConsole.exe; do
  [ -f "$root/publish/$file" ] || { echo "publish: $file missing from publish/" >&2; exit 1; }
done
echo "Published to $root/publish"
