#!/usr/bin/env python3
"""Dependency-free Conventional Commit release planner (no remote side effects)."""
from __future__ import annotations

import argparse
from datetime import date
import re
import subprocess
import sys

TAG = re.compile(r"v(\d+)\.(\d+)\.(\d+)")
SUBJECT = re.compile(r"^(feat|fix|perf|refactor|build|ci|docs|style|test|chore|revert)(?:\([^)]+\))?(!)?: (.+)")
BREAKING = re.compile(r"(?m)^BREAKING(?: CHANGE|-CHANGE):\s*.+", re.I)


def plan(tag: str, messages: list[str]) -> tuple[str | None, list[str]]:
    match = TAG.fullmatch(tag)
    if not match:
        raise ValueError("Invalid stable release tag")
    major, minor, patch = map(int, match.groups())
    level = 0
    notes = []
    for message in reversed(messages):
        header, _, body = message.partition("\n")
        found = SUBJECT.match(header)
        if header.startswith("chore(release):"):
            continue
        if found is None:
            if BREAKING.search(message):
                level = max(level, 3)
                notes.append("- **BREAKING CHANGE:** " + header)
            continue
        kind, bang, detail = found.groups()
        if bang or BREAKING.search(body):
            level = max(level, 3)
        elif kind == "feat":
            level = max(level, 2)
        elif kind in ("fix", "perf", "revert"):
            level = max(level, 1)
        notes.append(f"- {kind}: {detail}")
    if not level:
        return None, []
    if level == 3:
        major, minor, patch = major + 1, 0, 0
    elif level == 2:
        minor, patch = minor + 1, 0
    else:
        patch += 1
    return f"v{major}.{minor}.{patch}", notes


def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], text=True).strip()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", required=True, help="GitHub Actions output file")
    parser.add_argument("--notes", required=True, help="Release notes output file")
    args = parser.parse_args()
    candidates = [t for t in git("tag", "--list", "v*").splitlines() if TAG.fullmatch(t)]
    if not candidates:
        raise SystemExit("No stable release tag found")
    latest = max(candidates, key=lambda t: tuple(map(int, TAG.fullmatch(t).groups())))
    # Fail closed if tag does not belong to this history.
    git("merge-base", "--is-ancestor", latest, "HEAD")
    log = subprocess.check_output(
        ["git", "log", "--no-merges", "--format=%B%x00", f"{latest}..HEAD"], text=True
    )
    tag, notes = plan(latest, [m.strip() for m in log.split("\x00") if m.strip()])
    if tag and git("tag", "--list", tag):
        raise SystemExit("Release tag already exists")
    release_notes = (f"## {tag} ({date.today().isoformat()})\n\n"
                     + "\n".join(notes) + "\n") if tag else ""
    from pathlib import Path
    Path(args.notes).write_text(release_notes, encoding="utf-8")
    with open(args.output, "a", encoding="utf-8") as stream:
        stream.write(f"release_tag={tag or ''}\n")
    if tag:
        changelog = Path("CHANGELOG.md")
        original = changelog.read_text(encoding="utf-8") if changelog.exists() else "# Changelog\n"
        if original.startswith("# Changelog\n"):
            original = original[len("# Changelog\n"):].lstrip("\n")
            changelog.write_text("# Changelog\n\n" + release_notes + "\n" + original,
                                 encoding="utf-8")
        else:
            changelog.write_text(release_notes + "\n" + original, encoding="utf-8")
    print(f"Planned release: {tag or 'none'}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
