#!/usr/bin/env bash
set -euo pipefail
mkdir -p /tmp/work
cp -R /input /tmp/work/source
ln -s /trusted-scripts /tmp/work/scripts
cd /tmp/work
if [[ ${1:?} == npm ]]; then
  status=0
  for component in root web; do
    directory=source
    [[ $component == root ]] || directory=source/frontend/web
    for scope in production development; do
      bash scripts/npm-security-audit.sh "$directory" "$component" "$scope" || status=1
    done
  done
  cp source/package.json source/package-lock.json .
  bash scripts/try-security-overrides.sh
  exit "$status"
fi
if ! dotnet restore source/backend/LearnPip.sln > /output/nuget-restore.log 2>&1; then
  echo 'NuGet restore incomplete inside isolated audit sandbox.' >&2
  exit 2
fi
if ! dotnet list source/backend/LearnPip.sln package --vulnerable --include-transitive --format json > /output/nuget.json 2> /output/nuget-error.log; then
  echo 'NuGet vulnerability lookup incomplete inside isolated audit sandbox.' >&2
  exit 2
fi
python3 scripts/security-report.py nuget /output/nuget.json
