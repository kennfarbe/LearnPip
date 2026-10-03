"""Known synthetic findings, retry deduplication, incomplete scans and database age."""
from datetime import datetime, timedelta, timezone
import importlib.util
from pathlib import Path
import unittest


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

    def test_stale_advisory_database_is_incomplete(self):
        now = datetime.now(timezone.utc)
        document = {'VulnerabilityDB': {'Version': 2, 'UpdatedAt': now.isoformat(), 'DownloadedAt': now.isoformat()}}
        self.assertTrue(age.fresh(document, now))
        document['VulnerabilityDB']['UpdatedAt'] = (now - timedelta(days=4)).isoformat()
        self.assertFalse(age.fresh(document, now))


if __name__ == '__main__':
    unittest.main()
