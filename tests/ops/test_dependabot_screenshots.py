"""Regression checks for the privileged screenshot publisher's data boundaries."""

import importlib.util
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("screenshots", ROOT / "scripts/dependabot-screenshots.py")
SCREENSHOTS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SCREENSHOTS)


class ScreenshotPublisherTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.directory = Path(self.temp.name)
        for relative in SCREENSHOTS.GENERATED:
            shutil.copyfile(ROOT / relative, self.directory / Path(relative).name)

    def test_existing_artifact_is_valid(self):
        SCREENSHOTS.validate(self.directory)

    def test_rejects_extra_artifact_file(self):
        (self.directory / "execute.sh").write_text("exit 0")
        with self.assertRaises(ValueError):
            SCREENSHOTS.validate(self.directory)

    def test_rejects_symlink(self):
        target = self.directory / "overview.png"
        target.unlink()
        target.symlink_to(ROOT / "docs/screenshots/overview.png")
        with self.assertRaises(ValueError):
            SCREENSHOTS.validate(self.directory)

    def test_rejects_changed_image(self):
        with (self.directory / "overview.png").open("ab") as image:
            image.write(b"corruption")
        with self.assertRaises(ValueError):
            SCREENSHOTS.validate(self.directory)

    def test_rejects_changed_dimensions(self):
        manifest = self.directory / "manifest.json"
        data = json.loads(manifest.read_text())
        data["images"]["overview.png"]["width"] += 1
        manifest.write_text(json.dumps(data))
        with self.assertRaises(ValueError):
            SCREENSHOTS.validate(self.directory)

    def test_only_dependency_and_generated_paths_are_eligible(self):
        files = [{"filename": path, "status": "modified"}
                 for path in SCREENSHOTS.DEPENDENCIES | SCREENSHOTS.GENERATED]
        self.assertTrue(SCREENSHOTS.eligible_files(files))
        self.assertFalse(SCREENSHOTS.eligible_files(files + [
            {"filename": ".github/workflows/ci.yml", "status": "modified"}]))
        self.assertFalse(SCREENSHOTS.eligible_files([
            {"filename": "frontend/web/package.json", "status": "removed"}]))
        self.assertFalse(SCREENSHOTS.eligible_files([
            {"filename": "docs/screenshots/manifest.json", "status": "modified"}]))

    def test_newer_pr_commit_is_never_overwritten(self):
        calls = []

        def fake_api(endpoint, method="GET", payload=None):
            calls.append((endpoint, method, payload))
            if "/git/commits/" in endpoint:
                return {"tree": {"sha": "base-tree"}}
            if "/contents/" in endpoint:
                return {"sha": "different-blob"}
            if endpoint.endswith("/git/blobs"):
                return {"sha": "blob"}
            if endpoint.endswith("/git/trees"):
                return {"sha": "tree"}
            if endpoint.endswith("/git/commits"):
                return {"sha": "new-commit"}
            return {"state": "open", "head": {"sha": "newer-head"}}

        ctx = ("owner/repo", {}, {"number": 1, "head": {"sha": "old-head", "ref": "dependabot/npm"}})
        with patch.object(SCREENSHOTS, "api", side_effect=fake_api):
            with self.assertRaisesRegex(ValueError, "PR changed"):
                SCREENSHOTS.publish(self.directory, ctx)
        self.assertFalse(any(method == "PATCH" for _, method, _ in calls))

    def test_context_rejects_foreign_repository_before_api_calls(self):
        event = self.directory / "event.json"
        event.write_text(json.dumps({"workflow_run": {
            "event": "pull_request", "path": ".github/workflows/ci.yml",
            "head_repository": {"full_name": "outsider/fork"}}}))
        with patch.dict(os.environ, {"GITHUB_REPOSITORY": "owner/repo", "GITHUB_EVENT_PATH": str(event)}):
            with patch.object(SCREENSHOTS, "api") as mocked:
                self.assertIsNone(SCREENSHOTS.context())
                mocked.assert_not_called()

    def test_context_rejects_stale_ci_and_non_dependabot_author(self):
        event = self.directory / "event.json"
        event.write_text(json.dumps({"workflow_run": {
            "event": "pull_request", "path": ".github/workflows/ci.yml",
            "head_repository": {"full_name": "owner/repo"},
            "head_branch": "dependabot/npm", "head_sha": "old-head"}}))
        pr = {"user": {"login": "dependabot[bot]"}, "base": {"ref": "main"},
              "head": {"repo": {"full_name": "owner/repo"}, "sha": "new-head"}}
        with patch.dict(os.environ, {"GITHUB_REPOSITORY": "owner/repo", "GITHUB_EVENT_PATH": str(event)}):
            with patch.object(SCREENSHOTS, "api", return_value=[pr]) as mocked:
                self.assertIsNone(SCREENSHOTS.context())
                self.assertEqual(mocked.call_count, 1)
            pr["head"]["sha"] = "old-head"
            pr["user"]["login"] = "another-user"
            with patch.object(SCREENSHOTS, "api", return_value=[pr]) as mocked:
                self.assertIsNone(SCREENSHOTS.context())
                self.assertEqual(mocked.call_count, 1)


if __name__ == "__main__":
    unittest.main()
