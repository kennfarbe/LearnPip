#!/usr/bin/env bash
# Download a stable release; never clone Git or install Docker on the Proxmox host.
set -euo pipefail
umask 077

usage() {
  echo 'Usage: install-release.sh [prepare|install|update] [--version latest|vX.Y.Z] [--domain HOST] [--directory PATH] [--yes]'
  echo 'Default: prepare only. Docker/Compose must already be installed for install/update.'
}
die() { echo "Error: $*" >&2; exit 1; }
action=prepare
version=latest
if [[ $EUID == 0 ]]; then install_dir=/opt/learnpip; else install_dir="$HOME/learnpip"; fi
rootless=false
domain=''
confirmed=false
if [[ ${1:-} =~ ^(prepare|install|update)$ ]]; then action=$1; shift; fi
while (($#)); do
  case "$1" in
    --version|--domain|--directory)
      (($# >= 2)) || die "Missing value for $1"
      case "$1" in
        --version) version=$2 ;;
        --domain) domain=$2 ;;
        --directory) install_dir=$2 ;;
      esac
      shift 2 ;;
    --yes) confirmed=true; shift ;;
    --help|-h) usage; exit 0 ;;
    *) usage; die "Unknown argument: $1" ;;
  esac
done
! command -v pveversion >/dev/null || die 'Do not run this on the Proxmox host. Use a Debian VM.'
for command in curl python3 tar realpath flock openssl; do
  command -v "$command" >/dev/null || die "Missing dependency: $command"
done
[[ $install_dir == /* ]] || die 'Installation directory must be absolute.'
install_dir=$(realpath -m "$install_dir")
[[ $install_dir != / && $install_dir != /root && $install_dir != /home ]] || die 'Use a dedicated installation directory.'
[[ $install_dir != *$'\n'* ]] || die 'Installation directory must not contain newlines.'
python3 - "$shared/.env.production" "$version" "$domain" <<'PY'
import sys
from pathlib import Path
p = Path(sys.argv[1])
version, domain = sys.argv[2], sys.argv[3]
lines = p.read_text().splitlines()
lines = [line for line in lines if not line.startswith('LEARNPIP_VERSION=')]
if domain:
    lines = [line for line in lines if not line.startswith('LEARNPIP_DOMAIN=')]
lines.append('LEARNPIP_VERSION=' + version)
if domain:
    lines.append('LEARNPIP_DOMAIN=' + domain)
p.write_text('\n'.join(lines) + '\n')
PY
echo "Prepared $version at $target"
if [[ $action == prepare ]]; then
  echo "Edit $shared/.env.production; then run this script with install --version $version."
  exit 0
fi
domain=$(sed -n 's/^LEARNPIP_DOMAIN=//p' "$shared/.env.production")
[[ -n $domain && $domain != learn.example.org && $domain != *example.com* ]] || die 'Set your actual domain in shared/.env.production first.'
cd "$target"
compose=(docker compose --env-file deploy/.env.production -f deploy/compose.prod.yaml)
if [[ $rootless == true ]]; then
  [[ -f deploy/compose.rootless.yaml ]] || die 'This release does not support rootless deployment.'
  compose+=(-f deploy/compose.rootless.yaml)
fi
"${compose[@]}" config --quiet
if [[ $action == update ]]; then
  bash "$current/scripts/backup-prod.sh"
fi
"${compose[@]}" pull
"${compose[@]}" up -d db
"${compose[@]}" --profile ops run --rm migrate
"${compose[@]}" up -d
"${compose[@]}" ps
curl -fsS --proto '=https' --retry 12 --retry-delay 5 --retry-all-errors --connect-timeout 10 --max-time 20 \
  "https://$domain/health/ready" >/dev/null
ln -s "$target" "$staging/current"
mv -Tf "$staging/current" "$install_dir/current"
echo "Running $version. Verify sign-in in the browser; schedule external backups."
echo 'On migration/health failure: investigate before retrying; changing current alone is NOT a database rollback.'
