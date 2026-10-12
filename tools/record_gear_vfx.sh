#!/usr/bin/env bash
# 单独复录《命运的齿轮》的普通与空虚模式，保留原生声音。
set -euo pipefail
BRANCH="${1:-stable}"
PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
powershell -NoProfile -ExecutionPolicy Bypass -File "$PROJECT/tools/launch_vfx_recording.ps1" \
  -Environment "$BRANCH" -Scenes 'geer,void-geer'
