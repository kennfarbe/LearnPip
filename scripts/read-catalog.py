#!/usr/bin/env python3
"""Read a validated LearnPip draft catalog fully offline without extracting files.

The returned objects are in-memory snapshots; this module does not write to a
database or silently migrate unknown versions. Format 0.1.0 is still a draft.
"""
from __future__ import annotations

from dataclasses import dataclass
import json
from pathlib import Path
import zipfile

from importlib.machinery import SourceFileLoader
from importlib.util import module_from_spec, spec_from_loader

_VALIDATOR_PATH = Path(__file__).with_name("validate-catalog.py")
_LOADER = SourceFileLoader("learnpip_catalog_validator", str(_VALIDATOR_PATH))
_SPEC = spec_from_loader(_LOADER.name, _LOADER)
_validator = module_from_spec(_SPEC)
_LOADER.exec_module(_validator)


@dataclass(frozen=True)
class CatalogSnapshot:
    manifest: dict
    questions: dict
    media: dict[str, bytes]
    notices: dict[str, bytes]


def read_catalog(filename: str | Path) -> CatalogSnapshot:
    """Validate first, then read all declared files with no filesystem extraction.

    Consumers must treat returned dicts as package data, not trusted code.
    Unknown schema versions fail inside validate before any data is returned.
    """
    filename = str(filename)
    _validator.validate(filename)
    with zipfile.ZipFile(filename) as archive:
        manifest = _validator.parse_json(archive.read("manifest.json"))
        questions = _validator.parse_json(archive.read("questions.json"))
        media = {}
        notices = {}
        for record in manifest["files"]:
            name = record["path"]
            if name.startswith("media/"):
                media[name] = archive.read(name)
            elif name in ("LICENSES.md", "NOTICE", "ATTRIBUTION"):
                notices[name] = archive.read(name)
        return CatalogSnapshot(manifest, questions, media, notices)
