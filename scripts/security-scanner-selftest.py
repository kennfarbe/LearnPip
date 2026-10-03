#!/usr/bin/env python3
"""Exercise real npm advisory lookup and a controlled unavailable registry."""
import json
from pathlib import Path
import subprocess
import tempfile

report = Path(__file__).with_name('security-report.mjs')
with tempfile.TemporaryDirectory(prefix='learnpip-scanner-selftest-') as temporary:
    directory = Path(temporary)
    (directory / 'package.json').write_text(json.dumps({'name': 'synthetic-audit-test', 'version': '1.0.0', 'private': True, 'dependencies': {'lodash': '4.17.20'}}))
    install = subprocess.run(['npm', 'install', '--package-lock-only', '--ignore-scripts', '--no-audit', '--no-fund'], cwd=directory, capture_output=True, timeout=120)
    if install.returncode:
        raise SystemExit('Synthetic dependency resolution incomplete.')
    scan = subprocess.run(['npm', 'audit', '--json'], cwd=directory, capture_output=True, timeout=120)
    scan_path = directory / 'scan.json'
    scan_path.write_bytes(scan.stdout)
    evaluation = subprocess.run(['node', str(report.resolve()), 'npm', str(scan_path)], capture_output=True, timeout=30)
    if scan.returncode != 1 or evaluation.returncode != 1:
        raise SystemExit('Known vulnerable fixture was not detected; scanner self-test incomplete.')
    failure = subprocess.run(['npm', 'audit', '--json', '--registry=https://audit.invalid', '--fetch-retries=0', '--fetch-timeout=3000'], cwd=directory, capture_output=True, timeout=15)
    scan_path.write_bytes(failure.stdout)
    evaluation = subprocess.run(['node', str(report.resolve()), 'npm', str(scan_path)], capture_output=True, timeout=30)
    if not failure.returncode or evaluation.returncode != 2:
        raise SystemExit('Registry failure incorrectly accepted as clean.')
print('PASS: real known-vulnerable fixture detected; unavailable registry rejected. No vulnerable packages installed.')
