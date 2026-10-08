#!/usr/bin/env python3
"""Lossless, offline conversion of supported catalog versions to an explicit target.

The original archive is never modified. No code from packages is executed.
Unrepresentable legacy content fails before the destination is replaced; the
original version remains directly readable by LearnPip.
"""
from __future__ import annotations

import argparse
import copy
from pathlib import Path
import runpy

ROOT = Path(__file__).resolve().parent
READER = runpy.run_path(str(ROOT / "read-catalog.py"))
WRITER = runpy.run_path(str(ROOT / "write-catalog.py"))
VERSIONS = ("0.1.0", "0.2.0", "1.0.0")


def migrate_catalog(source: str | Path, destination: str | Path,
                    target_version: str = "1.0.0") -> None:
    """Validate source, convert a copy and atomically validate/write the result."""
    if Path(source).resolve() == Path(destination).resolve():
        raise ValueError("Originaldatei darf nicht ersetzt werden")
    snapshot = copy.deepcopy(READER["read_catalog"](source))
    if target_version not in VERSIONS:
        raise ValueError("Unbekannte Zielversion")
    version = snapshot.manifest["schema_version"]
    if version == "0.1.0" and target_version != version:
        origin = {key: copy.deepcopy(value) for key, value in snapshot.manifest.items()
                  if key != "files"}
        for question in snapshot.questions["questions"]:
            # The old importer displayed all legacy media after the prompt.
            # Explicit blocks make this same order portable without moving assets.
            question.update(subject=question["topics"][0][:120], topic=question["topics"][0][:120],
                            question_version=snapshot.manifest["catalog_version"],
                            selection_mode="single" if len(question["correct_answer_ids"]) == 1 else "multiple",
                            prompt_blocks=[{"kind": "text", "text": question["prompt"]}] +
                            [{"kind": "image", "path": asset["path"]} for asset in question["media"]],
                            explanation_blocks=[{"kind": "text", "text": question["explanation"]}] if question["explanation"] else [],
                            origin=copy.deepcopy(origin))
            for answer in question["answers"]:
                answer["blocks"] = [{"kind": "text", "text": answer["text"]}]
        snapshot.manifest["schema_version"] = "0.2.0"
        version = "0.2.0"
    if target_version == "0.1.0" and version != target_version:
        raise ValueError("Abwärtsexport nach 0.1.0 würde Block-/Herkunftsdaten verlieren")
    if target_version == "0.2.0":
        for question in snapshot.questions["questions"]:
            if question.get("origin", {}).get("schema_version") == "1.0.0":
                raise ValueError("Ziel 0.2.0 kann den stabilen Herkunftsvertrag nicht darstellen")
    snapshot.manifest["schema_version"] = target_version
    # 0.2.0 -> 1.0.0 stabilizes the block contract without rewriting content.
    # Writing validates every field and recalculates only JSON sizes and hashes.
    WRITER["write_catalog"](snapshot, destination)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source")
    parser.add_argument("destination")
    parser.add_argument("--target-version", choices=VERSIONS, default="1.0.0")
    args = parser.parse_args()
    try:
        migrate_catalog(args.source, args.destination, args.target_version)
    except (OSError, ValueError) as error:
        parser.exit(1, "Migration abgelehnt: " + str(error) + "\n")
    print("Paket vollständig geprüft; Originaldatei unverändert.")


if __name__ == "__main__":
    main()
