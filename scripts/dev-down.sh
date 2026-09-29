#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
docker compose \
  --env-file "$repo_root/deploy/.env" \
  --file "$repo_root/deploy/compose.yaml" \
  down
