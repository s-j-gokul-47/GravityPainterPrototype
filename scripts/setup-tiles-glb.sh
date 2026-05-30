#!/usr/bin/env bash
# Build tiles.glb prefab and apply to Tile (37) in Level 2.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
"$SCRIPT_DIR/unity-editor.sh" \
  -batchmode \
  -nographics \
  -quit \
  -executeMethod SetupTilesGlb.BatchSetup \
  "$@"

echo "tiles.glb setup finished."
