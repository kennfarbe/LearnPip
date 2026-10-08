#!/usr/bin/env bash
# Geheimnisse dürfen auch bei Aufruf mit bash -x nicht verfolgt werden.
set +x
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
command -v python3 >/dev/null || { echo 'Python 3 wird für die Administrator-Einrichtung benötigt.' >&2; exit 1; }
exec python3 "$repo_root/scripts/setup-admin.py" "$@"
