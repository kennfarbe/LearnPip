"""Review gates, immutable releases, withdrawal and independent offline imports."""
import copy
import hashlib
import json
from pathlib import Path
import runpy
import subprocess
import tempfile
import unittest

from test_catalog_contract import base_files, make_zip

ROOT = Path(__file__).resolve().parents[2]
REVIEW = runpy.run_path(str(ROOT / "scripts/review-catalog.py"))
READER = runpy.run_path(str(ROOT / "scripts/read-catalog.py"))


class CatalogReviewTests(unittest.TestCase):
    def setUp(self):
        self.folder = tempfile.TemporaryDirectory()
        self.addCleanup(self.folder.cleanup)
        self.package = Path(self.folder.name) / "catalog.zip"
        self.registry = Path(self.folder.name) / "reviews.json"
        self.manifest, self.files, self.questions = base_files()
        self.write()

    def write(self):
        self.files["questions.json"] = json.dumps(self.questions).encode()
        self.manifest["files"] = [{"path": path, "size": len(data),
                                    "sha256": hashlib.sha256(data).hexdigest()}
                                   for path, data in sorted(self.files.items())]
        self.package.write_bytes(make_zip(self.manifest, self.files))

    def decide(self, decision="approved", checks=("rights", "content", "privacy"),
               url="https://example.org/releases/v1/catalog.zip"):
        return REVIEW["decide"](self.package, self.registry, decision,
                                "synthetic-reviewer", "Synthetische Fachprüfung", checks, url)

    def test_three_human_checks_required_without_side_effects(self):
        for missing in ("rights", "content", "privacy"):
            with self.subTest(missing=missing), self.assertRaises(ValueError):
                self.decide(checks={"rights", "content", "privacy"} - {missing})
            self.assertFalse(self.registry.exists())
            self.assertFalse(Path(str(self.registry) + ".lock").exists())

    def test_private_image_blocks_open_text_approval(self):
        self.questions["questions"][0]["media"][0]["license"]["id"] = "LicenseRef-Private"
        self.write()
        with self.assertRaises(ValueError):
            self.decide()
        self.assertEqual(self.decide("rejected")["index"], [])

    def test_source_text_license_and_independent_image_license(self):
        question = self.questions["questions"][0]
        question["provenance"] = {"kind": "adapted", "source_url": "https://de.wikipedia.org/wiki/Synthetic",
                                  "source_revision": "synthetic-42", "modification_note": "Synthetic edit"}
        # An open license alone cannot replace the source's required text license.
        self.write()
        with self.assertRaises(ValueError):
            self.decide()
        question["license"]["id"] = "CC BY-SA 4.0"
        self.write()
        self.assertEqual(len(self.decide()["index"]), 1)
        self.assertEqual(question["media"][0]["license"]["id"], "CC0-1.0")

    def test_declared_original_cannot_hide_saved_third_party_source(self):
        # Stable format carries the source note even when provenance is mislabeled.
        package = ROOT / "tests/fixtures/catalog/1.0.0/golden.zip"
        snapshot = READER["read_catalog"](package)
        question = copy.deepcopy(snapshot.questions["questions"][0])
        question["source_note"] = "https://de.wikipedia.org/wiki/Synthetic"
        question["provenance"] = {"kind": "original"}
        with self.assertRaises(ValueError):
            REVIEW["check_open_rights"](question)

    def test_rejection_feedback_and_revision_then_withdrawal(self):
        rejected = self.decide("rejected")
        self.assertEqual(rejected["index"], [])
        self.assertEqual(rejected["events"][0]["reason"], "Synthetische Fachprüfung")
        approved = self.decide()
        self.assertEqual(len(approved["index"]), 1)
        self.assertEqual(approved["index"][0]["sha256"], hashlib.sha256(self.package.read_bytes()).hexdigest())
        # Both independent readers consume the exact reviewed artifact, without the app or network.
        first = READER["read_catalog"](self.package)
        second_path = Path(self.folder.name) / "second-instance.zip"
        second_path.write_bytes(self.package.read_bytes())
        second = READER["read_catalog"](second_path)
        self.assertEqual(first, second)
        withdrawn = self.decide("withdrawn")
        self.assertEqual(withdrawn["index"], [])
        self.assertEqual(len(withdrawn["events"]), 3)
        self.assertEqual(READER["read_catalog"](second_path), first)
        with self.assertRaises(ValueError):
            self.decide()
        self.assertEqual(REVIEW["load"](self.registry), withdrawn)

    def test_changed_bytes_require_new_content_version(self):
        original = self.decide()
        self.questions["questions"][0]["prompt"] = "Geänderte synthetische Frage?"
        self.write()
        with self.assertRaises(ValueError):
            self.decide()
        self.assertEqual(REVIEW["load"](self.registry), original)
        self.manifest["catalog_version"] = "0.2.0"
        self.write()
        current = self.decide()
        self.assertEqual(len(current["index"]), 2)
        self.assertEqual(current["events"][0], original["events"][0])

    def test_tampered_index_and_incomplete_history_are_rejected(self):
        state = self.decide("rejected")
        state["index"] = [{"url": "https://example.org/unreviewed.zip"}]
        self.registry.write_text(json.dumps(state))
        with self.assertRaises(ValueError):
            REVIEW["load"](self.registry)
        state = copy.deepcopy(state)
        state["index"] = []
        state["events"][0]["decision"] = "approved"
        state["events"][0]["checks"] = ["rights"]
        self.registry.write_text(json.dumps(state))
        with self.assertRaises(ValueError):
            REVIEW["load"](self.registry)

    def test_unsafe_release_locations_cannot_enter_index(self):
        for url in ("http://example.org/a.zip", "https://user:password@example.org/a.zip",
                    "https://example.org/a.zip?token=secret", "https://example.org/a.zip#x"):
            with self.subTest(url=url), self.assertRaises(ValueError):
                self.decide(url=url)
        self.assertFalse(self.registry.exists())

    def test_invalid_archive_never_changes_prior_decision(self):
        state = self.decide()
        self.package.write_bytes(b"invalid archive")
        with self.assertRaises(ValueError):
            self.decide()
        self.assertEqual(REVIEW["load"](self.registry), state)

    def test_concurrent_writer_is_rejected_without_losing_history(self):
        state = self.decide()
        lock = Path(str(self.registry) + ".lock")
        lock.mkdir()
        with self.assertRaises(FileExistsError):
            self.decide("withdrawn")
        self.assertEqual(REVIEW["load"](self.registry), state)
        lock.rmdir()

    def test_package_cannot_be_replaced_by_registry(self):
        before = self.package.read_bytes()
        with self.assertRaises(ValueError):
            REVIEW["decide"](self.package, self.package, "rejected", "reviewer", "reason")
        self.assertEqual(self.package.read_bytes(), before)

    def test_cli_exports_only_approved_entries_and_rejects_missing_registry(self):
        command = ["python3", str(ROOT / "scripts/review-catalog.py"), "index", str(self.registry)]
        missing = subprocess.run(command, capture_output=True, text=True, check=False)
        self.assertNotEqual(missing.returncode, 0)
        self.assertEqual(missing.stdout, "")
        self.decide()
        result = subprocess.run(command, capture_output=True, text=True, check=True)
        self.assertEqual(len(json.loads(result.stdout)["packages"]), 1)
        self.decide("withdrawn")
        result = subprocess.run(command, capture_output=True, text=True, check=True)
        self.assertEqual(json.loads(result.stdout)["packages"], [])

    def test_archived_formats_remain_reviewable_without_migration(self):
        for version in ("0.1.0", "0.2.0", "1.0.0"):
            with self.subTest(version=version):
                package = ROOT / "tests/fixtures/catalog" / version / "golden.zip"
                registry = Path(self.folder.name) / (version + ".json")
                state = REVIEW["decide"](package, registry, "rejected", "reviewer", "Test review")
                self.assertEqual(state["events"][0]["schema_version"], version)
                self.assertEqual(state["events"][0]["sha256"], hashlib.sha256(package.read_bytes()).hexdigest())


if __name__ == "__main__":
    unittest.main()
