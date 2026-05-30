#!/usr/bin/env bash
# Place RedLaserBeam Korrath Beam at Tile (42) in Level 2.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
"$SCRIPT_DIR/unity-editor.sh" \
  -batchmode \
  -nographics \
  -quit \
  -executeMethod SetupLaserGateObstacle.BatchSetup \
  "$@"

echo "Korrath Beam setup finished."
