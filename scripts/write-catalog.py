#!/usr/bin/env python3
"""Write a complete draft 0.1.0 catalog snapshot without changing its source.

No database access, network calls, automatic publication or schema migration.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path
import tempfile
import zipfile

from importlib.machinery import SourceFileLoader
from importlib.util import module_from_spec, spec_from_loader

_PATH = Path(__file__).with_name("validate-catalog.py")
_LOADER = SourceFileLoader("learnpip_catalog_writer_validator", str(_PATH))
_SPEC = spec_from_loader(_LOADER.name, _LOADER)
_validator = module_from_spec(_SPEC)
_LOADER.exec_module(_validator)


def write_catalog(snapshot, destination: str | Path) -> None:
    """Write atomically and validate before replacing the destination.

    Caller supplies a complete catalog snapshot from the validated reader.
    This function intentionally does not export selected/private application data.
    """
    manifest = dict(snapshot.manifest)
    if manifest.get("format_id") != _validator.FORMAT_ID:
        raise _validator.InvalidPackage("Unsupported format_id")
    if manifest.get("schema_version") != _validator.VERSION:
        raise _validator.InvalidPackage("Unsupported schema_version")
    files = {
        "questions.json": json.dumps(snapshot.questions, ensure_ascii=False,
                                     separators=(",", ":")).encode("utf-8"),
        **snapshot.notices,
        **snapshot.media,
    }
    if set(snapshot.notices) != {"LICENSES.md", "NOTICE", "ATTRIBUTION"}:
        raise _validator.InvalidPackage("Required attribution files missing")
    if set(files) != {record["path"] for record in manifest["files"]}:
        raise _validator.InvalidPackage("Snapshot does not match manifest file list")
    manifest["files"] = [
        {"path": record["path"], "size": len(files[record["path"]]),
         "sha256": hashlib.sha256(files[record["path"]]).hexdigest()}
        for record in manifest["files"]
    ]
    destination = Path(destination)
    with tempfile.NamedTemporaryFile(dir=destination.parent, suffix=".zip",
                                     delete=False) as temporary:
        name = Path(temporary.name)
    try:
        with zipfile.ZipFile(name, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            archive.writestr("manifest.json", json.dumps(manifest, ensure_ascii=False,
                                                         separators=(",", ":")).encode("utf-8"))
            for record in manifest["files"]:
                archive.writestr(record["path"], files[record["path"]])
        _validator.validate(str(name))
        name.replace(destination)
    finally:
        name.unlink(missing_ok=True)
