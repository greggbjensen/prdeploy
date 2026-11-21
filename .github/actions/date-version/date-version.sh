#!/usr/bin/env bash
# Writes a date-based build version to $GITHUB_OUTPUT as `version` and to $GITHUB_ENV as `VERSION`.
#
# Inputs (environment):
#   RUN_ID           Workflow run ID, used as the pre-release suffix.
#   USE_PRERELEASE   `true` (default) for YYYY.M.D-r<RUN_ID>.
#   USE_FOUR_DIGITS  `true` for a four-part version; takes precedence over USE_PRERELEASE.
#   DATE_VERSION_EPOCH  Optional Unix time to use instead of now, for tests.
set -euo pipefail

# Formats the build time, using DATE_VERSION_EPOCH when it is set (GNU date first, then BSD date).
format_date() {
  if [ -n "${DATE_VERSION_EPOCH:-}" ]; then
    date -u -d "@$DATE_VERSION_EPOCH" "$1" 2>/dev/null || date -u -r "$DATE_VERSION_EPOCH" "$1"
  else
    date "$1"
  fi
}

if [ "${USE_FOUR_DIGITS:-false}" = "true" ]; then
  version=$(format_date '+%Y.%-m.%-d%-H.%-M%-S')
elif [ "${USE_PRERELEASE:-true}" = "true" ]; then
  version="$(format_date '+%Y.%-m.%-d')-r${RUN_ID:?RUN_ID is required for a pre-release version}"
else
  # Each version part must be at most 65,535, so the seconds become tenths of a minute (0-9).
  seconds=$(format_date '+%-S')
  minute10th=$(( (seconds * 9 + 30) / 60 ))
  version="$(format_date '+%Y.%-m%-d.%-H%-M')$minute10th"
fi

echo "VERSION: $version"
echo "version=$version" >> "$GITHUB_OUTPUT"
echo "VERSION=$version" >> "$GITHUB_ENV"
