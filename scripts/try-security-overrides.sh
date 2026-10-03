#!/usr/bin/env bash
# Experiment with targeted transitive fixes in a disposable copy.
set -euo pipefail
base="${RUNNER_TEMP:?}/learnpip-override-trial"
mkdir -p "$base"
cp package.json package-lock.json "$base/"
python3 - "$base/package.json" <<'PY'
import json
from pathlib import Path
import sys
path = Path(sys.argv[1])
data = json.loads(path.read_text(encoding="utf-8"))
# Minimum versions follow the actual audit advisory ranges; they are not blanket downgrades.
data["overrides"] = {
    "brace-expansion": "^5.0.12",
    "undici": "^6.28.1",
}
path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")
PY
set +e
(cd "$base" && npm install --package-lock-only --ignore-scripts --no-fund --no-audit > "$base/npm-resolution.log" 2>&1)
resolution=$?
set -e
if [ "$resolution" -ne 0 ]; then
  echo "Override candidate could not be resolved; inspect candidate artifact."
  exit 0
fi
set +e
(cd "$base" && npm audit --json > "$base/override-audit.json" 2> "$base/npm-audit-stderr.log")
scan=$?
set -e
if [ "$scan" -gt 1 ]; then
  echo "Override candidate audit incomplete."
else
  python3 scripts/security-report.py npm "$base/override-audit.json" || true
fi
echo "Override trial is diagnostic only. Existing package files remain untouched."
