#!/usr/bin/env bash
set -euo pipefail

# Synthetic CI-only credentials. Never use production data.
if [[ -e deploy/.env ]]; then
  echo 'Refusing to overwrite an existing deploy/.env in the CI workspace.' >&2
  exit 1
fi
cp deploy/.env.example deploy/.env
password="$(openssl rand -hex 32)"
sed -i "s/^POSTGRES_PASSWORD=$/POSTGRES_PASSWORD=$password/" deploy/.env
unset password
chmod 600 deploy/.env

compose=(docker compose --project-name learnpip-ci --env-file deploy/.env --file deploy/compose.yaml)
second_override=$(mktemp)
cat > "$second_override" <<'SECOND'
services:
  db:
    ports: !override ["127.0.0.1:55432:5432"]
  api:
    ports: !override ["127.0.0.1:8081:8080"]
  web:
    ports: !override ["127.0.0.1:4201:80"]
SECOND
second=(docker compose --project-name learnpip-exchange-ci --env-file deploy/.env --file deploy/compose.yaml --file "$second_override")
legacy_source=$(mktemp -d)
legacy_override=$(mktemp)
legacy=(docker compose --project-name learnpip-ci --env-file deploy/.env --file deploy/compose.yaml --file "$legacy_override")
cleanup() {
  result=$?
  trap - EXIT
  if (( result != 0 )); then
    echo 'Integration smoke failed: container status and diagnostic logs:' >&2
    "${compose[@]}" ps || true
    "${compose[@]}" logs --tail 150 db migrate api web || true
  fi
  "${second[@]}" down --volumes --remove-orphans || true
  rm -f "$second_override"
  rm -rf "$legacy_source"
  rm -f "$legacy_override"
  docker image rm learnpip-catalog-legacy-ci >/dev/null 2>&1 || true
  "${compose[@]}" down --volumes --remove-orphans || true
  rm -f deploy/.env
  exit "$result"
}
trap cleanup EXIT

"${compose[@]}" up --build --detach --wait db api web
for attempt in $(seq 1 30); do
  if curl --fail --silent --show-error http://127.0.0.1:8080/health/ready >/dev/null &&
     curl --fail --silent --show-error http://127.0.0.1:4200/ >/dev/null; then
    break
  fi
  if [ "$attempt" -eq 30 ]; then
    "${compose[@]}" ps
    "${compose[@]}" logs --tail 100
    echo 'Integrated API and web did not become healthy.' >&2
    exit 1
  fi
  sleep 2
done

# An unknown API path returns 404 from ASP.NET, not 502/503/504 from nginx:
# this checks the actual reverse-proxy path rather than just two isolated ports.
status="$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' \
  http://127.0.0.1:4200/api/v1/ci-smoke-unknown-route)"
if [ "$status" != 404 ]; then
  echo "Expected proxied API 404, received HTTP $status" >&2
  exit 1
fi
echo 'Integrated PostgreSQL migration, API, Angular host and API proxy passed.'

# Independent networks and volumes; the only transferred object is the local ZIP.
"${second[@]}" up --build --detach --wait db api web
for attempt in $(seq 1 30); do
  if curl --fail --silent --show-error http://127.0.0.1:8081/health/ready >/dev/null &&
     curl --fail --silent --show-error http://127.0.0.1:4201/ >/dev/null; then
    break
  fi
  if [ "$attempt" -eq 30 ]; then
    echo 'Second independent stack did not become healthy.' >&2
    exit 1
  fi
  sleep 2
done
python3 tests/ops/catalog-two-instances.py http://127.0.0.1:4200 http://127.0.0.1:4201

# Pin the actual released old application, not a rewritten current fixture.
# Only its API replaces the first stack; the second remains the current app.
legacy_commit=719d82a6cd9072242ed3726ff5829bd303256683
git cat-file -e "$legacy_commit^{commit}"
git archive "$legacy_commit" | tar -x -C "$legacy_source"
docker build --file "$legacy_source/backend/Dockerfile" --tag learnpip-catalog-legacy-ci "$legacy_source"
cat > "$legacy_override" <<'LEGACY'
services:
  api:
    image: learnpip-catalog-legacy-ci
LEGACY
"${legacy[@]}" up --no-build --detach --wait api
for attempt in $(seq 1 30); do
  if curl --fail --silent http://127.0.0.1:8080/health/ready >/dev/null; then
    break
  fi
  if [ "$attempt" -eq 30 ]; then
    echo 'Legacy exporter API did not become healthy.' >&2
    exit 1
  fi
  sleep 2
done
python3 tests/ops/catalog-two-instances.py http://127.0.0.1:8080 http://127.0.0.1:8081 0.2.0
echo 'Released old-app ZIP imported idempotently by current app with media, IDs and rights intact.'
