#!/usr/bin/env bash
# Run the Game.Core.Tests EditMode suite headlessly via Unity batch mode.
#
# This is the durable --verify command for the rope/interaction stories
# (US-001, US-002). It cannot run while the Unity Editor has this project
# open (Unity holds an exclusive project lock) — close the Editor or run it
# in CI. The MCP `run_tests` tool is the in-Editor equivalent for local dev.
#
# Override the editor path with UNITY_BIN if your install differs.
set -euo pipefail

PROJECT_PATH="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
UNITY_BIN="${UNITY_BIN:-$HOME/Unity/Hub/Editor/6000.5.1f1/Editor/Unity}"
RESULTS="${PROJECT_PATH}/Logs/editmode-results.xml"

mkdir -p "${PROJECT_PATH}/Logs"

"${UNITY_BIN}" \
  -runTests \
  -batchmode \
  -projectPath "${PROJECT_PATH}" \
  -testPlatform EditMode \
  -testResults "${RESULTS}" \
  -logFile - \
  -testCategory "" \
  -testFilter "Game.Core.Tests"

echo "EditMode results written to ${RESULTS}"
