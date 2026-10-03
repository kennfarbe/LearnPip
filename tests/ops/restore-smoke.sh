#!/usr/bin/env bash
# Synthetic fixture: verifies the production restore path without real user data.
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
fixture="learnpip-restore-fixture-$$"
work="$(mktemp -d)"
cleanup() {
  docker rm -f "$fixture" >/dev/null 2>&1 || true
  rm -rf "$work"
}
trap cleanup EXIT
password="$(openssl rand -hex 24)"
db_image=kennfarbe/learnpip:db-v0.0.0
docker build -f deploy/postgres.Dockerfile -t "$db_image" .
docker run -d --name "$fixture" --network none \
  -e POSTGRES_DB=learnpip -e POSTGRES_USER=learnpip \
  -e "POSTGRES_PASSWORD=$password" "$db_image" >/dev/null
ready=0
for ((attempt=0; attempt<60; attempt++)); do
  if docker exec "$fixture" psql -XAt -U learnpip -d learnpip -c "SELECT 1" >/dev/null 2>&1; then
    ready=1
    break
  fi
  sleep 1
done
[[ "$ready" -eq 1 ]]
docker exec -i "$fixture" psql -Xv ON_ERROR_STOP=1 -U learnpip -d learnpip <<'SQL'
CREATE TABLE "MediaBlobs" ("Data" bytea NOT NULL);
CREATE TABLE "StudyAttempts" ("Id" integer NOT NULL);
INSERT INTO "MediaBlobs" VALUES (decode('89504e470d0a1a0a', 'hex'));
INSERT INTO "StudyAttempts" VALUES (1);
SQL
backup="$work/learnpip-fixture.dump"
docker exec "$fixture" pg_dump -Fc -U learnpip learnpip > "$backup"
{
  printf 'sha256=%s\n' "$(sha256sum "$backup" | cut -d ' ' -f 1)"
  printf 'postgres_image=%s\nMediaBlobs=1\nStudyAttempts=1\nmedia_bytes=8\n' "$db_image"
} > "$work/learnpip-fixture.manifest"
"$repo_root/scripts/restore-test-prod.sh" "$backup"
printf 'corrupt' >> "$backup"
if "$repo_root/scripts/restore-test-prod.sh" "$backup" >/dev/null 2>&1; then
  echo 'Corrupted backup passed integrity check.' >&2
  exit 1
fi
