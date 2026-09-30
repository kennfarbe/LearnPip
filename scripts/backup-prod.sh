#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
backup_dir="${LEARNPIP_BACKUP_DIR:-${HOME}/learnpip-backups}"
umask 077
mkdir -p "$backup_dir"
backup_dir="$(cd "$backup_dir" && pwd)"
temporary="$(mktemp "$backup_dir/.learnpip-backup-XXXXXXXX.dump")"
temporary_manifest="$(mktemp "$backup_dir/.learnpip-backup-XXXXXXXX.manifest")"
trap 'rm -f "$temporary" "$temporary_manifest"' EXIT

# The dump uses one consistent PostgreSQL snapshot. Capture expected row counts
# before and after; if live data changed in between, skip the manifest and retry.
query='SELECT (SELECT count(*) FROM "MediaBlobs") || chr(10) || (SELECT count(*) FROM "StudyAttempts") || chr(10) || (SELECT coalesce(sum(octet_length("Data")), 0) FROM "MediaBlobs")'
read_counts() {
  docker compose --env-file "$repo_root/deploy/.env.production" \
    --file "$repo_root/deploy/compose.prod.yaml" exec -T db \
    sh -c 'psql -XAt --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -c "$1"' sh "$query"
}
# These counts are operational smoke checks; concurrent writes can cause a
# false mismatch (retry the backup during a quiet period in that case).
counts="$(read_counts)"
mapfile -t expected < <(printf '%s\n' "$counts")
if [[ "${#expected[@]}" -ne 3 ]] ||
   [[ ! "${expected[0]}" =~ ^[0-9]+$ ]] ||
   [[ ! "${expected[1]}" =~ ^[0-9]+$ ]] ||
   [[ ! "${expected[2]}" =~ ^[0-9]+$ ]]; then
  echo 'Unable to record backup row counts.' >&2
  exit 1
fi
docker compose --env-file "$repo_root/deploy/.env.production" \
  --file "$repo_root/deploy/compose.prod.yaml" \
  exec -T db sh -c 'pg_dump --format=custom --username "$POSTGRES_USER" "$POSTGRES_DB"' \
  > "$temporary"
if [[ "$(read_counts)" != "$counts" ]]; then
  echo 'Rows changed during backup; retry during a quiet period.' >&2
  exit 1
fi
stamp="$(date -u +%Y%m%dT%H%M%SZ)"
{
  printf 'sha256=%s\n' "$(sha256sum "$temporary" | cut -d ' ' -f 1)"
  printf 'postgres_image=postgres:18-alpine\n'
  printf 'MediaBlobs=%s\nStudyAttempts=%s\nmedia_bytes=%s\n' \
    "${expected[0]}" "${expected[1]}" "${expected[2]}"
} > "$temporary_manifest"
mv "$temporary" "$backup_dir/learnpip-$stamp.dump"
mv "$temporary_manifest" "$backup_dir/learnpip-$stamp.manifest"
trap - EXIT
find "$backup_dir" -maxdepth 1 -type f \
  \( -name 'learnpip-*.dump' -o -name 'learnpip-*.manifest' \) -mmin +43200 -delete
