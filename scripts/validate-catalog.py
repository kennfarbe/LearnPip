#!/usr/bin/env python3
"""Offline safety, archived schema and semantic validation of LearnPip catalog ZIP files.

Stable format 1.0.0 and retained drafts 0.1.0/0.2.0; no publication/usage rights proof.
The immutable JSON schemas are the format specification; this validator also
checks relationships and ZIP properties which JSON Schema cannot express.
"""

from __future__ import annotations

import argparse
from datetime import datetime
import hashlib
import json
import re
import stat
import sys
import zipfile
from pathlib import Path
import runpy

_SCHEMA = runpy.run_path(str(Path(__file__).with_name("catalog-schema.py")))
validate_document = _SCHEMA["validate_document"]

FORMAT_ID = "org.learnpip.catalog.zip"
VERSION = "0.1.0"
VERSIONS = {"0.1.0", "0.2.0", "1.0.0"}
REQUIRED = {"questions.json", "LICENSES.md", "NOTICE", "ATTRIBUTION"}
FILE_LIMIT = 2000
TOTAL_LIMIT = 100 * 1024 * 1024
ARCHIVE_LIMIT = 25 * 1024 * 1024
SINGLE_LIMIT = 20 * 1024 * 1024
QUESTION_LIMIT = 10000
SAFE_MEDIA = re.compile(r"^media/[A-Za-z0-9._/-]+$")
SAFE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp", ".gif", ".svg", ".txt", ".pdf"}
SAFE_ID = re.compile(r"^[a-z0-9][a-z0-9._:-]{2,127}$")


class InvalidPackage(ValueError):
    """Archive must not be imported."""


def require(condition: bool, message: str) -> None:
    if not condition:
        raise InvalidPackage(message)


def unique_object(pairs: list[tuple[str, object]]) -> dict[str, object]:
    result = {}
    for key, value in pairs:
        require(key not in result, "Duplicate JSON property")
        result[key] = value
    return result


def parse_json(content: bytes) -> dict:
    def reject_constant(value: str):
        raise InvalidPackage("Invalid JSON number: " + value)

    try:
        result = json.loads(content.decode("utf-8-sig"), object_pairs_hook=unique_object,
                            parse_constant=reject_constant)
    except (UnicodeError, json.JSONDecodeError) as error:
        raise InvalidPackage("Invalid UTF-8 or JSON") from error
    require(isinstance(result, dict), "JSON root must be an object")
    return result


def is_safe_path(path: str) -> bool:
    if path in REQUIRED:
        return True
    if not SAFE_MEDIA.fullmatch(path):
        return False
    parts = path.split("/")
    return (len(parts) >= 2 and all(part not in ("", ".", "..") for part in parts)
            and "." + parts[-1].rsplit(".", 1)[-1].lower() in SAFE_EXTENSIONS
            and "." in parts[-1])


def known_fields(item: object, allowed: set[str], label: str) -> None:
    require(isinstance(item, dict), "Invalid " + label)
    require(not (set(item) - allowed), "Unknown " + label + " fields")


def check_license(item: object) -> None:
    known_fields(item, {"id", "holder", "attribution", "license_url"}, "license")
    for key in ("id", "holder", "attribution"):
        require(isinstance(item.get(key), str) and bool(item[key].strip()),
                "Missing license field: " + key)


def check_provenance(item: object) -> None:
    known_fields(item, {"kind", "source_url", "source_revision", "modification_note"},
                 "provenance")
    kind = item.get("kind")
    require(kind in ("original", "adapted", "verbatim"), "Invalid provenance kind")
    if kind != "original":
        for key in ("source_url", "source_revision"):
            require(isinstance(item.get(key), str) and bool(item[key].strip()),
                    "External source requires " + key)
    if kind == "adapted":
        require(isinstance(item.get("modification_note"), str) and
                bool(item["modification_note"].strip()),
                "Adapted content needs a modification note")


