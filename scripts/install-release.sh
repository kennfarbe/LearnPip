#!/usr/bin/env bash
# Download a stable release; never clone Git or install Docker on the Proxmox host.
set -euo pipefail
umask 077
# Debian keeps administrative system tools in sbin, which may be absent from a normal user's PATH.
export PATH="${PATH:-/usr/local/bin:/usr/bin:/bin}:/usr/local/sbin:/usr/sbin:/sbin"

usage() {
  echo 'Usage: install-release.sh [prepare|install|update] [--version latest|vX.Y.Z] [--domain HOST] [--internal] [--rootless-standard-ports] [--directory PATH] [--yes] [--admin-user NAME] [--admin-password-file PATH] [--catalog-package ZIP]'
  echo 'Default: prepare only. Docker/Compose must already be installed for install/update.'
}
die() { echo "Error: $*" >&2; exit 1; }
action=prepare
version=latest
if [[ $EUID == 0 ]]; then install_dir=/opt/learnpip; else install_dir="$HOME/learnpip"; fi
rootless=false
domain=''
internal=false
rootless_standard_ports=false
confirmed=false
admin_user=admin
admin_password_file=''
admin_options=false
catalog_packages=()
if [[ ${1:-} =~ ^(prepare|install|update)$ ]]; then action=$1; shift; fi
while (($#)); do
  case "$1" in
    --version|--domain|--directory|--admin-user|--admin-password-file)
      (($# >= 2)) || die "Missing value for $1"
      case "$1" in
        --version) version=$2 ;;
        --domain) domain=$2 ;;
        --directory) install_dir=$2 ;;
        --admin-user) admin_user=$2; admin_options=true ;;
        --admin-password-file) admin_password_file=$2; admin_options=true ;;
      esac
      shift 2 ;;
    --catalog-package)
      (($# >= 2)) || die 'Missing value for --catalog-package'
      [[ -f $2 && ! -L $2 ]] || die 'Catalog package must be a regular local ZIP file.'
      catalog_packages+=("$(realpath -e "$2")")
      shift 2 ;;
    --internal) internal=true; shift ;;
    --rootless-standard-ports) rootless_standard_ports=true; shift ;;
    --yes) confirmed=true; shift ;;
    --help|-h) usage; exit 0 ;;
    *) usage; die "Unknown argument: $1" ;;
  esac
