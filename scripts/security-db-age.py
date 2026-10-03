#!/usr/bin/env python3
"""Reject stale or unverifiable Trivy vulnerability advisory databases."""
from datetime import datetime, timedelta, timezone
import json
from pathlib import Path
import sys


def fresh(document, now):
    database = document.get('VulnerabilityDB', {})
    updated = datetime.fromisoformat(database['UpdatedAt'].replace('Z', '+00:00'))
    downloaded = datetime.fromisoformat(database['DownloadedAt'].replace('Z', '+00:00'))
    return database.get('Version') == 2 and timedelta(0) <= now - updated <= timedelta(hours=72) and timedelta(0) <= now - downloaded <= timedelta(hours=24)


if __name__ == '__main__':
    try:
        valid = fresh(json.loads(Path(sys.argv[1]).read_text()), datetime.now(timezone.utc))
    except (KeyError, ValueError, TypeError, OSError):
        valid = False
    if not valid:
        raise SystemExit('Advisory database stale or unverifiable; audit incomplete.')
