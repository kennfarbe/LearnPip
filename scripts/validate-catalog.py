#!/usr/bin/env python3
"""Offline safety and semantic validation of draft LearnPip catalog ZIP files.

Draft format 0.1.0 only. This does not establish publication or usage rights.
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

FORMAT_ID = "org.learnpip.catalog.zip"
VERSION = "0.1.0"
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
    try:
        result = json.loads(content.decode("utf-8-sig"), object_pairs_hook=unique_object)
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


def check_license(item: object) -> None:
    require(isinstance(item, dict), "Missing license details")
    for key in ("id", "holder", "attribution"):
        require(isinstance(item.get(key), str) and bool(item[key].strip()),
                "Missing license field: " + key)


def check_provenance(item: object) -> None:
    require(isinstance(item, dict), "Missing provenance details")
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
            require(manifest.get("format_id") == FORMAT_ID, "Unsupported format_id")
            require(manifest.get("schema_version") == VERSION,
                    "Unsupported schema_version; do not downgrade or partially import")
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
                require(isinstance(item, dict), "Invalid file record")
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
            questions = questions_root.get("questions")
            require(isinstance(questions, list) and 0 < len(questions) <= QUESTION_LIMIT,
                    "Invalid question count")
            question_ids = set()
            referenced_media = set()
            for question in questions:
                require(isinstance(question, dict), "Question must be an object")
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
                require(isinstance(media, list) and len(media) <= 20, "Invalid media")
                for asset in media:
                    require(isinstance(asset, dict), "Invalid asset")
                    path = asset.get("path")
                    require(isinstance(path, str) and SAFE_MEDIA.fullmatch(path) is not None
                            and is_safe_path(path) and path in declared, "Missing media asset")
                    require(isinstance(asset.get("alt"), str) and asset["alt"].strip(),
                            "Missing media alt text")
                    check_license(asset.get("license"))
                    check_provenance(asset.get("provenance"))
                    referenced_media.add(path)
            require({path for path in declared if path.startswith("media/")} == referenced_media,
                    "Unused or missing media assets")
            return manifest["package_id"], len(questions)
    except (zipfile.BadZipFile, EOFError, RuntimeError) as error:
        raise InvalidPackage("Malformed ZIP archive") from error


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", help="Local catalog ZIP file to validate without network access")
    args = parser.parse_args()
    try:
        package_id, count = validate(args.package)
    except (OSError, InvalidPackage) as error:
        print("Rejected catalog: " + str(error), file=sys.stderr)
        return 1
    print(f"Draft catalog {package_id}: {count} questions, integrity verified")
    print("This check does not verify legal rights or certify a stable import format.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
