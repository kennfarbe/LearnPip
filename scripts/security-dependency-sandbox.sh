#!/usr/bin/env bash
# Execute audited manifests without runner credentials, cache access or Docker socket.
set -euo pipefail
kind=${1:?scanner kind required}
case "$kind" in npm|nuget) ;; *) exit 2 ;; esac
raw="$RUNNER_TEMP/security-sandbox-raw-$kind"
mkdir -p "$raw"
if [[ $kind == npm ]]; then
  docker build -f security/audit-node.Dockerfile -t learnpip-security:node-audit .
  image=learnpip-security:node-audit
else
  image=mcr.microsoft.com/dotnet/sdk:10.0.401
fi
status=0
docker run --rm --user "$(id -u):$(id -g)" \
  --cap-drop ALL --security-opt no-new-privileges --read-only \
  --tmpfs /tmp:rw,exec,nosuid,size=2g \
  --mount "type=bind,src=$PWD/source,dst=/input,readonly" \
  --mount "type=bind,src=$PWD/scripts,dst=/trusted-scripts,readonly" \
  --mount "type=bind,src=$raw,dst=/output" \
  -e RUNNER_TEMP=/output -e npm_config_cache=/tmp/npm-cache \
  -e DOTNET_CLI_HOME=/tmp/dotnet -e NUGET_PACKAGES=/tmp/nuget \
  "$image" bash /trusted-scripts/security-sandbox-entry.sh "$kind" || status=$?
python3 scripts/security-copy-reports.py "$kind" "$raw" "$RUNNER_TEMP"
if [[ $kind == nuget && $status -eq 0 ]]; then
  node scripts/security-report.mjs nuget "$RUNNER_TEMP/nuget.json" || status=$?
fi
exit "$status"
