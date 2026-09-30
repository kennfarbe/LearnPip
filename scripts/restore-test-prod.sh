#!/usr/bin/env bash
# Restore a production dump into a disposable, isolated PostgreSQL container.
set -euo pipefail

if [[ $# -ne 1 || ! -f "$1" ]]; then
  echo 'Usage: scripts/restore-test-prod.sh /absolute/path/learnpip-YYYYMMDDTHHMMSSZ.dump' >&2
  exit 2
fi
backup_file="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
manifest="${backup_file%.dump}.manifest"
if [[ ! -f "$manifest" ]]; then
  echo "Missing backup manifest: $manifest" >&2
  exit 2
fi
umask 077
expected_sha="$(sed -n 's/^sha256=//p' "$manifest")"
actual_sha="$(sha256sum "$backup_file" | cut -d ' ' -f 1)"
if [[ ! "$expected_sha" =~ ^[0-9a-f]{64}$ || "$expected_sha" != "$actual_sha" ]]; then
  echo 'Backup SHA-256 mismatch; restore refused.' >&2
  exit 1
fi
image="$(sed -n 's/^postgres_image=//p' "$manifest")"
if [[ ! "$image" =~ ^postgres:[0-9]+-alpine$ ]]; then
  echo 'Unsupported PostgreSQL image in manifest.' >&2
  exit 1
fi
container="learnpip-restore-test-$$"
password="$(openssl rand -hex 32)"
cleanup() { docker rm -f "$container" >/dev/null 2>&1 || true; }
trap cleanup EXIT
docker run -d --name "$container" --network none \
  -e POSTGRES_DB=learnpip -e POSTGRES_USER=learnpip \
  -e "POSTGRES_PASSWORD=$password" "$image" >/dev/null
ready=0
for ((attempt=0; attempt<60; attempt++)); do
  if docker exec "$container" pg_isready -U learnpip -d learnpip >/dev/null 2>&1; then
    ready=1
    break
  fi
  sleep 1
done
if [[ "$ready" -ne 1 ]]; then
  echo 'Isolated PostgreSQL did not become ready.' >&2
  exit 1
fi
# No host port or shared volume; data is wiped when the container is removed.
docker exec -i "$container" pg_restore --exit-on-error --no-owner \
  -U learnpip -d learnpip < "$backup_file"
for table in MediaBlobs StudyAttempts; do
  expected="$(sed -n "s/^${table}=//p" "$manifest")"
  if [[ ! "$expected" =~ ^[0-9]+$ ]]; then
    echo "Invalid $table count in manifest." >&2
    exit 1
  fi
  actual="$(docker exec "$container" psql -XAt -U learnpip -d learnpip \
    -c "SELECT count(*) FROM \"$table\"")"
  if [[ "$expected" != "$actual" ]]; then
    echo "$table count differs: backup=$expected restored=$actual" >&2
    exit 1
  fi
  echo "$table: $actual rows verified"
done
# Verify that the restored private media still contains actual bytes.
media_bytes="$(docker exec "$container" psql -XAt -U learnpip -d learnpip \
  -c 'SELECT coalesce(sum(octet_length("Data")), 0) FROM "MediaBlobs"')"
expected_bytes="$(sed -n 's/^media_bytes=//p' "$manifest")"
if [[ ! "$expected_bytes" =~ ^[0-9]+$ || "$expected_bytes" != "$media_bytes" ]]; then
  echo 'Private media byte count differs after restore.' >&2
  exit 1
fi
printf 'Restore verified: private media bytes=%s; learning attempts preserved.\n' "$media_bytes"
