"""Permanent version contracts, lossless migrations and independent schema oracle."""
import copy
import hashlib
import json
from pathlib import Path
import runpy
import tempfile
import unittest

from jsonschema import Draft202012Validator, FormatChecker
from referencing import Registry, Resource

ROOT = Path(__file__).resolve().parents[2]
READER = runpy.run_path(str(ROOT / "scripts/read-catalog.py"))
WRITER = runpy.run_path(str(ROOT / "scripts/write-catalog.py"))
MIGRATOR = runpy.run_path(str(ROOT / "scripts/migrate-catalog.py"))
PORTABLE = runpy.run_path(str(ROOT / "scripts/catalog-schema.py"))
VERSIONS = ("0.1.0", "0.2.0", "1.0.0")


def oracle(value, version, name):
    documents = [json.loads(path.read_text()) for path in
                 (ROOT / "schemas/catalog" / version).glob("*.schema.json")]
    registry = Registry().with_resources((document["$id"], Resource.from_contents(document))
                                         for document in documents)
    schema = next(document for document in documents if document["$id"].endswith(name + ".schema.json"))
    Draft202012Validator.check_schema(schema)
    Draft202012Validator(schema, registry=registry, format_checker=FormatChecker()).validate(value)


class CatalogVersionTests(unittest.TestCase):
    def test_every_archived_version_has_locked_golden_and_full_roundtrip(self):
        self.assertEqual({path.name for path in (ROOT / "schemas/catalog").iterdir()}, set(VERSIONS))
        for version in VERSIONS:
            with self.subTest(version=version), tempfile.TemporaryDirectory() as temp:
                folder = ROOT / "tests/fixtures/catalog" / version
                locked = json.loads((folder / "contract-lock.json").read_text())
                required = {str(path.relative_to(ROOT)) for path in
                            (ROOT / "schemas/catalog" / version).glob("*.schema.json")}
                required.add(str((folder / "golden.zip").relative_to(ROOT)))
                self.assertTrue(required <= locked.keys())
                for path, digest in locked.items():
                    self.assertEqual(hashlib.sha256((ROOT / path).read_bytes()).hexdigest(), digest)
                snapshot = READER["read_catalog"](folder / "golden.zip")
                oracle(snapshot.manifest, version, "manifest")
                oracle(snapshot.questions, version, "questions")
                target = Path(temp) / "roundtrip.zip"
                WRITER["write_catalog"](snapshot, target)
                restored = READER["read_catalog"](target)
                self.assertEqual(restored.questions, snapshot.questions)
                self.assertEqual(restored.media, snapshot.media)
                self.assertEqual(restored.notices, snapshot.notices)
                self.assertEqual(restored.manifest["catalog_version"], snapshot.manifest["catalog_version"])

    def test_legacy_migration_is_lossless_sequential_reproducible_and_idempotent(self):
        source = ROOT / "tests/fixtures/catalog/0.1.0/golden.zip"
        original = source.read_bytes()
        old = READER["read_catalog"](source)
        with tempfile.TemporaryDirectory() as temp:
            middle, target, direct, again = [Path(temp) / name for name in
                                            ("v2.zip", "stable.zip", "direct.zip", "again.zip")]
            migrate = MIGRATOR["migrate_catalog"]
            migrate(source, middle, "0.2.0")
            migrate(middle, target)
            migrate(source, direct)
            migrate(target, again)
            current = READER["read_catalog"](target)
            self.assertEqual(current.questions, READER["read_catalog"](direct).questions)
            self.assertEqual(current.questions, READER["read_catalog"](again).questions)
            self.assertEqual(target.read_bytes(), direct.read_bytes())
            self.assertEqual(target.read_bytes(), again.read_bytes())
            self.assertEqual(current.media, old.media)
            self.assertEqual(current.notices, old.notices)
            question = current.questions["questions"][0]
            for key, value in old.questions["questions"][0].items():
                if key != "answers":
                    self.assertEqual(question[key], value)
            self.assertEqual([{k: v for k, v in answer.items() if k != "blocks"}
                              for answer in question["answers"]], old.questions["questions"][0]["answers"])
            self.assertEqual(question["origin"], {key: value for key, value in old.manifest.items() if key != "files"})
            self.assertEqual(source.read_bytes(), original)

    def test_explicit_old_target_preserves_blocks_and_refuses_loss(self):
        source = ROOT / "tests/fixtures/catalog/1.0.0/golden.zip"
        with tempfile.TemporaryDirectory() as temp:
            target = Path(temp) / "old.zip"
            MIGRATOR["migrate_catalog"](source, target, "0.2.0")
            self.assertEqual(READER["read_catalog"](source).questions, READER["read_catalog"](target).questions)
            target.write_bytes(b"existing target")
            with self.assertRaisesRegex(ValueError, "verlieren"):
                MIGRATOR["migrate_catalog"](source, target, "0.1.0")
            self.assertEqual(target.read_bytes(), b"existing target")
            with self.assertRaisesRegex(ValueError, "Originaldatei"):
                MIGRATOR["migrate_catalog"](source, source)

    def test_both_schema_evaluators_reject_invalid_field_boundaries(self):
        mutations = [("manifest", "language", "not a language"),
                     ("manifest", "package_id", "bad:namespace"),
                     ("manifest", "title", "x" * 257),
                     ("manifest", "created_at", "2026-10-08"),
                     ("manifest", "exporter_app_version", None),
                     ("question", "topics", ["same", "same"]),
                     ("question", "age_band", "x" * 81),
                     ("question", "prompt", "x" * 12001)]
        for version in VERSIONS:
            snapshot = READER["read_catalog"](ROOT / "tests/fixtures/catalog" / version / "golden.zip")
            for kind, key, value in mutations:
                with self.subTest(version=version, field=key):
                    name = "manifest" if kind == "manifest" else "questions"
                    document = copy.deepcopy(snapshot.manifest if name == "manifest" else snapshot.questions)
                    item = document if name == "manifest" else document["questions"][0]
                    item[key] = value
                    with self.assertRaises(Exception):
                        oracle(document, version, name)
                    with self.assertRaises(ValueError):
                        PORTABLE["validate_document"](document, version, name)


if __name__ == "__main__":
    unittest.main()
