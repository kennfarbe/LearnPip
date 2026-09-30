#!/usr/bin/env bash
# Intended for a systemd timer or external scheduler, never exposed as HTTP.
set -euo pipefail
repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
config="$repo_root/deploy/.env.production"
backup_dir="${LEARNPIP_BACKUP_DIR:-${HOME}/learnpip-backups}"
if [[ ! -r "$config" ]]; then
  echo 'ALERT: production environment file is unavailable' >&2
  exit 1
fi
domain="$(sed -n 's/^LEARNPIP_DOMAIN=//p' "$config" | tail -1)"
if [[ ! "$domain" =~ ^[a-zA-Z0-9.-]+$ ]]; then
  echo 'ALERT: invalid production domain' >&2
  exit 1
fi
if ! curl --fail --silent --show-error --max-time 10 \
    "https://$domain/health/ready" >/dev/null; then
  echo 'ALERT: HTTPS API/database readiness failed' >&2
  exit 1
fi
if [[ ! -d "$backup_dir" ]] ||
   ! find "$backup_dir" -maxdepth 1 -type f -name 'learnpip-*.dump' \
       -mmin -1560 -print -quit | grep -q .; then
  echo 'ALERT: no production database backup younger than 26 hours' >&2
  exit 1
fi
if [[ ! -d "$backup_dir" ]] ||
   ! find "$backup_dir" -maxdepth 1 -type f -name 'learnpip-*.manifest' \
       -mmin -1560 -print -quit | grep -q .; then
  echo 'ALERT: no recent backup manifest' >&2
  exit 1
fi
printf 'OK: HTTPS/database ready; latest backup younger than 26 hours.\n'
