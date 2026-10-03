#!/usr/bin/env python3
"""Read the previous default-branch scheduled/manual ledger with a read-only token."""
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import zipfile


def api(endpoint, binary=False):
    data = subprocess.check_output(['gh', 'api', 'repos/kennfarbe/LearnPip/' + endpoint], stderr=subprocess.DEVNULL)
    return data if binary else json.loads(data)


runs = []
for event in ('schedule', 'workflow_dispatch'):
    runs.extend(api('actions/workflows/security.yml/runs?event=' + event + '&status=completed&per_page=10')['workflow_runs'])
for run in sorted(runs, key=lambda item: item['created_at'], reverse=True):
    if run['head_branch'] != 'main' or str(run['id']) == os.environ.get('GITHUB_RUN_ID'):
        continue
    artifacts = api(f"actions/runs/{run['id']}/artifacts")['artifacts']
    matching = [item for item in artifacts if item['name'] == 'security-ledger' and not item['expired']]
    if not matching:
        continue
    archive = api(f"actions/artifacts/{matching[0]['id']}/zip", binary=True)
    with zipfile.ZipFile(io.BytesIO(archive)) as package:
        entry = package.getinfo('security-ledger.json')
        if entry.file_size > 10_000_000:
            raise SystemExit('Previous ledger exceeds size limit')
        document = json.loads(package.read(entry))
        if document.get('schema_version') != 1 or not isinstance(document.get('findings'), dict):
            raise SystemExit('Previous ledger invalid; audit incomplete')
        Path(sys.argv[1]).write_text(json.dumps(document))
    break
else:
    print('No retained previous scheduled ledger; recording a new baseline.')
