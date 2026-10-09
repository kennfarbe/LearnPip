#!/usr/bin/env python3
"""Record offline catalog review decisions; never upload or publish a package."""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import runpy
import tempfile
from urllib.parse import urlsplit

READER = runpy.run_path(str(Path(__file__).with_name("read-catalog.py")))
LIMIT = 8 * 1024 * 1024
OPEN_LICENSES = {"CC-BY-4.0", "CC-BY-SA-4.0", "CC0-1.0", "DL-DE/BY-2.0", "dl-de/by-2-0"}
CHECKS = {"rights", "content", "privacy"}
FORMAT = "org.learnpip.catalog.review.v1"


def check_open_rights(question):
    """Keep source text and independent media evidence separate, as in the app preview."""
    for item in [question, *question["media"]]:
        license_id = item["license"]["id"]
        license_id = {"CC BY 4.0": "CC-BY-4.0", "CC BY-SA 4.0": "CC-BY-SA-4.0",
                      "CC0 1.0": "CC0-1.0"}.get(license_id, license_id)
        require(license_id in OPEN_LICENSES,
                "Private/unknown individual licenses cannot enter the public index")
        provenance = item["provenance"]
        kind = provenance["kind"]
        source = provenance.get("source_url", "")
        require(kind != "original" or not source, "Original content cannot conceal a third-party source")
        require(kind == "original" or provenance.get("source_revision", "").strip(), "Missing source revision")
        require(kind != "adapted" or provenance.get("modification_note", "").strip(), "Missing modification note")
        if item is question:
            note = question.get("source_note", "")
            if urlsplit(note).scheme in ("http", "https"):
                require(kind != "original" and source == note, "Source note requires matching provenance")
            host = (urlsplit(source).hostname or "").lower()
            if host == "wikipedia.org" or host.endswith(".wikipedia.org"):
                require(license_id == "CC-BY-SA-4.0", "Wikipedia text requires its source license")
            if host == "bundesnetzagentur.de" or host.endswith(".bundesnetzagentur.de"):
                require(license_id in ("DL-DE/BY-2.0", "dl-de/by-2-0"), "Official source text requires its source license")


def require(value, message):
    if not value:
        raise ValueError(message)


def safe_url(value):
    url = urlsplit(value)
    require(url.scheme == "https" and url.hostname and not url.username and
            not url.password and not url.fragment and not url.query and
            not any(char.isspace() for char in value) and len(value) <= 2048,
            "An immutable HTTPS release URL without credentials/query/fragment is required")


def index(events):
    """Only the latest decision for each immutable version controls availability."""
    latest = {}
    hashes = {}
    for event in events:
        require(isinstance(event, dict) and event.get("decision") in
                ("approved", "rejected", "withdrawn"), "Invalid review event")
        key = (event["package_id"], event["catalog_version"])
        digest = event["sha256"]
        require(isinstance(digest, str) and len(digest) == 64 and
                all(char in "0123456789abcdef" for char in digest), "Invalid archive digest")
        require(key not in hashes or hashes[key] == digest,
                "An existing catalog version cannot change its archive bytes")
        hashes[key] = digest
        require(isinstance(event.get("reviewer"), str) and event["reviewer"].strip() and
                isinstance(event.get("reason"), str) and event["reason"].strip(), "Missing review explanation")
        if event["decision"] == "approved":
            require(set(event.get("checks", [])) == CHECKS, "Incomplete human review")
            safe_url(event["url"])
        if key in latest and latest[key]["decision"] == "withdrawn":
            require(event["decision"] == "withdrawn", "Withdrawn versions require a new catalog version")
        latest[key] = event
    fields = ("package_id", "catalog_version", "schema_version", "source_revision", "sha256", "url", "title")
    return [{field: event[field] for field in fields} for _, event in sorted(latest.items())
            if event["decision"] == "approved"]


