#!/usr/bin/env python3
"""Deduplicate findings in bounded audit artifacts, without public tickets or logs."""
from datetime import datetime, timedelta, timezone
import argparse
import hashlib
import json
from pathlib import Path


def findings(kind, target, report):
    records = []
    if kind == 'npm':
        if report.get('error') or 'metadata' not in report or not isinstance(report.get('vulnerabilities'), dict):
            raise ValueError('Incomplete npm scan')
        for package, item in report['vulnerabilities'].items():
            for advisory in item.get('via', []):
                if isinstance(advisory, dict):
                    records.append((package, ','.join(item.get('learnpipInstalledVersions', [])) or item.get('range', ''), str(advisory.get('source', advisory.get('url', ''))), advisory.get('severity', 'unknown'), str(item.get('fixAvailable', False))))
    elif kind == 'nuget':
        if not report.get('projects'):
            raise ValueError('Incomplete NuGet scan')
        for project in report['projects']:
            for framework in project.get('frameworks', []):
                for section in ('topLevelPackages', 'transitivePackages'):
                    for item in framework.get(section, []):
                        for advisory in item.get('vulnerabilities', []):
                            records.append((item['id'], item.get('resolvedVersion', ''), advisory['advisoryurl'], advisory['severity'].lower(), 'Check upstream'))
    elif kind == 'trivy':
        if 'Results' not in report or 'Metadata' not in report:
            raise ValueError('Incomplete image scan')
        for result in report['Results']:
            for item in result.get('Vulnerabilities') or []:
                records.append((item['PkgName'], item['InstalledVersion'], item['VulnerabilityID'], item['Severity'].lower(), item.get('FixedVersion', '')))
    else:
        raise ValueError('Unknown scanner')
    result = {}
    for package, version, advisory, severity, fix in records:
        key = hashlib.sha256(json.dumps([kind, target, package, version, advisory]).encode()).hexdigest()
        result[key] = {'scanner': kind, 'target': target, 'package': package, 'affected_version': version, 'advisory': advisory, 'severity': severity, 'fix': fix, 'exploitability': 'Not assessed; operator review required'}
    return result


def reconcile(current, previous, scanned, now, owner):
    result = dict(previous)
    for key, item in current.items():
        old = previous.get(key, {})
        days = {'critical': 1, 'high': 7, 'moderate': 30, 'medium': 30}.get(item['severity'], 90)
        result[key] = item | {'state': 'open', 'owner': owner, 'first_seen': old.get('first_seen', now.isoformat()), 'last_seen': now.isoformat(), 'due': old.get('due', (now + timedelta(days=days)).isoformat())}
    for key, item in previous.items():
        if key not in current and (item['scanner'], item['target']) in scanned:
            result[key] = item | {'state': 'resolved', 'verified_at': now.isoformat()}
    # Missing/failed targets never resolve a previous finding.
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--reports', type=Path, required=True)
    parser.add_argument('--previous', type=Path)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--owner', default='kennfarbe')
    args = parser.parse_args()
    previous = json.loads(args.previous.read_text()).get('findings', {}) if args.previous and args.previous.exists() else {}
    current, scanned, incomplete = {}, set(), []
    for meta_path in args.reports.rglob('scan-*.json'):
        try:
            meta = json.loads(meta_path.read_text())
            report = json.loads((meta_path.parent / meta['report']).read_text())
            if not meta.get('complete'):
                raise ValueError('Scanner failed')
            current.update(findings(meta['scanner'], meta['target'], report))
            scanned.add((meta['scanner'], meta['target']))
        except (ValueError, KeyError, OSError, TypeError):
            incomplete.append(meta_path.name)
    now = datetime.now(timezone.utc)
    ledger = reconcile(current, previous, scanned, now, args.owner)
    args.output.write_text(json.dumps({'schema_version': 1, 'checked_at': now.isoformat(), 'targets': sorted(scanned), 'incomplete': incomplete, 'findings': ledger}, indent=2))
    print(f'Audit ledger: {len(scanned)} complete target scans; {len(incomplete)} incomplete scans. Details remain in audit artifacts.')
    if incomplete or not scanned:
        raise SystemExit(2)


if __name__ == '__main__':
    main()
