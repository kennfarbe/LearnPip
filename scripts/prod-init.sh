#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
secrets_dir="$repo_root/deploy/secrets"
config_file="$repo_root/deploy/.env.production"
umask 077

if [[ ! -f "$config_file" ]]; then
  cp "$repo_root/deploy/.env.production.example" "$config_file"
fi
chmod 600 "$config_file"

mkdir -p "$secrets_dir"
chmod 700 "$secrets_dir"

# Never replace a credential used by an existing PostgreSQL volume.
if [[ -e "$secrets_dir/postgres_password" || -e "$secrets_dir/ConnectionStrings__LearnPip" ]]; then
  if [[ ! -s "$secrets_dir/postgres_password" || ! -s "$secrets_dir/ConnectionStrings__LearnPip" ]]; then
    echo 'Database secret files are incomplete. Repair them from a backup; refusing to rotate credentials.' >&2
    exit 1
  fi
else
  password="$(openssl rand -hex 32)"
  printf '%s' "$password" > "$secrets_dir/postgres_password"
  printf 'Host=db;Port=5432;Database=learnpip;Username=learnpip;Password=%s' "$password" \
    > "$secrets_dir/ConnectionStrings__LearnPip"
fi

if [[ ! -e "$secrets_dir/Authentication__EmailCodeKey" ]]; then
  openssl rand -base64 32 | tr -d "\n" > "$secrets_dir/Authentication__EmailCodeKey"
fi
for optional_secret in Mail__Password Oidc__ClientSecret Ai__CloudKey; do
  if [[ ! -e "$secrets_dir/$optional_secret" ]]; then
    : > "$secrets_dir/$optional_secret"
  fi
done
if [[ ! -e "$secrets_dir/Ai__KeyEncryptionKey" ]]; then
  openssl rand -base64 32 | tr -d '\n' > "$secrets_dir/Ai__KeyEncryptionKey"
fi
chmod 600 "$secrets_dir"/*

printf 'Edit deploy/.env.production (domain and optional providers), then follow docs/OPERATIONS.md.\n'
