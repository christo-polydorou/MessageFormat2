#!/usr/bin/env bash
# Copies the official MessageFormat 2 test suite into the test project at a pinned commit.
#
# Usage: eng/update-spec-tests.sh <commit-or-tag>
#
# After running, update the commit recorded in tests/MessageFormat2.Tests/spec-tests/README.md,
# then run the tests and review any new failures.
set -euo pipefail

commit="${1:?Usage: $0 <commit-or-tag>}"
repo_root="$(cd "$(dirname "$0")/.." && pwd)"
destination="$repo_root/tests/MessageFormat2.Tests/spec-tests"
workdir="$(mktemp -d)"
trap 'rm -rf "$workdir"' EXIT

git clone --quiet https://github.com/unicode-org/message-format-wg.git "$workdir/repo"
git -C "$workdir/repo" checkout --quiet "$commit"

rm -rf "$destination/tests" "$destination/schemas"
mkdir -p "$destination"
cp -R "$workdir/repo/test/tests" "$destination/tests"
cp -R "$workdir/repo/test/schemas" "$destination/schemas"
cp "$workdir/repo/LICENSE" "$destination/LICENSE"

echo "Copied test suite at $(git -C "$workdir/repo" rev-parse HEAD)"
