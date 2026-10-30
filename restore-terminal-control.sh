#!/bin/bash
# Download the Windows Terminal WPF control (pinned, SHA-512 checked) into vendor/terminal.
# The nupkg stores its entries with '\' separators, which NuGet on Linux cannot read, so the
# package is extracted with unzip instead of being restored by a PackageReference.
# Usage: ./restore-terminal-control.sh [--force]
set -euo pipefail

package=ci.microsoft.terminal.wpf
version=1.25.260303002
sha512=dQ8sGT/vD8Gvc01J1Y7Xmf/xb+kLDtkHqrhPgY+Mr11ojIG3mCNCyQ0h0K53MdQF4h28Wid7xprReLYUC2xlKQ==
url="https://api.nuget.org/v3-flatcontainer/$package/$version/$package.$version.nupkg"
root=$(cd "$(dirname "$0")" && pwd)
dest="$root/vendor/terminal"
expected=(
  "lib/net8.0-windows7.0/Microsoft.Terminal.Wpf.dll"
  "runtimes/win-x64/native/Microsoft.Terminal.Control.dll"
)

case "${1:-}" in
  "") ;;
  --force) rm -rf "$dest" ;;
  -h|--help) sed -n '2,5p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
  *) echo "restore-terminal-control: unknown option: $1" >&2; exit 2 ;;
esac

complete() {
  local file
  for file in "${expected[@]}"; do
    [ -f "$dest/$file" ] || return 1
  done
}

if complete; then
  echo "Terminal control $version already in $dest"
  exit 0
fi

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT
curl -fsSL -o "$work/package.nupkg" "$url"
actual=$(openssl dgst -sha512 -binary "$work/package.nupkg" | base64 -w0)
if [ "$actual" != "$sha512" ]; then
  echo "restore-terminal-control: SHA-512 mismatch for $package $version" >&2
  exit 1
fi

rm -rf "$dest"
mkdir -p "$dest"
# unzip exits 1 on this package: a warning about the '\' separators, while the paths come out right.
unzip -o -q "$work/package.nupkg" -d "$dest" || [ $? -eq 1 ]
if ! complete; then
  echo "restore-terminal-control: expected files missing after extraction" >&2
  exit 1
fi
echo "Terminal control $version extracted to $dest"
