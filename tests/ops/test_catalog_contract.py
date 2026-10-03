"""Regression tests for the offline draft catalog interchange contract."""

from __future__ import annotations

import hashlib
import io
import json
from pathlib import Path
import runpy
import tempfile
import unittest
import zipfile


MODULE = runpy.run_path(str(Path(__file__).resolve().parents[2] /
                            "scripts" / "validate-catalog.py"))
validate = MODULE["validate"]
InvalidPackage = MODULE["InvalidPackage"]
READER = runpy.run_path(str(Path(__file__).resolve().parents[2] /
                            "scripts" / "read-catalog.py"))
read_catalog = READER["read_catalog"]
ReaderInvalidPackage = READER["_validator"].InvalidPackage
WRITER = runpy.run_path(str(Path(__file__).resolve().parents[2] /
                            "scripts" / "write-catalog.py"))
write_catalog = WRITER["write_catalog"]
WriterInvalidPackage = WRITER["_validator"].InvalidPackage


def license_details():
    return {"id": "CC0-1.0", "holder": "Synthetic test author",
            "attribution": "Synthetic fixture; not a real question catalog"}


def base_files():
    question = {
        "id": "example:synthetic-01",
        "language": "de-DE",
        "prompt": "Welche Antwort ist in diesem synthetischen Test markiert?",
        "answers": [{"id": "a", "text": "Erste Antwort"},
                    {"id": "b", "text": "Zweite Antwort"}],
        "correct_answer_ids": ["b"],
        "explanation": "Synthetischer Test, kein veröffentlichter Fragenkatalog.",
        "topics": ["Technischer Vertragstest"],
        "difficulty": "unknown",
        "license": license_details(),
        "provenance": {"kind": "original"},
        "media": [{"path": "media/test.txt", "alt": "Synthetische Mediendatei",
                   "license": license_details(), "provenance": {"kind": "original"}}],
    }
    questions = {"questions": [question]}
    files = {
        "questions.json": json.dumps(questions, ensure_ascii=False).encode("utf-8"),
        "LICENSES.md": b"CC0-1.0 synthetic data only\n",
        "NOTICE": b"Synthetic data for CI contract tests\n",
        "ATTRIBUTION": b"Synthetic test author\n",
        "media/test.txt": b"synthetic media fixture\n",
    }
    manifest = {
        "format_id": "org.learnpip.catalog.zip",
        "schema_version": "0.1.0",
        "package_id": "example.synthetic",
        "catalog_version": "0.1.0",
        "source_revision": "synthetic-fixture-1",
        "title": "Synthetisches Testpaket",
        "description": "Kein offizieller Fragenkatalog",
        "language": "de-DE",
        "publisher": "Synthetic test author",
        "created_at": "2026-10-03T00:00:00Z",
        "license": license_details(),
        "files": [
            {"path": path, "sha256": hashlib.sha256(content).hexdigest(),
             "size": len(content)}
            for path, content in files.items()
        ],
    }
    return manifest, files, questions


def make_zip(manifest, files, extra=None):
    payload = io.BytesIO()
    with zipfile.ZipFile(payload, "w", compression=zipfile.ZIP_DEFLATED) as archive:
        archive.writestr("manifest.json",
                         json.dumps(manifest, ensure_ascii=False).encode("utf-8"))
        for path, content in files.items():
            archive.writestr(path, content)
        for path, content in extra or []:
            archive.writestr(path, content)
    return payload.getvalue()