def validate(filename: str) -> tuple[str, int]:
    with open(filename, "rb") as archive_file:
        archive_file.seek(0, 2)
        require(archive_file.tell() <= ARCHIVE_LIMIT, "Archive exceeds compressed size limit")
    try:
        with zipfile.ZipFile(filename) as archive:
            infos = archive.infolist()
            require(0 < len(infos) <= FILE_LIMIT + 1, "Invalid number of ZIP entries")
            entries = {}
            total_size = 0
            for info in infos:
                name = info.filename
                require(name == "manifest.json" or is_safe_path(name),
                        "Unsafe or unsupported ZIP path: " + name)
                require(name not in entries, "Duplicate ZIP entry")
                require(not info.is_dir(), "Directory entries are not supported")
                file_type = (info.external_attr >> 16) & 0o170000
                require(file_type in (0, stat.S_IFREG), "Non-regular ZIP entry")
                require(not info.flag_bits & 0x1, "Encrypted ZIP entries are unsupported")
                require(info.file_size <= SINGLE_LIMIT, "Entry exceeds size limit")
                require(info.compress_size > 0 or info.file_size == 0,
                        "Invalid compressed entry size")
                require(info.file_size <= max(info.compress_size, 1) * 100,
                        "Suspicious ZIP compression ratio")
                total_size += info.file_size
                require(total_size <= TOTAL_LIMIT, "Archive exceeds extracted size limit")
                entries[name] = info

            require("manifest.json" in entries, "Missing manifest")
            require(infos[0].filename == "manifest.json",
                    "manifest.json must be the first ZIP entry")
            require(REQUIRED <= entries.keys(), "Missing mandatory catalog file")
            manifest = parse_json(archive.read("manifest.json"))
            known_fields(manifest, {"format_id", "schema_version", "package_id",
                                    "catalog_version", "source_revision", "title",
                                    "description", "language", "publisher", "created_at",
                                    "exporter_app_version", "license", "files"}, "manifest")
            require(manifest.get("format_id") == FORMAT_ID, "Unsupported format_id")
            require(manifest.get("schema_version") in VERSIONS,
                    "Unsupported schema_version: " + str(manifest.get("schema_version")) +
                    "; supported: " + ", ".join(sorted(VERSIONS)) +
                    ". Bitte einen passenden Reader verwenden; kein Teilimport.")
            for key in ("package_id", "catalog_version", "source_revision",
                        "title", "publisher", "description", "language", "created_at"):
                require(isinstance(manifest.get(key), str), "Missing manifest field: " + key)
            require(SAFE_ID.fullmatch(manifest["package_id"]) is not None,
                    "Invalid package_id")
            try:
                timestamp = datetime.fromisoformat(manifest["created_at"].replace("Z", "+00:00"))
                require(timestamp.tzinfo is not None, "created_at must contain timezone")
            except ValueError as error:
                raise InvalidPackage("Invalid created_at timestamp") from error
            check_license(manifest.get("license"))
            files = manifest.get("files")
            require(isinstance(files, list), "Manifest files must be an array")
            declared = {}
            for item in files:
                known_fields(item, {"path", "sha256", "size", "media_type"}, "file record")
                path = item.get("path")
                require(isinstance(path, str) and is_safe_path(path), "Invalid declared path")
                require(path not in declared, "Duplicate declared file")
                require(type(item.get("size")) is int and item["size"] >= 0,
                        "Invalid file size")
                require(isinstance(item.get("sha256"), str) and
                        re.fullmatch(r"[a-f0-9]{64}", item["sha256"]) is not None,
                        "Invalid SHA-256")
                declared[path] = item
            require(set(entries) == set(declared) | {"manifest.json"},
                    "ZIP entries and manifest file list differ")
            for path, record in declared.items():
                data = archive.read(path)
                require(len(data) == record["size"], "File size mismatch: " + path)
                require(hashlib.sha256(data).hexdigest() == record["sha256"],
                        "File checksum mismatch: " + path)

            questions_root = parse_json(archive.read("questions.json"))
            known_fields(questions_root, {"questions"}, "questions document")
            questions = questions_root.get("questions")
            require(isinstance(questions, list) and 0 < len(questions) <= QUESTION_LIMIT,
                    "Invalid question count")
            blocks_format = manifest["schema_version"] != "0.1.0"
            question_ids = set()
            referenced_media = set()
            for question in questions:
                known_fields(question, {"id", "language", "prompt", "answers",
                                        "correct_answer_ids", "explanation", "topics",
                                        "difficulty", "age_band", "license", "provenance",
                                        "media"} | ({"subject", "topic", "question_version", "selection_mode", "prompt_blocks", "explanation_blocks", "source_note", "origin"} if blocks_format else set()), "question")
                question_id = question.get("id")
                require(isinstance(question_id, str) and
                        SAFE_ID.fullmatch(question_id) is not None, "Invalid question ID")
                require(question_id not in question_ids, "Duplicate question ID")
                question_ids.add(question_id)
                for field in ("language", "prompt", "explanation"):
                    require(isinstance(question.get(field), str), "Missing question field: " + field)
                require(bool(question["prompt"].strip()), "Empty question prompt")
                require(isinstance(question.get("topics"), list) and
                        len(question["topics"]) > 0 and
                        all(isinstance(topic, str) and topic.strip()
                            for topic in question["topics"]), "Invalid topics")
                require(question.get("difficulty") in ("unknown", "easy", "medium", "hard"),
                        "Invalid difficulty")
                check_license(question.get("license"))
                check_provenance(question.get("provenance"))
                answers = question.get("answers")
                correct = question.get("correct_answer_ids")
                require(isinstance(answers, list) and 2 <= len(answers) <= 20,
                        "Invalid answer count")
                answer_ids = []
                for answer in answers:
                    known_fields(answer, {"id", "text"} | ({"blocks"} if blocks_format else set()), "answer")
                    require(isinstance(answer, dict) and isinstance(answer.get("id"), str)
                            and bool(answer["id"].strip()) and
                            isinstance(answer.get("text"), str) and
                            bool(answer["text"].strip()), "Invalid answer")
                    answer_ids.append(answer["id"])
                require(len(set(answer_ids)) == len(answer_ids), "Duplicate answer ID")
                require(isinstance(correct, list) and bool(correct) and
                        all(isinstance(value, str) for value in correct) and
                        len(set(correct)) == len(correct) and
                        set(correct) <= set(answer_ids), "Invalid correct answer references")
                media = question.get("media")
                require(isinstance(media, list) and len(media) <= (180 if blocks_format else 20), "Invalid media")
                for asset in media:
                    known_fields(asset, {"path", "alt", "license", "provenance"}, "asset")
                    path = asset.get("path")
                    require(isinstance(path, str) and SAFE_MEDIA.fullmatch(path) is not None
                            and is_safe_path(path) and path in declared, "Missing media asset")
                    require(isinstance(asset.get("alt"), str) and asset["alt"].strip(),
                            "Missing media alt text")
                    check_license(asset.get("license"))
                    check_provenance(asset.get("provenance"))
                    referenced_media.add(path)
                if blocks_format:
                    check_blocks_question(question)
            require({path for path in declared if path.startswith("media/")} == referenced_media,
                    "Unused or missing media assets")
            try:
                validate_document(manifest, manifest["schema_version"], "manifest")
                validate_document(questions_root, manifest["schema_version"], "questions")
            except ValueError as error:
                raise InvalidPackage("Archived JSON schema rejected package: " + str(error)) from error
            return manifest["package_id"], len(questions)
    except (zipfile.BadZipFile, EOFError, RuntimeError) as error:
        raise InvalidPackage("Malformed ZIP archive") from error


