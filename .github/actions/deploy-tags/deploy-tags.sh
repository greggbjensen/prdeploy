#!/usr/bin/env bash
# Writes the Docker image tags for a release to $GITHUB_OUTPUT as `tags`.
#
# Inputs (environment):
#   REPOSITORY   Docker repository, such as greggbjensen/prdeploy-api.
#   RELEASE_TAG  Release tag name, or `temp` when the build is not a release.
#
# A stable release such as 1.2.15 (or v1.2.15) is tagged 1.2.15, 1.2, 1, and latest. Any other tag, including
# pre-releases such as 1.3.0-beta.1, is tagged only with itself so it never replaces latest or a stable line.
set -euo pipefail

if [ -z "${REPOSITORY:-}" ]; then
  echo "REPOSITORY is required." >&2
  exit 1
fi

release_tag="${RELEASE_TAG:-temp}"
if [ "$release_tag" = "temp" ]; then
  tags="temp"
elif [[ "$release_tag" =~ ^(v?)([0-9]+)\.([0-9]+)\.([0-9]+)$ ]]; then
  prefix="${BASH_REMATCH[1]}"
  major="${BASH_REMATCH[2]}"
  minor="${BASH_REMATCH[3]}"
  tags="$REPOSITORY:$release_tag, $REPOSITORY:$prefix$major.$minor, $REPOSITORY:$prefix$major, $REPOSITORY:latest"
else
  tags="$REPOSITORY:$release_tag"
fi

echo "tags=$tags" >> "$GITHUB_OUTPUT"
