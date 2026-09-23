#!/usr/bin/env bash
# Print the docs/CHANGELOG.md section for a release version, followed by a link
# to the full changelog. Fails if the section is missing or empty, so a release
# cannot be packed without release notes.
#
# Usage: scripts/release-notes.sh VERSION [CHANGELOG]
set -euo pipefail

version="${1:?Usage: $0 VERSION [CHANGELOG]}"
changelog="${2:-docs/CHANGELOG.md}"
# Conservative cap (not a documented nuget.org limit): fail before approval rather
# than risk a rejected push after it.
max_length=30000

# Command substitution drops trailing newlines; sed drops leading blank lines.
notes="$(awk -v heading="## [${version}]" '
    index($0, heading) == 1 { found = 1; next }
    found && /^## \[/ { exit }
    found { print }
' "$changelog" | sed -e '/./,$!d')"

if [[ -z "${notes//[[:space:]]/}" ]]; then
    printf 'No "## [%s]" section with content found in %s.\n' "$version" "$changelog" >&2
    exit 1
fi

# Reference-style links such as [#232][i232] are defined at the end of the
# changelog; copy the definitions this section uses so the links still resolve.
definitions=""
while IFS= read -r label; do
    definition="$(grep -F -m 1 "[${label}]: " "$changelog" || true)"
    if [[ -n "$definition" ]]; then
        definitions+="${definition}"$'\n'
    fi
done < <(grep -o '\]\[[^]]*\]' <<< "$notes" | sed -e 's/^\]\[//' -e 's/\]$//' | sort -u)

output="${notes}"$'\n\n'"Full changelog: https://github.com/fszlin/certes/blob/v${version}/docs/CHANGELOG.md"
if [[ -n "$definitions" ]]; then
    output+=$'\n\n'"${definitions%$'\n'}"
fi

if (( ${#output} > max_length )); then
    printf 'Release notes for %s are %s characters; shorten them below %s.\n' \
        "$version" "${#output}" "$max_length" >&2
    exit 1
fi

printf '%s\n' "$output"
