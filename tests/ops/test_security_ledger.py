"""Known synthetic findings, retry deduplication, incomplete scans and database age."""
from datetime import datetime, timedelta, timezone
import importlib.util
from pathlib import Path
import unittest
import json
import os
import subprocess
import tempfile


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).resolve().parents[2] / 'scripts' / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


ledger = load('security-ledger')
age = load('security-db-age')


class LedgerTests(unittest.TestCase):
    def test_known_finding_is_deduplicated_and_verified_after_fix(self):
        report = {'Metadata': {}, 'Results': [{'Vulnerabilities': [{'PkgName': 'synthetic', 'InstalledVersion': '1.0', 'VulnerabilityID': 'TEST-ONLY-001', 'Severity': 'HIGH', 'FixedVersion': '1.1'}]}]}
        current = ledger.findings('trivy', 'image@sha256:test', report)
        now = datetime(2026, 10, 3, tzinfo=timezone.utc)
        first = ledger.reconcile(current, {}, set(), now, 'kennfarbe')
        second = ledger.reconcile(current, first, set(), now + timedelta(days=1), 'kennfarbe')
        self.assertEqual(len(second), 1)
        self.assertEqual(next(iter(first.values()))['first_seen'], next(iter(second.values()))['first_seen'])
        self.assertEqual(next(iter(first.values()))['due'], next(iter(second.values()))['due'])
        failed = ledger.reconcile({}, second, set(), now, 'kennfarbe')
        self.assertEqual(next(iter(failed.values()))['state'], 'open')
        fixed = ledger.reconcile({}, second, {('trivy', 'image@sha256:test')}, now, 'kennfarbe')
        self.assertEqual(next(iter(fixed.values()))['state'], 'resolved')

    def test_scanner_error_cannot_resolve_findings(self):
        with self.assertRaises(ValueError):
            ledger.findings('npm', 'target', {'error': {'code': 'ENOAUDIT'}})
        with self.assertRaises(ValueError):
            ledger.findings('trivy', 'target', {})

    def test_real_metadata_script_records_complete_and_stale_scans(self):
        script = Path(__file__).resolve().parents[2] / 'scripts/security-scan-metadata.py'
        now = datetime.now(timezone.utc)
        with tempfile.TemporaryDirectory() as temporary:
            directory = Path(temporary)
            (directory / 'trivy.json').write_text(json.dumps({'SchemaVersion': 2, 'Metadata': {}, 'Results': []}))
            version = {'VulnerabilityDB': {'Version': 2, 'UpdatedAt': now.isoformat(), 'DownloadedAt': now.isoformat()}}
            (directory / 'trivy-version.json').write_text(json.dumps(version))
            env = os.environ | {'AUDIT_TARGET': 'synthetic', 'SCAN_OUTCOME': 'success'}
            subprocess.run(['python3', str(script), 'trivy', str(directory)], env=env, check=True, capture_output=True)
            self.assertTrue(json.loads((directory / 'scan-trivy.json').read_text())['complete'])
            (directory / 'trivy-version.json').unlink()
            subprocess.run(['python3', str(script), 'trivy', str(directory)], env=env, check=True, capture_output=True)
            self.assertFalse(json.loads((directory / 'scan-trivy.json').read_text())['complete'])

    def test_stale_advisory_database_is_incomplete(self):
        now = datetime.now(timezone.utc)
        document = {'VulnerabilityDB': {'Version': 2, 'UpdatedAt': now.isoformat(), 'DownloadedAt': now.isoformat()}}
        self.assertTrue(age.fresh(document, now))
        document['VulnerabilityDB']['UpdatedAt'] = (now - timedelta(days=4)).isoformat()
        self.assertFalse(age.fresh(document, now))


if __name__ == '__main__':
    unittest.main()