def check_blocks_question(question: dict) -> None:
    if "origin" in question:
        origin = question["origin"]
        known_fields(origin, {"format_id", "schema_version", "package_id", "catalog_version",
                              "source_revision", "title", "description", "language", "publisher",
                              "created_at", "exporter_app_version", "license"}, "origin")
        require(origin.get("format_id") == FORMAT_ID and origin.get("schema_version") in VERSIONS,
                "Invalid original manifest metadata")
        for field in ("package_id", "catalog_version", "source_revision", "title", "publisher",
                      "description", "language", "created_at"):
            require(isinstance(origin.get(field), str), "Missing origin metadata")
        check_license(origin.get("license"))
    for field, limit in (("subject", 120), ("topic", 120), ("question_version", 128)):
        require(isinstance(question.get(field), str) and
                1 <= len(question[field]) <= limit and question[field].strip(),
                "Invalid block metadata: " + field)
    mode = question.get("selection_mode")
    count = len(question["correct_answer_ids"])
    require((mode == "single" and count == 1) or (mode == "multiple" and count >= 2),
            "Selection mode does not match solutions")
    declared = {asset["path"] for asset in question["media"]}
    require(len(declared) == len(question["media"]), "Duplicate media description")
    referenced = set()

    def blocks(value, name, summary, required):
        items = value.get(name)
        require(isinstance(items, list) and (1 if required else 0) <= len(items) <= 20,
                "Invalid block list")
        text = []
        for block in items:
            require(isinstance(block, dict), "Invalid block")
            if block.get("kind") == "text":
                known_fields(block, {"kind", "text"}, "text block")
                require(isinstance(block.get("text"), str) and
                        1 <= len(block["text"]) <= 4000 and block["text"].strip(),
                        "Invalid text block")
                text.append(block["text"])
            else:
                known_fields(block, {"kind", "path"}, "image block")
                require(block.get("kind") == "image" and isinstance(block.get("path"), str) and
                        block["path"] in declared, "Missing image block")
                referenced.add(block["path"])
        require(summary == ("[Bild]" if not text and required else "\n".join(text)),
                "Block summary mismatch")

    blocks(question, "prompt_blocks", question["prompt"], True)
    blocks(question, "explanation_blocks", question["explanation"], False)
    for answer in question["answers"]:
        blocks(answer, "blocks", answer["text"], True)
    require(declared == referenced, "Unreferenced block media")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", help="Local catalog ZIP file to validate without network access")
    args = parser.parse_args()
    try:
        package_id, count = validate(args.package)
    except (OSError, InvalidPackage) as error:
        print("Rejected catalog: " + str(error), file=sys.stderr)
        return 1
    print(f"Catalog {package_id}: {count} questions, schema and integrity verified")
    print("This check does not verify legal rights or authenticate the publisher.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
