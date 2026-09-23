#!/usr/bin/env bash
# Print the docs/CHANGELOG.md section for a release version, followed by a link
# to the full changelog. Fails if the section is missing or empty, so a release
# cannot be packed without release notes.
#
# Usage: scripts/release-notes.sh VERSION [CHANGELOG]
set -euo pipefail

version="${1:?Usage: $0 VERSION [CHANGELOG]}"
changelog="${2:-docs/CHANGELOG.md}"

notes="$(awk -v heading="## [${version}]" '
    index($0, heading) == 1 { found = 1; next }
    found && /^## \[/ { exit }
    found { print }
' "$changelog" | sed -e '/./,$!d' | sed -e ':a' -e '/^\n*$/{$d;N;ba' -e '}')"

if [[ -z "${notes//[[:space:]]/}" ]]; then
    printf 'No "## [%s]" section with content found in %s.\n' "$version" "$changelog" >&2
    exit 1
fi

printf '%s\n\nFull changelog: https://github.com/fszlin/certes/blob/v%s/docs/CHANGELOG.md\n' "$notes" "$version"
