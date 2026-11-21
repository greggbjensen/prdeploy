#!/usr/bin/env bash
# Tests deploy-tags.sh by running it with each kind of release tag and checking the tags it outputs.
set -uo pipefail
script="$(cd "$(dirname "$0")/.." && pwd)/deploy-tags.sh"
failures=0

# Runs the script with a release tag and prints the `tags` output.
tags_for() {
  local output
  output=$(mktemp)
  REPOSITORY="org/app" RELEASE_TAG="$1" GITHUB_OUTPUT="$output" "$script" > /dev/null || return 1
  sed -n 's/^tags=//p' "$output"
}

# Compares an actual value with the expected one and records a failure.
assert_eq() {
  if [ "$2" == "$3" ]; then echo "ok - $1"; else echo "not ok - $1: expected '$3' but was '$2'"; failures=$((failures + 1)); fi
}

assert_eq "non-release builds use temp" "$(tags_for temp)" "temp"
assert_eq "stable release gets patch, minor, major, and latest" "$(tags_for 1.2.15)" \
  "org/app:1.2.15, org/app:1.2, org/app:1, org/app:latest"
assert_eq "v prefix is kept on every version tag" "$(tags_for v2.0.3)" \
  "org/app:v2.0.3, org/app:v2.0, org/app:v2, org/app:latest"
assert_eq "pre-release does not move latest or stable lines" "$(tags_for 1.3.0-beta.1)" "org/app:1.3.0-beta.1"
assert_eq "non-semver tag is only tagged as itself" "$(tags_for 1)" "org/app:1"

if REPOSITORY="" RELEASE_TAG="1.0.0" GITHUB_OUTPUT=/dev/null "$script" 2> /dev/null; then
  echo "not ok - missing repository fails"; failures=$((failures + 1))
else
  echo "ok - missing repository fails"
fi

exit $failures
