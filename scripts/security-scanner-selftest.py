#!/usr/bin/env python3
"""Exercise real npm advisory lookup and a controlled unavailable registry."""
import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile

spec = importlib.util.spec_from_file_location('report', Path(__file__).with_name('security-report.py'))
report = importlib.util.module_from_spec(spec)
spec.loader.exec_module(report)
with tempfile.TemporaryDirectory(prefix='learnpip-scanner-selftest-') as temporary:
    directory = Path(temporary)
    (directory / 'package.json').write_text(json.dumps({'name': 'synthetic-audit-test', 'version': '1.0.0', 'private': True, 'dependencies': {'lodash': '4.17.20'}}))
    install = subprocess.run(['npm', 'install', '--package-lock-only', '--ignore-scripts', '--no-audit', '--no-fund'], cwd=directory, capture_output=True, timeout=120)
    if install.returncode:
        raise SystemExit('Synthetic dependency resolution incomplete.')
    scan = subprocess.run(['npm', 'audit', '--json'], cwd=directory, capture_output=True, timeout=120)
    high, critical = report.evaluate('npm', json.loads(scan.stdout))
    if scan.returncode != 1 or high + critical == 0:
        raise SystemExit('Known vulnerable fixture was not detected; scanner self-test incomplete.')
    failure = subprocess.run(['npm', 'audit', '--json', '--registry=https://audit.invalid', '--fetch-retries=0', '--fetch-timeout=3000'], cwd=directory, capture_output=True, timeout=15)
    invalid = False
    try:
        report.evaluate('npm', json.loads(failure.stdout))
    except (ValueError, TypeError):
        invalid = True
    if not failure.returncode or not invalid:
        raise SystemExit('Registry failure incorrectly accepted as clean.')
print('PASS: real known-vulnerable fixture detected; unavailable registry rejected. No vulnerable packages installed.')
