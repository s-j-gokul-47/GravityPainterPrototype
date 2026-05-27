#!/usr/bin/env bash
# Run Unity Editor CLI for this project (batchmode, tests, etc.)
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT_VERSION_FILE="$PROJECT_ROOT/ProjectSettings/ProjectVersion.txt"

read_version() {
  grep 'm_EditorVersion:' "$PROJECT_VERSION_FILE" | awk '{print $2}'
}

find_editor() {
  local version="$1"
  local hub="/Applications/Unity/Hub/Editor/${version}/Unity.app/Contents/MacOS/Unity"
  if [[ -x "$hub" ]]; then
    echo "$hub"
    return 0
  fi

  local newest
  newest="$(ls -1 /Applications/Unity/Hub/Editor 2>/dev/null | sort -V | tail -1 || true)"
  if [[ -n "$newest" ]]; then
    hub="/Applications/Unity/Hub/Editor/${newest}/Unity.app/Contents/MacOS/Unity"
    if [[ -x "$hub" ]]; then
      echo "$hub"
      return 0
    fi
  fi

  return 1
}

VERSION="$(read_version)"
UNITY="$(find_editor "$VERSION")" || {
  echo "Unity Editor not found for version ${VERSION}." >&2
  echo "Install via Unity Hub or: Unity Hub -- --headless install -v ${VERSION}" >&2
  exit 1
}

echo "Using Unity: $UNITY (project asks for $VERSION)" >&2
exec "$UNITY" -projectPath "$PROJECT_ROOT" "$@"