class CatalogContractTests(unittest.TestCase):
    def setUp(self):
        self.tempdir = tempfile.TemporaryDirectory()
        self.addCleanup(self.tempdir.cleanup)
        self.filename = str(Path(self.tempdir.name) / "sample.zip")
        self.manifest, self.files, self.questions = base_files()

    def check(self, data):
        Path(self.filename).write_bytes(data)
        return validate(self.filename)

    def test_synthetic_package_with_media_and_umlauts(self):
        self.assertEqual(self.check(make_zip(self.manifest, self.files)),
                         ("example.synthetic", 1))

    def test_reader_preserves_questions_media_and_attribution(self):
        original = make_zip(self.manifest, self.files)
        Path(self.filename).write_bytes(original)
        snapshot = read_catalog(self.filename)
        self.assertEqual(snapshot.manifest, self.manifest)
        self.assertEqual(snapshot.questions, self.questions)
        self.assertEqual(snapshot.media["media/test.txt"], self.files["media/test.txt"])
        self.assertEqual(snapshot.notices["ATTRIBUTION"], self.files["ATTRIBUTION"])
        self.assertEqual(Path(self.filename).read_bytes(), original)
        self.assertFalse((Path(self.tempdir.name) / "media").exists())

    def test_writer_roundtrip_preserves_content_and_source(self):
        original = make_zip(self.manifest, self.files)
        Path(self.filename).write_bytes(original)
        snapshot = read_catalog(self.filename)
        target = Path(self.tempdir.name) / "export.zip"
        write_catalog(snapshot, target)
        exported = read_catalog(target)
        self.assertEqual(exported.questions, snapshot.questions)
        self.assertEqual(exported.media, snapshot.media)
        self.assertEqual(exported.notices, snapshot.notices)
        # JSON reserialization may change bytes and therefore the recorded hashes.
        # Metadata and content must survive; the validator checks regenerated hashes.
        original_metadata = {key: value for key, value in snapshot.manifest.items()
                             if key != "files"}
        exported_metadata = {key: value for key, value in exported.manifest.items()
                             if key != "files"}
        self.assertEqual(exported_metadata, original_metadata)
        self.assertEqual([record["path"] for record in exported.manifest["files"]],
                         [record["path"] for record in snapshot.manifest["files"]])
        self.assertEqual(validate(str(target)), ("example.synthetic", 1))
        self.assertEqual(Path(self.filename).read_bytes(), original)

    def test_writer_refuses_invalid_snapshot_without_replacing_target(self):
        snapshot = read_catalog(self.write_sample())
        changed = dict(snapshot.manifest)
        changed["schema_version"] = "9.0.0"
        target = Path(self.tempdir.name) / "preserve.zip"
        target.write_bytes(b"existing")
        with self.assertRaisesRegex(WriterInvalidPackage, "Unsupported schema_version"):
            write_catalog(snapshot._replace(manifest=changed), target)
        self.assertEqual(target.read_bytes(), b"existing")

    def write_sample(self):
        Path(self.filename).write_bytes(make_zip(self.manifest, self.files))
        return self.filename

    def test_reader_rejects_unknown_future_version(self):
        self.manifest["schema_version"] = "2.0.0"
        Path(self.filename).write_bytes(make_zip(self.manifest, self.files))
        with self.assertRaisesRegex(ReaderInvalidPackage, "Unsupported schema_version"):
            read_catalog(self.filename)

    def test_reader_rejects_tampered_media(self):
        self.files["media/test.txt"] = b"not the declared bytes"
        Path(self.filename).write_bytes(make_zip(self.manifest, self.files))
        with self.assertRaisesRegex(ReaderInvalidPackage, "mismatch"):
            read_catalog(self.filename)

    def test_unsupported_schema_is_not_silently_downgraded(self):
        self.manifest["schema_version"] = "9.0.0"
        with self.assertRaisesRegex(InvalidPackage, "Unsupported schema_version"):
            self.check(make_zip(self.manifest, self.files))

    def test_changed_contents_are_rejected(self):
        self.files["NOTICE"] = b"modified after manifest creation"
        with self.assertRaisesRegex(InvalidPackage, "checksum mismatch|size mismatch"):
            self.check(make_zip(self.manifest, self.files))

    def test_manifest_must_be_first_entry(self):
        payload = io.BytesIO()
        with zipfile.ZipFile(payload, "w", compression=zipfile.ZIP_DEFLATED) as archive:
            archive.writestr("NOTICE", self.files["NOTICE"])
            archive.writestr("manifest.json", json.dumps(self.manifest).encode("utf-8"))
            for path, content in self.files.items():
                if path != "NOTICE":
                    archive.writestr(path, content)
        with self.assertRaisesRegex(InvalidPackage, "first ZIP entry"):
            self.check(payload.getvalue())

    def test_executable_media_is_rejected_even_if_declared(self):
        self.files["media/launch.sh"] = b"echo unsafe\\n"
        self.manifest["files"].append({
            "path": "media/launch.sh",
            "sha256": hashlib.sha256(self.files["media/launch.sh"]).hexdigest(),
            "size": len(self.files["media/launch.sh"]),
        })
        with self.assertRaisesRegex(InvalidPackage, "Unsafe"):
            self.check(make_zip(self.manifest, self.files))

    def test_traversal_path_is_rejected(self):
        with self.assertRaisesRegex(InvalidPackage, "Unsafe"):
            self.check(make_zip(self.manifest, self.files, [("../outside", b"bad")]))

    def test_duplicate_path_is_rejected(self):
        with self.assertWarns(UserWarning):
            data = make_zip(self.manifest, self.files, [("NOTICE", b"duplicate")])
        with self.assertRaisesRegex(InvalidPackage, "Duplicate ZIP"):
            self.check(data)

    def test_correct_answer_must_refer_to_existing_option(self):
        self.questions["questions"][0]["correct_answer_ids"] = ["absent"]
        self.files["questions.json"] = json.dumps(self.questions).encode("utf-8")
        self.update_question_record()
        with self.assertRaisesRegex(InvalidPackage, "correct answer"):
            self.check(make_zip(self.manifest, self.files))

    def test_media_requires_independent_attribution(self):
        del self.questions["questions"][0]["media"][0]["license"]["holder"]
        self.files["questions.json"] = json.dumps(self.questions).encode("utf-8")
        self.update_question_record()
        with self.assertRaisesRegex(InvalidPackage, "license"):
            self.check(make_zip(self.manifest, self.files))

    def test_adapted_content_requires_source_revision_and_change_note(self):
        self.questions["questions"][0]["provenance"] = {
            "kind": "adapted", "source_url": "https://example.org/source",
            "source_revision": "rev-1"
        }
        self.files["questions.json"] = json.dumps(self.questions).encode("utf-8")
        self.update_question_record()
        with self.assertRaisesRegex(InvalidPackage, "modification note"):
            self.check(make_zip(self.manifest, self.files))

    def test_duplicate_json_keys_are_rejected(self):
        self.files["questions.json"] = b'{"questions":[],"questions":[]}'
        self.update_question_record()
        with self.assertRaisesRegex(InvalidPackage, "Duplicate JSON"):
            self.check(make_zip(self.manifest, self.files))

    def update_question_record(self):
        record = next(item for item in self.manifest["files"]
                      if item["path"] == "questions.json")
        record["sha256"] = hashlib.sha256(self.files["questions.json"]).hexdigest()
        record["size"] = len(self.files["questions.json"])


if __name__ == "__main__":
    unittest.main()
