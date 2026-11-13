#!/usr/bin/env bash
# Tests for resolve-version.sh. Run from anywhere: .github/scripts/test-resolve-version.sh
set -uo pipefail

here=$(cd "$(dirname "$0")" && pwd)
script="$here/resolve-version.sh"
sandbox=$(mktemp -d)
trap 'rm -rf "$sandbox"' EXIT

passed=0
failed=0

# check <description> <expected stdout | FAIL> <stdin> <resolve-version.sh args...>
check() {
  local desc=$1 want=$2 stdin=$3
  shift 3
  local got rc=0
  got=$(printf '%s' "$stdin" | "$script" "$@" 2>/dev/null) || rc=$?

  if [[ $want == FAIL ]]; then
    if [[ $rc -ne 0 && -z $got ]]; then
      passed=$((passed + 1))
    else
      failed=$((failed + 1))
      echo "FAIL: $desc: expected an error, got exit $rc and output '$got'"
    fi
  elif [[ $rc -eq 0 && $got == "$want" ]]; then
    passed=$((passed + 1))
  else
    failed=$((failed + 1))
    echo "FAIL: $desc: expected '$want', got exit $rc and output '$got'"
  fi
}

# --- tag: validates pushed tags ---
check "stable tag" 1.2.3 "" tag v1.2.3
check "zero version" 0.0.1 "" tag v0.0.1
check "multi-digit parts" 10.20.30 "" tag v10.20.30
check "prerelease" 1.2.3-rc.1 "" tag v1.2.3-rc.1
check "hyphen and numeric prerelease identifiers" 10.20.30-alpha-1.0 "" tag v10.20.30-alpha-1.0
check "prerelease identifier starting with a digit" 1.0.0-0a "" tag v1.0.0-0a

check "missing argument" FAIL "" tag
check "empty tag" FAIL "" tag ""
check "missing patch" FAIL "" tag v1.2
check "missing v prefix" FAIL "" tag 1.2.3
check "uppercase V" FAIL "" tag V1.2.3
check "leading zero in major" FAIL "" tag v01.2.3
check "leading zero in patch" FAIL "" tag v1.2.03
check "build metadata" FAIL "" tag v1.2.3+build.5
check "empty prerelease" FAIL "" tag v1.2.3-
check "empty prerelease identifier" FAIL "" tag v1.2.3-rc..1
check "trailing dot in prerelease" FAIL "" tag v1.2.3-rc.1.
check "numeric prerelease with leading zero" FAIL "" tag v1.2.3-01
check "numeric prerelease identifier with leading zero" FAIL "" tag v1.2.3-rc.01
check "space in prerelease" FAIL "" tag "v1.2.3-rc 1"
check "trailing space" FAIL "" tag "v1.2.3 "
check "shell metacharacters" FAIL "" tag 'v1.2.3;rm -rf x'
check "command substitution" FAIL "" tag "v1.2.3\$(touch $sandbox/pwned)"
check "backticks" FAIL "" tag "v1.2.3\`touch $sandbox/pwned\`"
[[ ! -e $sandbox/pwned ]] || {
  failed=$((failed + 1))
  echo "FAIL: tag input was executed as a command"
}

# --- bump: next version from existing tags ---
check "no tags: patch" 0.0.1 "" bump patch
check "no tags: minor" 0.1.0 "" bump minor
check "no tags: major" 1.0.0 "" bump major

tags=$'v1.2.3\nv1.10.0\nv1.9.9\nv2.0.0-rc.1\nrelease-2019\nlatest\n1.5.0\nv3.0\nv01.2.3\nxv1.2.4-rc.1'
check "patch uses the highest stable tag (numeric sort)" 1.10.1 "$tags" bump patch
check "minor" 1.11.0 "$tags" bump minor
check "major ignores prerelease tags" 2.0.0 "$tags" bump major
check "prerelease on minor" 1.11.0-rc.1 "$tags" bump minor rc.1
check "prerelease with hyphen" 1.10.1-beta-2 "$tags" bump patch beta-2
check "next prerelease of the same version" 2.0.0-rc.2 "$tags" bump major rc.2
check "tag that only contains the version as a suffix is not a collision" 1.2.4-rc.1 $'v1.2.3\nxv1.2.4-rc.1' bump patch rc.1

check "existing prerelease tag" FAIL "$tags" bump major rc.1
check "only prerelease tags: base stays 0.0.0" 0.0.1 $'v1.0.0-rc.1' bump patch

check "invalid prerelease: empty identifier" FAIL "$tags" bump patch rc..1
check "invalid prerelease: leading zero" FAIL "$tags" bump patch 01
check "invalid prerelease: underscore" FAIL "$tags" bump patch rc_1
check "invalid prerelease: space" FAIL "$tags" bump patch "rc 1"
check "invalid prerelease: build metadata" FAIL "$tags" bump patch +build
check "unknown bump kind" FAIL "$tags" bump huge
check "missing bump kind" FAIL "$tags" bump
check "unknown command" FAIL "" frobnicate

echo "$passed passed, $failed failed"
[[ $failed -eq 0 ]]
