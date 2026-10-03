#!/usr/bin/env python3
"""Record explicit complete/incomplete targets, including absent scanner reports."""
import importlib.util
import json
import os
from pathlib import Path
import sys
from datetime import datetime, timezone


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).with_name(name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


kind, directory = sys.argv[1], Path(sys.argv[2])
directory.mkdir(exist_ok=True, parents=True)
target = os.environ['AUDIT_TARGET']
reports = [('trivy', 'trivy.json', target)] if kind == 'trivy' else [
    ('npm', f'{component}-{scope}-npm-audit-private.json', f'{target}:{component}:{scope}')
    for component in ('root', 'web') for scope in ('production', 'development')
] + [('nuget', 'nuget.json', target)]
for scanner, name, scan_target in reports:
    complete = False
    try:
        document = json.loads((directory / name).read_text())
        load('security-report').evaluate(scanner, document)
        complete = True
        if scanner == 'npm':
            component = scan_target.split(':')[-2]
            lock_path = Path('source/frontend/web/package-lock.json' if component == 'web' else 'source/package-lock.json')
            lock = json.loads(lock_path.read_text())
            for item in document.get('vulnerabilities', {}).values():
                item['learnpipInstalledVersions'] = sorted({lock['packages'][node]['version'] for node in item.get('nodes', []) if node in lock.get('packages', {}) and 'version' in lock['packages'][node]})
            (directory / name).write_text(json.dumps(document))
        if scanner == 'trivy':
            complete = os.environ.get('SCAN_OUTCOME') == 'success' and load('security-db-age').fresh(
                json.loads((directory / 'trivy-version.json').read_text()), datetime.now(timezone.utc))
    except (OSError, ValueError, KeyError, TypeError):
        complete = False
    (directory / ('scan-' + name)).write_text(json.dumps({'scanner': scanner, 'target': scan_target, 'report': name, 'complete': complete, 'source_ref': os.environ.get('AUDIT_REF', target)}))
