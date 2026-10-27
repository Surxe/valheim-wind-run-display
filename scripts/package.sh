#!/usr/bin/env bash
# Builds the Thunderstore package: dist/WindRunDisplay-<version>.zip containing
#   manifest.json, README.md, CHANGELOG.md, icon.png (from assets/icon-clean.png),
#   plugins/WindRunDisplay.dll
# Fails if the version differs between manifest.json, src/Plugin.cs and the csproj,
# or if the tests fail.
#
#   bash scripts/package.sh
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO"

ver_manifest="$(python3 -I -c 'import json;print(json.load(open("manifest.json"))["version_number"])')"
ver_plugin="$(sed -nE 's/.*const string Version = "([^"]+)".*/\1/p' src/Plugin.cs)"
ver_csproj="$(sed -nE 's|.*<Version>([^<]+)</Version>.*|\1|p' WindRunDisplay.csproj)"
if [ "$ver_manifest" != "$ver_plugin" ] || [ "$ver_manifest" != "$ver_csproj" ]; then
  echo "ERROR: version mismatch: manifest=$ver_manifest plugin=$ver_plugin csproj=$ver_csproj"
  exit 1
fi
name="$(python3 -I -c 'import json;print(json.load(open("manifest.json"))["name"])')"

rm -rf bin/Release obj/Release
dotnet build -c Release WindRunDisplay.csproj -nologo -v q
dotnet test tests/WindRunDisplay.Tests.csproj -nologo -v q

stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
mkdir -p "$stage/plugins"
cp manifest.json README.md CHANGELOG.md "$stage/"
cp assets/icon-clean.png "$stage/icon.png"
cp bin/Release/WindRunDisplay.dll "$stage/plugins/"

mkdir -p dist
out="dist/$name-$ver_manifest.zip"
rm -f "$out"
(cd "$stage" && zip -q -X -r "$REPO/$out" manifest.json README.md CHANGELOG.md icon.png plugins)

echo
unzip -l "$out"
sha256sum "$out" "$stage/plugins/WindRunDisplay.dll"
