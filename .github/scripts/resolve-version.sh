#!/usr/bin/env bash
# Resolves the Semantic Version (https://semver.org) used by the release workflow.
#
#   resolve-version.sh tag  <tag>                         validate a pushed tag (vMAJOR.MINOR.PATCH[-prerelease])
#   resolve-version.sh bump <major|minor|patch> [pre]     next version after the highest stable tag read from stdin
#
# Prints the version (no leading "v") on stdout. Problems go to stderr with exit code 1.
# Build metadata (+build) is intentionally unsupported: it cannot be part of a git tag name portably
# and the Android/macOS version fields cannot carry it.
set -euo pipefail
export LC_ALL=C

NUM='(0|[1-9][0-9]*)'
PRE_ID='(0|[1-9][0-9]*|[0-9]*[a-zA-Z-][0-9a-zA-Z-]*)'
SEMVER_RE="^${NUM}\\.${NUM}\\.${NUM}(-${PRE_ID}(\\.${PRE_ID})*)?\$"
STABLE_TAG_RE="^v${NUM}\\.${NUM}\\.${NUM}\$"

die() {
  echo "error: $*" >&2
  exit 1
}

is_semver() {
  [[ $1 =~ $SEMVER_RE ]]
}

cmd_tag() {
  local tag=${1:-}
  [[ $tag == v* ]] || die "tag $(printf '%q' "$tag") must look like vMAJOR.MINOR.PATCH[-prerelease]"
  local version=${tag#v}
  is_semver "$version" || die "tag $(printf '%q' "$tag") is not a valid semantic version (expected vMAJOR.MINOR.PATCH[-prerelease])"
  echo "$version"
}

cmd_bump() {
  local kind=${1:-} pre=${2:-}
  case $kind in
    major | minor | patch) ;;
    *) die "bump must be one of: major, minor, patch" ;;
  esac

  local tags
  tags=$(cat)

  # Prereleases never move the base version, so rc.1 -> rc.2 -> final all share one base.
  local latest
  latest=$(printf '%s\n' "$tags" | grep -E "$STABLE_TAG_RE" | sed 's/^v//' | sort -t. -k1,1n -k2,2n -k3,3n | tail -n 1 || true)

  local major minor patch
  IFS=. read -r major minor patch <<<"${latest:-0.0.0}"
  case $kind in
    major) major=$((major + 1)) minor=0 patch=0 ;;
    minor) minor=$((minor + 1)) patch=0 ;;
    patch) patch=$((patch + 1)) ;;
  esac

  local version="$major.$minor.$patch"
  [[ -z $pre ]] || version="$version-$pre"
  is_semver "$version" || die "invalid prerelease $(printf '%q' "$pre") (use dot-separated identifiers of [0-9A-Za-z-], e.g. rc.1)"

  if printf '%s\n' "$tags" | grep -Fxq "v$version"; then
    die "tag v$version already exists"
  fi
  echo "$version"
}

case ${1:-} in
  tag)
    shift
    cmd_tag "$@"
    ;;
  bump)
    shift
    cmd_bump "$@"
    ;;
  *) die "usage: $0 tag <tag> | bump <major|minor|patch> [prerelease]" ;;
esac
