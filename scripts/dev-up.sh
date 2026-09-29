#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
env_file="$repo_root/deploy/.env"

if [[ ! -f "$env_file" ]]; then
  cp "$repo_root/deploy/.env.example" "$env_file"
  chmod 600 "$env_file"
fi

if ! grep -q '^POSTGRES_PASSWORD=.' "$env_file"; then
  password="$(openssl rand -hex 32)"
  if grep -q '^POSTGRES_PASSWORD=' "$env_file"; then
    temporary_file="${env_file}.tmp"
    awk -v password="$password" '
      /^POSTGRES_PASSWORD=/ { print "POSTGRES_PASSWORD=" password; replaced = 1; next }
      { print }
      END { if (!replaced) print "POSTGRES_PASSWORD=" password }
    ' "$env_file" > "$temporary_file"
    mv "$temporary_file" "$env_file"
  else
    printf '\nPOSTGRES_PASSWORD=%s\n' "$password" >> "$env_file"
  fi
fi
chmod 600 "$env_file"

docker compose \
  --env-file "$env_file" \
  --file "$repo_root/deploy/compose.yaml" \
  up --build --detach

printf 'LearnPip is starting. Web: http://localhost:4200  API health: http://localhost:8080/health/live\n'