def load(path):
    if not path.exists():
        return {"format": FORMAT, "events": [], "index": []}
    with path.open("rb") as stream:
        data = stream.read(LIMIT + 1)
    require(len(data) <= LIMIT, "Review registry exceeds 8 MiB")
    state = READER["_validator"].parse_json(data)
    require(state.get("format") == FORMAT and isinstance(state.get("events"), list),
            "Unknown review registry format")
    require(state.get("index") == index(state["events"]), "Registry index does not match review history")
    return state


def decide(package, registry, decision, reviewer, reason, checks=(), url=""):
    """Validate one byte snapshot and atomically store history and its derived index."""
    require(decision in ("approved", "rejected", "withdrawn"), "Unknown decision")
    require(reviewer.strip() and len(reviewer) <= 128 and reason.strip() and len(reason) <= 4096,
            "A reviewer pseudonym and a concise explanation are required")
    registry = Path(registry)
    require(registry.resolve() != Path(package).resolve(), "Registry must not replace the package")
    with Path(package).open("rb") as stream:
        archive = stream.read(25 * 1024 * 1024 + 1)
    require(len(archive) <= 25 * 1024 * 1024, "Archive exceeds 25 MiB")
    with tempfile.TemporaryDirectory() as folder:
        snapshot = Path(folder) / "package.zip"
        snapshot.write_bytes(archive)
        catalog = READER["read_catalog"](snapshot)
    if decision == "approved":
        require(set(checks) == CHECKS, "Confirm rights, subject review and privacy separately")
        safe_url(url)
        for question in catalog.questions["questions"]:
            check_open_rights(question)
    manifest = catalog.manifest
    event = {field: manifest[field] for field in
             ("package_id", "catalog_version", "schema_version", "source_revision", "title")}
    event.update(sha256=hashlib.sha256(archive).hexdigest(), decision=decision,
                 reviewer=reviewer, reason=reason, checks=sorted(set(checks)), url=url,
                 reviewed_at=datetime.now(timezone.utc).isoformat())
    # Reject concurrent writers rather than lose a decision. Remove stale locks only after checking the process.
    lock = registry.with_name(registry.name + ".lock")
    lock.mkdir()
    temporary = None
    try:
        state = load(registry)
        state["events"].append(event)
        state["index"] = index(state["events"])
        data = (json.dumps(state, ensure_ascii=False, indent=2) + "\n").encode("utf-8")
        require(len(data) <= LIMIT, "Review registry exceeds 8 MiB")
        with tempfile.NamedTemporaryFile(dir=registry.parent, delete=False) as stream:
            temporary = Path(stream.name)
            stream.write(data)
            stream.flush()
            os.fsync(stream.fileno())
        os.replace(temporary, registry)
        return state
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
        lock.rmdir()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    review = commands.add_parser("decide")
    review.add_argument("package", type=Path)
    review.add_argument("registry", type=Path)
    review.add_argument("--decision", choices=("approved", "rejected", "withdrawn"), required=True)
    review.add_argument("--reviewer", required=True)
    review.add_argument("--reason", required=True)
    review.add_argument("--checked", choices=sorted(CHECKS), action="append", default=[])
    review.add_argument("--url", default="")
    export = commands.add_parser("index")
    export.add_argument("registry", type=Path)
    args = parser.parse_args()
    try:
        if args.command == "index":
            require(args.registry.is_file(), "Review registry does not exist")
            print(json.dumps({"format": FORMAT, "packages": load(args.registry)["index"]},
                             ensure_ascii=False, indent=2))
        else:
            decide(args.package, args.registry, args.decision, args.reviewer,
                   args.reason, args.checked, args.url)
            print("Review recorded locally. No package uploaded or published.")
        return 0
    except (OSError, ValueError, KeyError, TypeError) as error:
        parser.exit(1, "Review rejected: " + str(error) + "\n")


if __name__ == "__main__":
    raise SystemExit(main())
