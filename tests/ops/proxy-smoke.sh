#!/usr/bin/env bash
set -euo pipefail

image=learnpip-proxy-ci
container="learnpip-proxy-ci-$$"
cleanup() {
  docker rm --force "$container" >/dev/null 2>&1 || true
}
trap cleanup EXIT

docker build -f deploy/caddy.Dockerfile -t "$image" .
for config in Caddyfile Caddyfile.internal; do
  docker run --rm --network none -e LEARNPIP_DOMAIN=localhost \
    -v "$PWD/deploy/$config:/etc/caddy/Caddyfile:ro" "$image" \
    caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile
done

docker run --detach --network none --name "$container" -e LEARNPIP_DOMAIN=localhost \
  -v "$PWD/deploy/Caddyfile.internal:/etc/caddy/Caddyfile:ro" "$image"
for attempt in $(seq 1 30); do
  if [ "$(docker exec "$container" wget -q -O - http://127.0.0.1:8081/healthz 2>/dev/null)" = ok ]; then
    echo 'Rebuilt Caddy validates both configurations and serves its health endpoint.'
    exit 0
  fi
  sleep 1
done
docker logs "$container"
echo 'Rebuilt Caddy did not become healthy.' >&2
exit 1
