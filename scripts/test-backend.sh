#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
env_file="$repo_root/deploy/.env"

if [[ ! -f "$env_file" ]]; then
  printf 'Run ./scripts/dev-up.sh once to create the local database configuration.\n' >&2
  exit 1
fi

docker compose \
  --env-file "$env_file" \
  --file "$repo_root/deploy/compose.yaml" \
  up --detach db

set -a
# The ignored local .env is created by dev-up.sh and contains only local credentials.
source "$env_file"
set +a

export ConnectionStrings__LearnPip="Host=127.0.0.1;Port=5432;Database=$POSTGRES_DB;Username=$POSTGRES_USER;Password=$POSTGRES_PASSWORD"
cd "$repo_root"
dotnet test backend/LearnPip.sln --configuration Release
