#!/usr/bin/env bash
set -euo pipefail

version="${1:?Usage: scripts/package-release.sh VERSION}"
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] || {
  echo 'Invalid release version' >&2
  exit 1
}
[[ "${GITHUB_REPOSITORY:-}" == 'kennfarbe/LearnPip' ]] || {
  echo 'Release images may only be published from kennfarbe/LearnPip' >&2
  exit 1
}

for service in api worker web; do
  case "$service" in
    api) dockerfile=backend/Dockerfile ;;
    worker) dockerfile=backend/src/LearnPip.Worker/Dockerfile ;;
    web) dockerfile=frontend/web/Dockerfile ;;
  esac
  docker buildx build --platform linux/amd64 --push \
    --label "org.opencontainers.image.source=https://github.com/kennfarbe/LearnPip" \
    --label "org.opencontainers.image.revision=${GITHUB_SHA:?}" \
    -f "$dockerfile" -t "ghcr.io/kennfarbe/learnpip-$service:$version" .
done

mkdir -p dist
tar -czf "dist/learnpip-install-v$version.tar.gz" \
  deploy/compose.release.yaml deploy/Caddyfile deploy/.env.production.example \
  scripts/prod-init.sh scripts/backup-prod.sh docs/ LICENSE
(cd dist && sha256sum "learnpip-install-v$version.tar.gz" > "learnpip-install-v$version.tar.gz.sha256")
