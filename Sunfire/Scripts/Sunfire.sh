#!/usr/bin/env bash
set -euo pipefail
IFS=$'\n\t'

dir="$(realpath "$(dirname -- "$0")")"

export PATH="$dir:$PATH"

chmod +x "$dir/sunfire-kitty-previewer"
chmod +x "$dir/sunfire-kitty-cleaner"
chmod +x "$dir/kitty-helper" 2>/dev/null || true

cleanup() {
    sunfire-kitty-cleaner
    rm -rf "$SUNFIRE_KITTY_TEMPDIR"
}
trap cleanup INT HUP EXIT

export SUNFIRE_KITTY_TEMPDIR="$(mktemp -d -t sunfire-kitty-XXXXXX)"
export SUNFIRE_KITTY_IMAGE_ID=1234

dotnet "$dir/Sunfire.dll" "$@"
