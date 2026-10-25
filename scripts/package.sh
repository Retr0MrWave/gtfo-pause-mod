#!/usr/bin/env bash
# Builds Release and writes two zips into dist/:
#   GTFOPause-<ver>.zip         Thunderstore package (r2modman: Settings > Import local mod)
#   GTFOPause-<ver>-manual.zip  BepInEx/plugins/GTFOPause/GTFOPause.dll, to extract into a profile folder
set -euo pipefail
cd "$(dirname "$0")/.."

version=$(sed -n 's/.*"version_number": *"\([^"]*\)".*/\1/p' thunderstore/manifest.json)
plugin_version=$(sed -n 's/.*const string Version = "\([^"]*\)".*/\1/p' GTFOPause/Plugin.cs)
csproj_version=$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' GTFOPause/GTFOPause.csproj)
if [[ "$version" != "$plugin_version" || "$version" != "$csproj_version" ]]; then
  echo "Version mismatch: manifest $version, Plugin.cs $plugin_version, csproj $csproj_version" >&2
  exit 1
fi

dotnet build GTFOPause -c Release

staging=$(mktemp -d)
trap 'rm -rf "$staging"' EXIT
cp thunderstore/manifest.json thunderstore/README.md thunderstore/CHANGELOG.md thunderstore/icon.png "$staging/"
cp GTFOPause/bin/Release/GTFOPause.dll "$staging/"

mkdir -p dist
out="$PWD/dist/GTFOPause-$version.zip"
rm -f "$out"
(cd "$staging" && zip -q -r "$out" .)
echo "Packaged $out"

manual_staging=$(mktemp -d)
trap 'rm -rf "$staging" "$manual_staging"' EXIT
mkdir -p "$manual_staging/BepInEx/plugins/GTFOPause"
cp GTFOPause/bin/Release/GTFOPause.dll "$manual_staging/BepInEx/plugins/GTFOPause/"
manual_out="$PWD/dist/GTFOPause-$version-manual.zip"
rm -f "$manual_out"
(cd "$manual_staging" && zip -q -r -X "$manual_out" BepInEx)
echo "Packaged $manual_out"
