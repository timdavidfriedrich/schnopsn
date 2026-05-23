#!/usr/bin/env bash
set -euo pipefail

# Every push to main builds artifacts. A Play Store upload only happens when a
# commit since the last v* tag is prefixed [RELEASE] (minor bump) or
# [PATCH]/[HOTFIX] (patch bump). versionCode is the total commit count on the
# branch so each build still gets a unique, monotonically increasing code for
# sideload testing.
#
# Emits version_name, version_code, tag, should_upload, bump to $GITHUB_OUTPUT
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
    if [[ "$line" == *"[RELEASE]"* ]]; then
        bump="minor"
        break
    elif [[ ("$line" == *"[PATCH]"* || "$line" == *"[HOTFIX]"*) && "$bump" == "none" ]]; then
        bump="patch"
    fi
done <<< "$commits"

current="${last_tag#v}"
IFS='.' read -r major minor patch <<< "$current"
major=${major:-0}; minor=${minor:-0}; patch=${patch:-0}

case "$bump" in
    minor) minor=$((minor + 1)); patch=0 ;;
    patch) patch=$((patch + 1)) ;;
    none)  : ;;
esac

version_name="${major}.${minor}.${patch}"
version_code=$(git rev-list --count HEAD)
tag="v${version_name}"

should_upload="true"
[[ "$bump" == "none" ]] && should_upload="false"

notes_file="${RELEASE_NOTES_PATH:-release-notes.md}"
if [[ -n "$commits" ]]; then
    echo "$commits" | sed 's/^/- /' > "$notes_file"
else
    echo "Maintenance release." > "$notes_file"
fi

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
    echo "should_upload=${should_upload}"
    echo "bump=${bump}"
} >> "$out"

if [[ "$out" == "/dev/stdout" ]]; then
    echo "---"
    echo "Last tag:       ${last_tag}"
    echo "Bump:           ${bump}"
    echo "Next tag:       ${tag}"
    echo "versionName:    ${version_name}"
    echo "versionCode:    ${version_code}"
    echo "Upload to Play: ${should_upload}"
    echo "Release notes:  ${notes_file}"
fi
