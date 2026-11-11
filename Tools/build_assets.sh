#!/usr/bin/env bash
# Rebuilds every model, texture and data file of TANGHALAN VR from the web app's sources.
#
#   Tools/build_assets.sh
#
# Steps: (1) export the museum, the street and the props from three.js (Tools/web-export);
#        (2) lay out the palette of flat colors; (3) copy the textures into the Unity project;
#        (4) build the Blender files and the FBX models (Tools/blender); (5) write Unity's .meta files;
#        (6) write the C# data files from the same content.
#
# Needs Node 20+, Python 3 with Pillow, and Blender 4.2 LTS or newer: either set BLENDER to the blender
# program, or set BLENDER_PYTHON to a Python that has the bpy module (pip install bpy==4.2.*).
set -euo pipefail
cd "$(dirname "$0")/.."
TEX=Unity/Assets/TANGHALAN/Resources/TANGHALAN/Textures

echo "== 1. export from the web app"
(cd Tools/web-export && node export.mjs)

echo "== 2. palette"
python3 Tools/blender/make_palette.py Tools/web-export/out/glb Tools/blender/palette.png Tools/blender/palette.json

echo "== 3. textures"
mkdir -p "$TEX"
cp Tools/web-export/out/textures/*.png "$TEX/"
cp Tools/blender/palette.png "$TEX/palette.png"

echo "== 4. Blender files and FBX models"
if [ -n "${BLENDER:-}" ]; then
  "$BLENDER" --background --python Tools/blender/build_models.py -- "$@" 2>&1 | grep -E "pieces ->|rror|Traceback" || true
else
  "${BLENDER_PYTHON:-python3}" Tools/blender/build_models.py "$@" 2>&1 | grep -E "pieces ->|rror|Traceback" || true
fi

echo "== 5. Unity .meta files"
python3 Tools/unity_meta.py

echo "== 6. C# data"
node Tools/web-export/gen-csharp.mjs
echo "done"
