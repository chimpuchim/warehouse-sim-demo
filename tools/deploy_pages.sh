#!/usr/bin/env bash
# Publish Build/WebGL to the gh-pages branch of the current repo (GitHub Pages).
# Usage: tools/deploy_pages.sh [remote]   (default remote: origin)
set -euo pipefail
cd "$(dirname "$0")/.."
REMOTE="${1:-origin}"
SRC="Build/WebGL"
[ -f "$SRC/index.html" ] || { echo "No build at $SRC. Run the WebGL build first." >&2; exit 1; }

TMP="$(mktemp -d)"
cp -R "$SRC"/. "$TMP"/
touch "$TMP/.nojekyll"
rm -f "$TMP/unity-build.provenance.json"

git -C "$TMP" init -q
git -C "$TMP" checkout -q -b gh-pages
git -C "$TMP" add -A
git -C "$TMP" -c user.name="$(git config user.name)" -c user.email="$(git config user.email)" \
  commit -q -m "Deploy WebGL build $(date +%Y-%m-%d)"
git -C "$TMP" push -f "$(git remote get-url "$REMOTE")" gh-pages
rm -rf "$TMP"
echo "Pushed gh-pages. Enable Pages (branch gh-pages, folder /) in the repo settings if not done yet."
