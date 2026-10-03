#!/usr/bin/env python3
"""Select questions from a validated offline draft catalog without publication.

This is a package-level helper, not an API endpoint or authorization boundary.
The caller must separately enforce ownership, sharing rights and export consent.
"""
from __future__ import annotations

from pathlib import Path

from importlib.machinery import SourceFileLoader
from importlib.util import module_from_spec, spec_from_loader

_READER_PATH = Path(__file__).with_name("read-catalog.py")
_LOADER = SourceFileLoader("learnpip_catalog_filter_reader", str(_READER_PATH))
_SPEC = spec_from_loader(_LOADER.name, _LOADER)
_reader = module_from_spec(_SPEC)
_LOADER.exec_module(_reader)


def select_questions(snapshot, question_ids: set[str]):
    """Return a complete, smaller snapshot, retaining only referenced media.

    No question, license, attribution or provenance fields are rewritten.
    An empty or unknown selection fails instead of silently exporting nothing.
    """
    if not isinstance(question_ids, set) or not question_ids or not all(
            isinstance(value, str) for value in question_ids):
        raise ValueError("Choose at least one valid question ID")
    available = {question["id"]: question for question in snapshot.questions["questions"]}
    unknown = question_ids - available.keys()
    if unknown:
        raise ValueError("Unknown question IDs: " + ", ".join(sorted(unknown)))
    selected = [question for question in snapshot.questions["questions"]
                if question["id"] in question_ids]
    paths = {asset["path"] for question in selected for asset in question["media"]}
    media = {path: data for path, data in snapshot.media.items() if path in paths}
    manifest = dict(snapshot.manifest)
    manifest["files"] = [record for record in snapshot.manifest["files"]
                         if not record["path"].startswith("media/")
                         or record["path"] in paths]
    return snapshot._replace(manifest=manifest, questions={"questions": selected}, media=media)


def select_from_file(filename: str | Path, question_ids: set[str]):
    """Always validate the input archive before applying a selection."""
    return select_questions(_reader.read_catalog(filename), question_ids)
