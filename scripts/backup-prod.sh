#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
backup_dir="${LEARNPIP_BACKUP_DIR:-${HOME}/learnpip-backups}"
umask 077
mkdir -p "$backup_dir"
backup_dir="$(cd "$backup_dir" && pwd)"
temporary="$(mktemp "$backup_dir/.learnpip-backup-XXXXXXXX.dump")"
trap 'rm -f "$temporary"' EXIT

docker compose --env-file "$repo_root/deploy/.env.production" \
  --file "$repo_root/deploy/compose.prod.yaml" \
  exec -T db sh -c 'pg_dump --format=custom --username "$POSTGRES_USER" "$POSTGRES_DB"' \
  > "$temporary"

stamp="$(date -u +%Y%m%dT%H%M%SZ)"
mv "$temporary" "$backup_dir/learnpip-$stamp.dump"
trap - EXIT
find "$backup_dir" -maxdepth 1 -type f -name 'learnpip-*.dump' -mmin +43200 -delete
