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
SELECTOR = runpy.run_path(str(Path(__file__).resolve().parents[2] /
                              "scripts" / "select-catalog.py"))
select_from_file = SELECTOR["select_from_file"]


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

    def test_archived_contract_bytes_are_unchanged(self):
        root = Path(__file__).resolve().parents[2]
        fixture = root / "tests/fixtures/catalog/0.1.0"
        hashes = json.loads((fixture / "contract-lock.json").read_text(encoding="utf-8"))
        expected_paths = {str(path.relative_to(root)) for path in
                          (root / "schemas/catalog/0.1.0").glob("*.json")}
        expected_paths.update(str((fixture / name).relative_to(root))
                              for name in ("golden.zip", "frozen.json", "provenance.json"))
        self.assertEqual(set(hashes), expected_paths)
        for name, digest in hashes.items():
            with self.subTest(path=name):
                self.assertEqual(hashlib.sha256((root / name).read_bytes()).hexdigest(), digest,
                                 "Archived contract changed; add a new version instead")

    def test_blocks_archive_is_frozen_and_roundtrips(self):
        root = Path(__file__).resolve().parents[2]
        fixture = root / "tests/fixtures/catalog/0.2.0"
        hashes = json.loads((fixture / "contract-lock.json").read_text())
        expected = {"tests/fixtures/catalog/0.2.0/golden.zip",
                    "schemas/catalog/0.2.0/manifest.schema.json",
                    "schemas/catalog/0.2.0/questions.schema.json"}
        self.assertEqual(set(hashes), expected)
        for name, digest in hashes.items():
            self.assertEqual(hashlib.sha256((root / name).read_bytes()).hexdigest(), digest)
        snapshot = read_catalog(fixture / "golden.zip")
        question = snapshot.questions["questions"][0]
        self.assertEqual(question["answers"][0]["blocks"][0]["kind"], "image")
        self.assertEqual(question["explanation_blocks"][1]["kind"], "image")
        target = Path(self.tempdir.name) / "blocks.zip"
        write_catalog(snapshot, target)
        restored = read_catalog(target)
        self.assertEqual(restored.questions, snapshot.questions)
        self.assertEqual(restored.media, snapshot.media)
        self.assertEqual(restored.notices, snapshot.notices)
        selected = select_from_file(target, {question["id"]})
        self.assertEqual(selected.questions, snapshot.questions)

    def test_blocks_reader_rejects_invalid_references_and_summaries(self):
        fixture = Path(__file__).resolve().parents[1] / "fixtures/catalog/0.2.0/golden.zip"
        for mutation in ("reference", "summary", "mode", "extra"):
            snapshot = read_catalog(fixture)
            question = snapshot.questions["questions"][0]
            if mutation == "reference":
                question["answers"][0]["blocks"][0]["path"] = "media/missing.png"
            elif mutation == "summary":
                question["prompt"] = "Wrong summary"
            elif mutation == "mode":
                question["selection_mode"] = "multiple"
            else:
                question["prompt_blocks"][0]["private_account"] = "not allowed"
            with self.subTest(mutation=mutation):
                with self.assertRaises(WriterInvalidPackage):
                    write_catalog(snapshot, Path(self.tempdir.name) / "invalid-blocks.zip")

    def test_actual_golden_archive_roundtrip_and_selection(self):
        golden = (Path(__file__).resolve().parents[1] / "fixtures/catalog/0.1.0/golden.zip")
        original = golden.read_bytes()
        snapshot = read_catalog(golden)
        self.assertEqual(snapshot.manifest["exporter_app_version"], "fixture-old-app")
        self.assertTrue(snapshot.media["media/diagram.png"].startswith(b"\x89PNG\r\n\x1a\n"))
        target = Path(self.tempdir.name) / "golden-roundtrip.zip"
        write_catalog(snapshot, target)
        exported = read_catalog(target)
        self.assertEqual(exported.questions, snapshot.questions)
        self.assertEqual(exported.media, snapshot.media)
        self.assertEqual(exported.notices, snapshot.notices)
        self.assertEqual({key: value for key, value in exported.manifest.items() if key != "files"},
                         {key: value for key, value in snapshot.manifest.items() if key != "files"})
        selected = select_from_file(golden, {snapshot.questions["questions"][0]["id"]})
        write_catalog(selected, target)
        self.assertEqual(read_catalog(target), exported)
        self.assertEqual(golden.read_bytes(), original)

    def test_writer_preserves_optional_file_metadata(self):
        for record in self.manifest["files"]:
            record["media_type"] = "text/plain"
        snapshot = read_catalog(self.write_sample())
        target = Path(self.tempdir.name) / "metadata.zip"
        write_catalog(snapshot, target)
        self.assertEqual(
            [{key: value for key, value in record.items() if key not in ("size", "sha256")}
             for record in read_catalog(target).manifest["files"]],
            [{key: value for key, value in record.items() if key not in ("size", "sha256")}
             for record in snapshot.manifest["files"]])

    def test_invalid_content_does_not_replace_destination_or_leave_temporary_file(self):
        snapshot = read_catalog(self.write_sample())
        target = Path(self.tempdir.name) / "existing.zip"
        target.write_bytes(b"existing destination")
        before = set(Path(self.tempdir.name).iterdir())
        import copy
        questions = copy.deepcopy(snapshot.questions)
        questions["questions"][0]["correct_answer_ids"] = ["missing"]
        with self.assertRaisesRegex(WriterInvalidPackage, "correct answer"):
            write_catalog(snapshot._replace(questions=questions), target)
        self.assertEqual(target.read_bytes(), b"existing destination")
        self.assertEqual(set(Path(self.tempdir.name).iterdir()), before)

    def test_non_json_numbers_are_rejected(self):
        for value in ("NaN", "Infinity", "-Infinity"):
            with self.subTest(value=value):
                self.files["questions.json"] = (
                    '{"questions":[],"unexpected":' + value + '}').encode("utf-8")
                self.update_question_record()
                with self.assertRaisesRegex(InvalidPackage, "Invalid JSON number"):
                    self.check(make_zip(self.manifest, self.files))

    def test_unknown_schema_message_includes_received_and_supported_version(self):
        self.manifest["schema_version"] = "99.0.0"
        with self.assertRaisesRegex(InvalidPackage, "99.0.0; supported: 0.1.0"):
            self.check(make_zip(self.manifest, self.files))

    def test_frozen_draft_fixture_remains_readable_and_roundtrips(self):
        # This file is independent of base_files(), so future test helper edits
        # cannot silently rewrite the older format's compatibility example.
        fixture_path = (Path(__file__).resolve().parents[1] / "fixtures" /
                        "catalog" / "0.1.0" / "frozen.json")
        frozen = json.loads(fixture_path.read_text(encoding="utf-8"))
        question = frozen.pop("questions")
        expected = {"questions": question}
        files = {
            "questions.json": json.dumps(expected, ensure_ascii=False).encode("utf-8"),
            "LICENSES.md": b"CC0-1.0 synthetic fixture only\\n",
            "NOTICE": b"Frozen offline compatibility test\\n",
            "ATTRIBUTION": b"Synthetic test author\\n",
        }
        frozen["files"] = [
            {"path": path, "size": len(content),
             "sha256": hashlib.sha256(content).hexdigest()}
            for path, content in files.items()
        ]
        original = make_zip(frozen, files)
        Path(self.filename).write_bytes(original)
        self.assertEqual(validate(self.filename), ("example.frozen", 1))
        snapshot = read_catalog(self.filename)
        target = Path(self.tempdir.name) / "frozen-export.zip"
        write_catalog(snapshot, target)
        exported = read_catalog(target)
        self.assertEqual(exported.questions, expected)
        self.assertEqual(exported.questions["questions"][0]["correct_answer_ids"], ["b"])
        self.assertEqual(exported.manifest["schema_version"], "0.1.0")
        self.assertEqual(exported.manifest["source_revision"], "fixture-v1")
        self.assertEqual(exported.notices, snapshot.notices)
        self.assertEqual(Path(self.filename).read_bytes(), original)

    def test_provenance_fixture_preserves_distinct_licenses_and_media(self):
        fixture_path = (Path(__file__).resolve().parents[1] / "fixtures" /
                        "catalog" / "0.1.0" / "provenance.json")
        frozen = json.loads(fixture_path.read_text(encoding="utf-8"))
        expected = {"questions": frozen.pop("questions")}
        files = {
            "questions.json": json.dumps(expected, ensure_ascii=False).encode("utf-8"),
            "LICENSES.md": b"CC0-1.0, CC-BY-SA-4.0, CC-BY-4.0 synthetic only\\n",
            "NOTICE": b"Synthetic contract fixture\\n",
            "ATTRIBUTION": b"Synthetic question and media authors\\n",
            "media/diagram.txt": "Synthetisches Bild: Größe\\n".encode("utf-8"),
        }
        frozen["files"] = [
            {"path": path, "size": len(content),
             "sha256": hashlib.sha256(content).hexdigest()}
            for path, content in files.items()
        ]
        original = make_zip(frozen, files)
        Path(self.filename).write_bytes(original)
        snapshot = read_catalog(self.filename)
        target = Path(self.tempdir.name) / "provenance-export.zip"
        write_catalog(snapshot, target)
        exported = read_catalog(target)
        question = exported.questions["questions"][0]
        self.assertEqual(exported.questions, expected)
        self.assertEqual(question["license"]["id"], "CC-BY-SA-4.0")
        self.assertEqual(question["provenance"]["modification_note"],
                         "Synthetisch gekürzt")
        self.assertEqual(question["media"][0]["license"]["id"], "CC-BY-4.0")
        self.assertEqual(question["media"][0]["provenance"]["source_revision"],
                         "media-rev-1")
        self.assertEqual(exported.media["media/diagram.txt"], files["media/diagram.txt"])
        self.assertEqual(exported.notices, snapshot.notices)
        self.assertEqual(Path(self.filename).read_bytes(), original)

    def test_selective_export_keeps_only_selected_question_and_its_media(self):
        import copy
        second = copy.deepcopy(self.questions["questions"][0])
        second["id"] = "example:synthetic-02"
        second["prompt"] = "Weitere synthetische Frage"
        second["media"] = []
        self.questions["questions"].append(second)
        self.files["questions.json"] = json.dumps(self.questions, ensure_ascii=False).encode("utf-8")
        self.update_question_record()
        original = make_zip(self.manifest, self.files)
        Path(self.filename).write_bytes(original)
        target = Path(self.tempdir.name) / "selected.zip"
        snapshot = select_from_file(self.filename, {"example:synthetic-02"})
        write_catalog(snapshot, target)
        exported = read_catalog(target)
        self.assertEqual(exported.questions["questions"], [second])
        self.assertEqual(exported.media, {})
        self.assertEqual(exported.notices, snapshot.notices)
        self.assertEqual(exported.manifest["package_id"], "example.synthetic")
        self.assertEqual(Path(self.filename).read_bytes(), original)

    def test_selective_export_retains_question_license_and_media(self):
        Path(self.filename).write_bytes(make_zip(self.manifest, self.files))
        snapshot = select_from_file(self.filename, {"example:synthetic-01"})
        target = Path(self.tempdir.name) / "selected-with-media.zip"
        write_catalog(snapshot, target)
        exported = read_catalog(target)
        self.assertEqual(exported.questions, self.questions)
        self.assertEqual(exported.media["media/test.txt"], self.files["media/test.txt"])
        self.assertEqual(exported.questions["questions"][0]["license"], license_details())

    def test_selective_export_rejects_empty_unknown_or_invalid_input(self):
        Path(self.filename).write_bytes(make_zip(self.manifest, self.files))
        with self.assertRaisesRegex(ValueError, "at least one"):
            select_from_file(self.filename, set())
        with self.assertRaisesRegex(ValueError, "Unknown question IDs"):
            select_from_file(self.filename, {"example:missing"})
        self.files["NOTICE"] = b"tampered"
        Path(self.filename).write_bytes(make_zip(self.manifest, self.files))
        with self.assertRaisesRegex(Exception, "mismatch"):
            select_from_file(self.filename, {"example:synthetic-01"})

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

    def test_unknown_manifest_field_is_rejected(self):
        self.manifest["unreviewed_setting"] = True
        with self.assertRaisesRegex(InvalidPackage, "Unknown manifest fields"):
            self.check(make_zip(self.manifest, self.files))

    def test_unknown_questions_document_field_is_rejected(self):
        self.questions["private_export_metadata"] = {"token": "synthetic"}
        self.files["questions.json"] = json.dumps(self.questions).encode("utf-8")
        self.update_question_record()
        with self.assertRaisesRegex(InvalidPackage, "Unknown questions document fields"):
            self.check(make_zip(self.manifest, self.files))

    def test_unknown_question_field_is_rejected(self):
        self.questions["questions"][0]["unreviewed_setting"] = True
        self.files["questions.json"] = json.dumps(self.questions).encode("utf-8")
        self.update_question_record()
        with self.assertRaisesRegex(InvalidPackage, "Unknown question fields"):
            self.check(make_zip(self.manifest, self.files))

    def test_unknown_nested_license_field_is_rejected(self):
        self.questions["questions"][0]["license"]["unreviewed_setting"] = True
        self.files["questions.json"] = json.dumps(self.questions).encode("utf-8")
        self.update_question_record()
        with self.assertRaisesRegex(InvalidPackage, "Unknown license fields"):
            self.check(make_zip(self.manifest, self.files))

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
