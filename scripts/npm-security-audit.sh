#!/usr/bin/env bash
# Keep advisory details in runner-local files. Never echo or upload raw reports.
set -euo pipefail
directory="${1:?package directory required}"
label="${2:?safe report label required}"
case "$label" in root|web) ;; *) exit 2 ;; esac
mode="${3:-production}"
case "$mode" in production|development) ;; *) exit 2 ;; esac
private_dir="${RUNNER_TEMP:-${TMPDIR:-/tmp}}/learnpip-security-$label-$mode"
mkdir -p "$private_dir"
chmod 700 "$private_dir"
report="$private_dir/npm-audit-private.json"
audit_args=(--json)
if [ "$mode" = production ]; then
  audit_args+=(--omit=dev)
fi
status=0
(cd "$directory" && npm audit "${audit_args[@]}") > "$report" 2> "$private_dir/npm-audit-stderr.log" || status=$?
if (( status > 1 )); then
  echo "$label: npm audit failed to complete; inspect privately." >&2
  exit 2
fi
result=0
node scripts/security-report.mjs npm "$report" || result=$?
if [ "$result" -eq 0 ]; then
  echo "$label: dependency audit passed."
  exit 0
fi
if [ "$result" -ne 1 ]; then
  echo "$label: audit report invalid; scanner failure is blocking." >&2
  exit 2
fi
if [ "$mode" = development ]; then
  echo "$label: development findings are informational; review uploaded report for updates."
  exit 0
fi
# Try npm's non-force fixes in a disposable directory; NEVER modify tracked files
# or install packages/run lifecycle scripts in the CI checkout.
trial="$private_dir/fix-trial"
mkdir -p "$trial"
cp "$directory/package.json" "$directory/package-lock.json" "$trial/"
fix_status=0
(cd "$trial" && npm audit fix --package-lock-only --ignore-scripts --no-fund --json) > "$private_dir/npm-fix-private.json" 2> "$private_dir/npm-fix-stderr.log" || fix_status=$?
if (( fix_status > 1 )); then
  echo "$label: automatic non-force fix could not complete; private triage required." >&2
  exit 1
fi
post_status=0
(cd "$trial" && npm audit "${audit_args[@]}") > "$private_dir/npm-after-fix-private.json" 2> "$private_dir/npm-after-fix-stderr.log" || post_status=$?
if (( post_status > 1 )); then
  echo "$label: post-fix audit incomplete; private triage required." >&2
  exit 1
fi
if node scripts/security-report.mjs npm "$private_dir/npm-after-fix-private.json"; then
  if ! cmp -s "$directory/package-lock.json" "$trial/package-lock.json" || ! cmp -s "$directory/package.json" "$trial/package.json"; then
    echo "$label: safe npm audit fix is available. Commit the reviewed package and lockfile updates; checkout was not changed." >&2
  else
    echo "$label: audit succeeded only in disposable trial; investigate discrepancy privately." >&2
  fi
else
  echo "$label: unresolved findings remain after non-force fixes; inspect privately and evaluate dependency replacement. Ask project owner before exceptions." >&2
fi
# Findings in the committed lockfile block merging even if a scratch fix exists.
exit 1
