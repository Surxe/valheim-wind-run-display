#!/usr/bin/env bash
# Builds the 256x256 package icon (assets/icon.png) from an in-game screenshot of the
# Wind run status effect (assets/icon-source.png, any non-square size). The source
# is scaled to the full height and centered; the side gaps are filled with a
# blurred, slightly darkened zoom of the same image so the background continues.
#
#   bash scripts/make-icon.sh [source.png] [out.png]
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SRC="${1:-$REPO/assets/icon-source.png}"
OUT="${2:-$REPO/assets/icon.png}"

ffmpeg -v error -y -i "$SRC" -filter_complex "\
[0:v]format=rgb24,split[a][b];\
[a]scale=256:256:force_original_aspect_ratio=increase,crop=256:256,boxblur=12:2,eq=brightness=-0.06[bg];\
[b]scale=-1:256:flags=lanczos[fg];\
[bg][fg]overlay=(W-w)/2:0" -frames:v 1 "$OUT"

echo "Wrote $OUT"
