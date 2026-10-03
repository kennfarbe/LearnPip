#!/usr/bin/env python3
"""Conservative change classifier for the LearnPip CI workflow.

Read NUL-delimited paths from stdin. Git must use --no-renames so both sides
of a rename are evaluated. Unknown paths intentionally activate all checks.
"""

from __future__ import annotations

import os
import sys

FLAGS = ("markdown", "backend", "web", "installer", "restore", "integration", "release")
ALL = {flag: True for flag in FLAGS}


def classify(paths: list[str], force_all: bool = False) -> dict[str, bool]:
    """Determine the checks needed for the complete change set."""
    if force_all:
        return ALL.copy()

    flags = {flag: False for flag in FLAGS}
    for path in paths:
        path = path.removeprefix("./")
        if not path:
            continue

        if path.endswith(".md"):
            flags["markdown"] = True

        if path.startswith("docs/"):
            if path.startswith("docs/screenshots/"):
                flags["web"] = True
            # Other documentation and assets do not require a code build.
            continue

        if path.endswith(".md") and "/" not in path:
            continue

        if path in {".markdownlint-cli2.jsonc", "cspell.json"}:
            flags["markdown"] = True
            continue

        if path.startswith("backend/") or path.startswith("tests/LearnPip.Data.Tests/"):
            flags["backend"] = True
            flags["integration"] = True
        elif path.startswith("frontend/web/"):
            flags["web"] = True
            flags["integration"] = True
        elif path.startswith("deploy/"):
            flags["installer"] = True
            flags["restore"] = True
            flags["integration"] = True
        elif path == "scripts/install-release.sh" or path == "tests/ops/install-release-smoke.py":
            flags["installer"] = True
        elif path in {"scripts/backup-prod.sh", "scripts/restore-test-prod.sh",
                      "tests/ops/restore-smoke.sh"}:
            flags["restore"] = True
        elif path.startswith("scripts/") or path.startswith("tests/ops/"):
            # Shared operational scripts can affect both install and restore.
            return ALL.copy()
        elif path in {"Directory.Build.props", "stylecop.json"}:
            flags["backend"] = True
            flags["integration"] = True
        elif path == ".editorconfig":
            flags["markdown"] = True
            flags["backend"] = True
            flags["web"] = True
        elif path in {"LICENSE", "SECURITY.md", "CONTRIBUTING.md",
                      "CHANGELOG.md", "README.md", ".github/CODEOWNERS",
                      ".github/PULL_REQUEST_TEMPLATE.md"}:
            # Policy is checked independently on every run.
            continue
        else:
            # Workflow, dependencies, shared configuration or unknown path:
            # do not assume it is safe to skip any check.
            return ALL.copy()

        flags["release"] = True

    return flags


def main() -> None:
    paths = [
        os.fsdecode(part)
        for part in sys.stdin.buffer.read().split(b"\0")
        if part
    ]
    flags = classify(paths, force_all=os.environ.get("CI_FORCE_ALL") == "true")
    for flag, enabled in flags.items():
        value = str(enabled).lower()
        print(f"{flag}={value}")
        if output := os.environ.get("GITHUB_OUTPUT"):
            with open(output, "a", encoding="utf-8") as stream:
                stream.write(f"{flag}={value}\n")


if __name__ == "__main__":
    main()
