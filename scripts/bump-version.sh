#!/usr/bin/env bash
set -euo pipefail

# Parses commits since the last v* tag and decides the semver bump:
#   [BREAKING] -> major, [ADD] -> minor, [FIX] -> patch.
# Emits version_name, version_code, tag, has_release to $GITHUB_OUTPUT
# (or stdout when run locally) and writes release-notes.md.

last_tag=$(git describe --tags --abbrev=0 --match 'v*' 2>/dev/null || echo "v0.0.0")
range="${last_tag}..HEAD"
if [[ "$last_tag" == "v0.0.0" ]] && ! git rev-parse -q --verify "$last_tag" >/dev/null 2>&1; then
    range="HEAD"
fi

commits=$(git log "$range" --pretty=format:'%s' || true)

bump="none"
while IFS= read -r line; do
    [[ -z "$line" ]] && continue
    if [[ "$line" == *"[BREAKING]"* ]]; then
        bump="major"
        break
    elif [[ "$line" == *"[ADD]"* && "$bump" != "major" ]]; then
        bump="minor"
    elif [[ "$line" == *"[FIX]"* && "$bump" == "none" ]]; then
        bump="patch"
    fi
done <<< "$commits"

current="${last_tag#v}"
IFS='.' read -r major minor patch <<< "$current"
major=${major:-0}; minor=${minor:-0}; patch=${patch:-0}

case "$bump" in
    major) major=$((major + 1)); minor=0; patch=0 ;;
    minor) minor=$((minor + 1)); patch=0 ;;
    patch) patch=$((patch + 1)) ;;
    none)  : ;;
esac

version_name="${major}.${minor}.${patch}"
version_code=$(git rev-list --count HEAD)
tag="v${version_name}"

has_release="true"
[[ "$bump" == "none" ]] && has_release="false"

# Build changelog grouped by section, capped at 500 chars for Play Store.
notes_file="${RELEASE_NOTES_PATH:-release-notes.md}"
{
    added=$(echo "$commits"   | grep -E '\[ADD\]'       | sed -E 's/\[ADD\][[:space:]]*//'      || true)
    fixed=$(echo "$commits"   | grep -E '\[FIX\]'       | sed -E 's/\[FIX\][[:space:]]*//'      || true)
    breaking=$(echo "$commits" | grep -E '\[BREAKING\]' | sed -E 's/\[BREAKING\][[:space:]]*//' || true)

    [[ -n "$breaking" ]] && { echo "Breaking:"; echo "$breaking" | sed 's/^/- /'; echo; }
    [[ -n "$added"    ]] && { echo "Added:";    echo "$added"    | sed 's/^/- /'; echo; }
    [[ -n "$fixed"    ]] && { echo "Fixed:";    echo "$fixed"    | sed 's/^/- /'; echo; }
} > "$notes_file"

if [[ $(wc -c < "$notes_file") -gt 500 ]]; then
    head -c 497 "$notes_file" > "${notes_file}.tmp"
    echo "..." >> "${notes_file}.tmp"
    mv "${notes_file}.tmp" "$notes_file"
fi

out="${GITHUB_OUTPUT:-/dev/stdout}"
{
    echo "version_name=${version_name}"
    echo "version_code=${version_code}"
    echo "tag=${tag}"
    echo "has_release=${has_release}"
    echo "bump=${bump}"
} >> "$out"

if [[ "$out" == "/dev/stdout" ]]; then
    echo "---"
    echo "Last tag:   ${last_tag}"
    echo "Bump:       ${bump}"
    echo "New tag:    ${tag}"
    echo "versionName=${version_name}  versionCode=${version_code}"
    echo "Release notes written to: ${notes_file}"
fi
