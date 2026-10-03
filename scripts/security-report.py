#!/usr/bin/env python3
"""Evaluate scanner JSON without printing private vulnerability identifiers.

For local investigation, run the scanners directly on a trusted computer.
This CI checker prints only an aggregate status and never uploads the report.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import sys


class InvalidScan(ValueError):
    """Untrusted, incomplete or malformed scan result."""


def check(condition: bool, message: str) -> None:
    if not condition:
        raise InvalidScan(message)


def evaluate(kind: str, document: object) -> tuple[int, int]:
    check(isinstance(document, dict), "Expected JSON object")
    if kind == "npm":
        check(isinstance(document.get("auditReportVersion"), int), "Not an npm audit report")
        check(not document.get("error"), "npm scanner returned an error")
        metadata = document.get("metadata")
        check(isinstance(metadata, dict), "npm audit metadata missing")
        severities = metadata.get("vulnerabilities")
        check(isinstance(severities, dict), "npm vulnerability summary missing")
        return int(severities.get("high", 0)), int(severities.get("critical", 0))

    if kind == "nuget":
        projects = document.get("projects")
        check(isinstance(projects, list) and bool(projects), "NuGet projects missing")
        high = critical = 0
        scanned = False
        for project in projects:
            check(isinstance(project, dict), "Invalid NuGet project")
            frameworks = project.get("frameworks", [])
            check(isinstance(frameworks, list), "Invalid NuGet framework list")
            for framework in frameworks:
                check(isinstance(framework, dict), "Invalid NuGet framework")
                scanned = True
                for section in ("topLevelPackages", "transitivePackages"):
                    packages = framework.get(section, [])
                    check(isinstance(packages, list), "Invalid package list")
                    for package in packages:
                        check(isinstance(package, dict), "Invalid package")
                        vulns = package.get("vulnerabilities", [])
                        check(isinstance(vulns, list), "Invalid vulnerability list")
                        for finding in vulns:
                            check(isinstance(finding, dict), "Invalid vulnerability")
                            severity = str(finding.get("severity", "")).lower()
                            check(severity in ("low", "moderate", "medium", "high",
                                               "critical"), "Unknown vulnerability severity")
                            high += severity == "high"
                            critical += severity == "critical"
        # Empty framework lists occur in valid dotnet list JSON for projects without\n        # vulnerable packages. The top-level projects list establishes scanner output.
        return high, critical

    if kind == "trivy":
        check(isinstance(document.get("SchemaVersion"), int), "Invalid Trivy scan report")
        results = document.get("Results")
        check(isinstance(results, list), "Trivy result list missing")
        high = critical = 0
        for result in results:
            check(isinstance(result, dict), "Invalid Trivy result")
            findings = result.get("Vulnerabilities") or []
            check(isinstance(findings, list), "Invalid Trivy vulnerability list")
            for finding in findings:
                check(isinstance(finding, dict), "Invalid vulnerability record")
                severity = str(finding.get("Severity", "")).lower()
                check(severity in ("unknown", "low", "medium", "high", "critical"),
                      "Unknown Trivy severity")
                high += severity == "high"
                critical += severity == "critical"
        return high, critical

    raise InvalidScan("Unknown scanner type")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("kind", choices=("npm", "nuget", "trivy"))
    parser.add_argument("path", type=Path)
    args = parser.parse_args()
    try:
        data = json.loads(args.path.read_text(encoding="utf-8"))
        high, critical = evaluate(args.kind, data)
    except (OSError, UnicodeError, json.JSONDecodeError, InvalidScan, ValueError, TypeError):
        print("Security scan incomplete or invalid; investigation required.", file=sys.stderr)
        return 2
    if high or critical:
        print("Security scan detected high/critical findings. Investigate privately.",
              file=sys.stderr)
        return 1
    print("Security scan completed with no high/critical findings.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
