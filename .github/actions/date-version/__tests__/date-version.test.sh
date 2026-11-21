#!/usr/bin/env bash
# Tests date-version.sh at fixed times and checks the version it writes to the step output and environment.
set -uo pipefail
script="$(cd "$(dirname "$0")/.." && pwd)/date-version.sh"
failures=0

# 2026-11-20T09:04:50Z (50 seconds is 7.5 tenths of a minute, which rounds to 8)
epoch=1795165490

# Runs the script with the given settings and prints the `version` output, checking VERSION matches it.
version_for() {
  local output env
  output=$(mktemp)
  env=$(mktemp)
  env DATE_VERSION_EPOCH="${EPOCH:-$epoch}" RUN_ID=42 USE_PRERELEASE="$1" USE_FOUR_DIGITS="$2" \
    GITHUB_OUTPUT="$output" GITHUB_ENV="$env" "$script" > /dev/null || return 1
  local version
  version=$(sed -n 's/^version=//p' "$output")
  [ "$(sed -n 's/^VERSION=//p' "$env")" == "$version" ] || { echo "VERSION env does not match"; return 1; }
  echo "$version"
}

# Compares an actual value with the expected one and records a failure.
assert_eq() {
  if [ "$2" == "$3" ]; then echo "ok - $1"; else echo "not ok - $1: expected '$3' but was '$2'"; failures=$((failures + 1)); fi
}

# Checks every numeric part of a version fits in 16 bits, the limit for .NET and NuGet version parts.
assert_parts_fit() {
  local part
  for part in ${2//[.-]/ }; do
    if [[ "$part" =~ ^[0-9]+$ ]] && [ "$part" -gt 65535 ]; then
      echo "not ok - $1: part $part of $2 exceeds 65535"; failures=$((failures + 1)); return
    fi
  done
  echo "ok - $1"
}

assert_eq "pre-release is the date with the run ID" "$(version_for true false)" "2026.11.20-r42"
assert_eq "release is date, month-day, and hour-minute with tenths of a minute" "$(version_for false false)" "2026.1120.948"
assert_eq "four digits takes precedence" "$(version_for true true)" "2026.11.209.450"
assert_eq "tenths of a minute round from seconds" "$(EPOCH=1795165440 version_for false false)" "2026.1120.940"

# Latest time in a year, which produces the largest parts.
assert_parts_fit "release parts fit at year end" "$(EPOCH=1798761599 version_for false false)"
assert_parts_fit "four-digit parts fit at year end" "$(EPOCH=1798761599 version_for true true)"

if env -u RUN_ID DATE_VERSION_EPOCH=$epoch USE_PRERELEASE=true GITHUB_OUTPUT=/dev/null GITHUB_ENV=/dev/null \
  "$script" > /dev/null 2>&1; then
  echo "not ok - pre-release without RUN_ID fails"; failures=$((failures + 1))
else
  echo "ok - pre-release without RUN_ID fails"
fi

exit $failures
