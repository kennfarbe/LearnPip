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
if [[ -n $domain ]]; then
  [[ $domain =~ ^[a-zA-Z0-9]([a-zA-Z0-9.-]*[a-zA-Z0-9])?$ && $domain == *.* && $domain != *..* ]] || die 'Domain must be a hostname without scheme, port or path.'
fi
if [[ $action != prepare ]]; then
  command -v docker >/dev/null || die 'Install Docker Engine and its Compose plugin first.'
  docker compose version >/dev/null
  docker info >/dev/null
  if [[ $EUID != 0 ]]; then
    security=$(docker info --format '{{json .SecurityOptions}}')
    printf '%s' "$security" | python3 -c 'import json,sys; assert any("rootless" in item for item in json.load(sys.stdin))' \
      || die 'Use rootless Docker for unprivileged updates; docker-group access is effectively root.'
    rootless=true
  fi
fi
install -d -m 700 "$install_dir"
exec 9>"$install_dir/.installer.lock"
flock -n 9 || die 'Another installation is running.'
if [[ -e $install_dir/current || -L $install_dir/current ]]; then
  current=$(realpath -e "$install_dir/current")
  [[ $current == "$install_dir/releases/"* && -f $current/deploy/compose.prod.yaml ]] || die 'Invalid current release link.'
  [[ $action != install ]] || die 'An installation exists. Use update after reviewing release notes.'
else
  current=''
  [[ $action != update ]] || die 'No current installation. Use install first; see docs for migration from a clone.'
fi
shared="$install_dir/shared"
if [[ -n $current ]]; then
  for file in .env.production secrets/postgres_password secrets/ConnectionStrings__LearnPip; do
    [[ -s $shared/$file ]] || die "Existing configuration is incomplete: $file. Restore it from backup."
  done
  [[ -z $domain ]] || die 'Updates must reuse the existing domain/configuration.'
fi
if [[ $version == latest ]]; then
  release_endpoint=latest
else
  [[ $version =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]] || die 'Version must be latest or a stable vX.Y.Z tag.'
  release_endpoint="tags/$version"
fi
metadata=$(curl -fsSL --proto '=https' --proto-redir '=https' --connect-timeout 20 --max-time 120 \
  "https://api.github.com/repos/kennfarbe/LearnPip/releases/$release_endpoint")
resolved_version=$(printf '%s' "$metadata" | python3 -c 'import json,sys; r=json.load(sys.stdin); assert not r["draft"] and not r["prerelease"]; print(r["tag_name"])')
[[ $version == latest || $version == "$resolved_version" ]] || die 'Release metadata does not match the requested tag.'
version=$resolved_version
[[ $version =~ ^v[0-9]+\.[0-9]+\.[0-9]+$ ]] || die 'Version must be latest or a stable vX.Y.Z tag.'
target="$install_dir/releases/$version"
if [[ $action == update && $current == "$target" ]]; then
  echo "Already running $version; nothing changed."
  exit 0
fi
if [[ $action != prepare && $confirmed == false ]]; then
  echo "Will $action $version in $install_dir. Review release notes and keep an external VM backup."
  read -r -p 'Continue? Type yes: ' answer
  [[ $answer == yes ]] || die 'Cancelled.'
fi
install -d "$install_dir/releases"
staging=$(mktemp -d "$install_dir/.download-XXXXXXXX")
trap 'rm -rf -- "$staging"' EXIT
trap 'echo "Installation stopped. Check logs and backup; services/schema may have changed. No automatic rollback was attempted." >&2' ERR
if [[ -e $target || -L $target ]]; then
  [[ -f $target/.learnpip-release && $(<"$target/.learnpip-release") == "$version" ]] || die 'Existing unverified release directory; refusing to overwrite it.'
else
  curl -fSL --proto '=https' --proto-redir '=https' --connect-timeout 20 --max-time 600 \
    "https://github.com/kennfarbe/LearnPip/archive/refs/tags/$version.tar.gz" -o "$staging/source.tar.gz"
  python3 - "$staging/source.tar.gz" "$staging/unpacked" <<'PY'
import sys, tarfile
from pathlib import Path, PurePosixPath
with tarfile.open(sys.argv[1], 'r:gz') as archive:
    members = archive.getmembers()
    roots = set()
    for member in members:
        path = PurePosixPath(member.name)
        if path.is_absolute() or '..' in path.parts or not path.parts:
            raise SystemExit('Unsafe archive path')
        roots.add(path.parts[0])
        if not (member.isfile() or member.isdir()):
            raise SystemExit('Links and special files are not permitted in release archives')
        if '/deploy/secrets/' in '/' + member.name or path.name == '.env.production':
            raise SystemExit('Release archive must not contain production secrets')
    if len(roots) != 1:
        raise SystemExit('Expected one archive root')
    archive.extractall(sys.argv[2], filter='data')
root = Path(sys.argv[2]) / next(iter(roots))
for required in ['deploy/compose.prod.yaml', 'deploy/.env.production.example', 'scripts/prod-init.sh']:
    if not (root / required).is_file():
        raise SystemExit('Incomplete release: ' + required)
PY
  source_dir=$(find "$staging/unpacked" -mindepth 1 -maxdepth 1 -type d -print)
  printf '%s\n' "$version" > "$source_dir/.learnpip-release"
  mv "$source_dir" "$target"
fi
install -d -m 700 "$shared/secrets"
if [[ ! -e $shared/.env.production ]]; then
  cp "$target/deploy/.env.production.example" "$shared/.env.production"
fi
for link in secrets .env.production; do
  if [[ -L $target/deploy/$link ]]; then
    [[ $(readlink "$target/deploy/$link") == "$shared/$link" ]] || die "Unexpected shared link: $link"
  else
    [[ ! -e $target/deploy/$link ]] || die "Refusing to overwrite $link"
    ln -s "$shared/$link" "$target/deploy/$link"
  fi
done
if [[ -z $current ]]; then bash "$target/scripts/prod-init.sh"; fi
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
