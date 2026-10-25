#!/usr/bin/env bash
# Builds Release and zips a Thunderstore package into dist/.
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