done
[[ $action == install || ${#catalog_packages[@]} == 0 ]] || die 'Optional setup packages are only valid with install; use Administration after installation.'
[[ $action == install || $admin_options == false ]] || die 'Admin-Zugangsdaten sind nur bei install zulässig; Updates erhalten Konten und Rollen.'
[[ $admin_user =~ ^[a-zA-Z0-9_.-]{1,64}$ ]] || die 'Benutzername: 1–64 Buchstaben, Ziffern, Punkt, Unterstrich oder Bindestrich.'
! command -v pveversion >/dev/null || die 'Do not run this on the Proxmox host. Use a Debian VM.'

ensure_dependencies() {
  local missing=() packages=() command package answer
  local -a dependencies=(
    'curl:curl'
    'python3:python3'
    'tar:tar'
    'realpath:coreutils'
    'flock:util-linux'
    'openssl:openssl'
    'sysctl:procps'
  )
  for entry in "${dependencies[@]}"; do
    command=${entry%%:*}
    package=${entry#*:}
    if ! command -v "$command" >/dev/null; then
      missing+=("$command")
      packages+=("$package")
    fi
  done
  (("${#missing[@]}" == 0)) && return 0

  echo "Missing required dependencies: ${missing[*]}" >&2
  if [[ $action != prepare ]]; then
    die 'Run prepare first to install/check system dependencies.'
  fi
  if [[ ! -r /etc/os-release ]]; then
    die "Install these dependencies manually: ${packages[*]}"
  fi
  # shellcheck disable=SC1091
  . /etc/os-release
  if [[ ${ID:-} != debian && ${ID:-} != ubuntu && " ${ID_LIKE:-} " != *" debian "* ]]; then
    die "Install these dependencies manually: ${packages[*]}"
  fi
  if [[ $confirmed == false ]]; then
    printf 'Install missing system packages with apt: %s\n' "${packages[*]}" >&2
    read -r -p 'Continue? [y/N] ' answer
    [[ $answer =~ ^[Yy]$ ]] || die 'Cancelled.'
  fi

  local -a elevate=()
  if [[ $EUID != 0 ]]; then
    command -v sudo >/dev/null || die "sudo is required once to install: ${packages[*]}"
    elevate=(sudo)
  fi
  "${elevate[@]}" apt-get update
  "${elevate[@]}" apt-get install -y --no-install-recommends "${packages[@]}"
  for command in "${missing[@]}"; do
    command -v "$command" >/dev/null || die "Dependency installation did not provide: $command"
  done
}
ensure_dependencies

configure_rootless_standard_ports() {
  [[ $rootless_standard_ports == true ]] || return 0
  [[ $action == prepare ]] || die '--rootless-standard-ports is only valid with prepare.'
  [[ $EUID != 0 ]] || die 'Rootless standard-port setup must be run from the unprivileged Docker user.'
  command -v sudo >/dev/null || die 'sudo is required once to configure rootless standard ports.'
  current_low_port=$(sysctl -n net.ipv4.ip_unprivileged_port_start)
  if (( current_low_port <= 80 )); then
    echo "Unprivileged low ports are already enabled (net.ipv4.ip_unprivileged_port_start=$current_low_port)."
    return 0
  fi
  if [[ $confirmed == false ]]; then
    echo 'This host-wide setting allows unprivileged processes to bind TCP/UDP ports 80 and above.' >&2
    read -r -p 'Configure rootless Docker for standard ports 80/443? [y/N] ' answer
    [[ $answer =~ ^[Yy]$ ]] || die 'Cancelled.'
  fi
  printf '%s\n' 'net.ipv4.ip_unprivileged_port_start=80' | sudo tee /etc/sysctl.d/90-learnpip-rootless-ports.conf >/dev/null
  sudo sysctl --system >/dev/null
  [[ $(sysctl -n net.ipv4.ip_unprivileged_port_start) -le 80 ]] || die 'Failed to enable unprivileged ports 80/443.'
  echo 'Configured rootless standard ports 80/443.'
}
configure_rootless_standard_ports
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
  [[ $internal == false ]] || die 'Updates reuse the stored deployment mode; omit --internal.'
  [[ $rootless_standard_ports == false ]] || die 'Updates reuse the stored port configuration; omit --rootless-standard-ports.'
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
for required in ['deploy/compose.prod.yaml', 'deploy/compose.rootless.yaml', 'deploy/compose.internal.yaml', 'deploy/Caddyfile.internal', 'deploy/.env.production.example', 'scripts/prod-init.sh']:
    if not (root / required).is_file():
        raise SystemExit('Incomplete release: ' + required)
PY
  source_dir=$(find "$staging/unpacked" -mindepth 1 -maxdepth 1 -type d -print)
  printf '%s\n' "$version" > "$source_dir/.learnpip-release"
  mv "$source_dir" "$target"
fi
install -d -m 700 "$shared/secrets" "$shared/update-queue" "$shared/update-status"
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
# Initialize newly introduced optional secret files on upgrades as well.
# prod-init.sh preserves existing database and authentication credentials.
bash "$target/scripts/prod-init.sh"
python3 - "$shared/.env.production" "$version" "$domain" "$internal" "$rootless_standard_ports" "$shared" <<'PY'
import sys
from pathlib import Path
p = Path(sys.argv[1])
version, domain, internal, standard_ports, shared_dir = sys.argv[2:]
lines = p.read_text().splitlines()
existing_internal = next(
    (line.split('=', 1)[1] for line in lines if line.startswith('LEARNPIP_INTERNAL=')),
    'false',
)
existing_http_port = next((line.split('=', 1)[1] for line in lines if line.startswith('LEARNPIP_HTTP_PORT=')), '8080')
existing_https_port = next((line.split('=', 1)[1] for line in lines if line.startswith('LEARNPIP_HTTPS_PORT=')), '8443')
lines = [line for line in lines if not line.startswith(('LEARNPIP_VERSION=', 'LEARNPIP_INTERNAL=', 'LEARNPIP_HTTP_PORT=', 'LEARNPIP_HTTPS_PORT=', 'LEARNPIP_SHARED_DIR='))]
if domain:
    lines = [line for line in lines if not line.startswith('LEARNPIP_DOMAIN=')]
lines.append('LEARNPIP_VERSION=' + version)
lines.append('LEARNPIP_SHARED_DIR=' + shared_dir)
internal_mode = 'true' if internal == 'true' else existing_internal
lines.append('LEARNPIP_INTERNAL=' + internal_mode)
if standard_ports == 'true':
    lines.extend(['LEARNPIP_HTTP_PORT=80', 'LEARNPIP_HTTPS_PORT=443'])
else:
    lines.extend(['LEARNPIP_HTTP_PORT=' + existing_http_port, 'LEARNPIP_HTTPS_PORT=' + existing_https_port])
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
internal_mode=$(sed -n 's/^LEARNPIP_INTERNAL=//p' "$shared/.env.production")
if [[ $internal_mode == true ]]; then
  [[ -f deploy/compose.internal.yaml && -f deploy/Caddyfile.internal ]] || die 'This release does not support internal/LAN deployment.'
  compose+=(-f deploy/compose.internal.yaml)
fi
if [[ $rootless == true ]]; then
  [[ -f deploy/compose.rootless.yaml ]] || die 'This release does not support rootless deployment.'
  http_port=$(sed -n 's/^LEARNPIP_HTTP_PORT=//p' "$shared/.env.production")
  https_port=$(sed -n 's/^LEARNPIP_HTTPS_PORT=//p' "$shared/.env.production")
  http_port=${http_port:-8080}
  https_port=${https_port:-8443}
  if [[ $http_port == 80 || $https_port == 443 ]]; then
    low_port_start=$(sysctl -n net.ipv4.ip_unprivileged_port_start)
    (( low_port_start <= 80 )) || die 'Standard ports require prepare --rootless-standard-ports first.'
  fi
  compose+=(-f deploy/compose.rootless.yaml)
fi
"${compose[@]}" config --quiet
if [[ $action == update ]]; then
  bash "$current/scripts/backup-prod.sh"
fi
"${compose[@]}" pull
"${compose[@]}" up -d db
"${compose[@]}" --profile ops run --rm migrate
if [[ $action == install ]]; then
  # Secret is transported over stdin, never through process arguments or environment.
  generated=false
  if [[ -n $admin_password_file ]]; then
    python3 - "$admin_password_file" > "$staging/admin-password" <<'ADMINPY'
import os, stat, sys
p = sys.argv[1]
fd = os.open(p, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
with os.fdopen(fd, 'r', encoding='utf-8') as stream:
    info = os.fstat(stream.fileno())
    if not stat.S_ISREG(info.st_mode) or info.st_uid != os.getuid() or info.st_mode & 0o077:
        raise SystemExit('Passwortdatei muss eine eigene reguläre Datei mit Modus 600 oder 400 sein.')
    password = stream.read(1024)
    if password.endswith('\n'):
        password = password[:-1]
    if not 12 <= len(password) <= 128 or not password.strip() or any(c in password for c in '\n\r\0'):
        raise SystemExit('Passwort: 12–128 Zeichen, genau eine Zeile, nicht ausschließlich Leerzeichen.')
    print(password)
ADMINPY
  elif [[ -t 0 && -t 1 ]]; then
    read -r -p "Administrator-Benutzername [$admin_user]: " chosen_user
    admin_user=${chosen_user:-$admin_user}
    [[ $admin_user =~ ^[a-zA-Z0-9_.-]{1,64}$ ]] || die 'Ungültiger Benutzername.'
    echo 'Passwort: 12–128 Zeichen; keine zusätzlichen Zeichenklassen erforderlich. Leer lassen für Zufallspasswort.'
    read -r -s -p 'Passwort: ' admin_password
    printf '\n'
    if [[ -n $admin_password ]]; then
      read -r -s -p 'Passwort wiederholen: ' confirmation
      printf '\n'
      [[ $admin_password == "$confirmation" ]] || die 'Passwörter stimmen nicht überein.'
      [[ ${#admin_password} -ge 12 && ${#admin_password} -le 128 && $admin_password != *$'\r'* ]] || die 'Ungültige Passwortlänge oder Zeilenumbruch.'
    else
      admin_password=$(openssl rand -hex 24)
      generated=true
    fi
    printf '%s\n' "$admin_password" > "$staging/admin-password"
    unset admin_password confirmation
  else
    die 'Nichtinteraktive Installation benötigt --admin-password-file mit geschützter Datei; keine Passwörter in CI-Logs.'
  fi
  bootstrap_status=0
  { printf '%s\n' "$admin_user"; cat "$staging/admin-password"; } |
    "${compose[@]}" --profile ops run --rm -T initialize-admin || bootstrap_status=$?
  if (( bootstrap_status == 0 )); then
    if [[ $generated == true ]]; then
      printf 'Administrator %s – zufälliges Passwort (jetzt sicher speichern): ' "$admin_user"
      cat "$staging/admin-password"
    fi
  elif (( bootstrap_status != 10 )); then
    die 'Administrator-Einrichtung fehlgeschlagen; Installation nicht abgeschlossen.'
  fi
  for catalog_package in "${catalog_packages[@]}"; do
    "${compose[@]}" --profile ops run --rm -T initialize-admin --preview-instance-package < "$catalog_package"
    if [[ $confirmed == false ]]; then
      read -r -p 'Lizenz- und Weitergaberechte geprüft; dieses Paket für Lernende bereitstellen? Type yes: ' package_answer
      [[ $package_answer == yes ]] || die 'Optional package setup cancelled.'
    fi
    "${compose[@]}" --profile ops run --rm -T initialize-admin --install-instance-package < "$catalog_package"
  done
  rm -f "$staging/admin-password"
fi
"${compose[@]}" up -d
"${compose[@]}" ps
if [[ $internal_mode == true ]]; then
  curl -fkSs --proto '=https' --retry 12 --retry-delay 5 --retry-all-errors --connect-timeout 10 --max-time 20 \
    "https://$domain/health/ready" >/dev/null
else
  curl -fsS --proto '=https' --retry 12 --retry-delay 5 --retry-all-errors --connect-timeout 10 --max-time 20 \
    "https://$domain/health/ready" >/dev/null
fi
ln -s "$target" "$staging/current"
mv -Tf "$staging/current" "$install_dir/current"
echo "Running $version. Verify sign-in in the browser; schedule external backups."
echo 'On migration/health failure: investigate before retrying; changing current alone is NOT a database rollback.'
