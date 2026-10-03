#!/usr/bin/env bash
set -euo pipefail

# Synthetic CI-only credentials. Never use production data.
cp deploy/.env.example deploy/.env
printf '\nPOSTGRES_PASSWORD=%s\n' "$(openssl rand -hex 32)" >> deploy/.env
chmod 600 deploy/.env

compose=(docker compose --project-name learnpip-ci --env-file deploy/.env --file deploy/compose.yaml)
cleanup() {
  "${compose[@]}" down --volumes --remove-orphans || true
  rm -f deploy/.env
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
