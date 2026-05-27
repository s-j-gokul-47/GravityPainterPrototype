#!/usr/bin/env bash
# Import hammer.glb and wire it into Level 2 (scene + prefab).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
"$SCRIPT_DIR/unity-editor.sh" \
  -batchmode \
  -nographics \
  -quit \
  -executeMethod SetupHammerObstacle.BatchSetup \
  "$@"

echo "Hammer setup finished."
