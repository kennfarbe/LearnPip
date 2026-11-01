"""Regression checks for read-only Dependabot screenshot validation."""

import importlib.util
import copy
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

    def test_workflow_does_not_write_to_dependabot_branches(self):
        workflow = (ROOT / ".github/workflows/dependabot-screenshots.yml").read_text()
        self.assertIn('scripts/dependabot-screenshots.py verify', workflow)
        self.assertNotIn("create-github-app-token", workflow)
        self.assertNotIn("permission-contents: write", workflow)
        self.assertNotIn("scripts/dependabot-screenshots.py publish", workflow)

    def test_verify_cli_only_checks_artifact(self):
        with patch.object(SCREENSHOTS, "context") as context:
            with patch.object(SCREENSHOTS, "validate", return_value=self.directory) as checked:
                with patch("sys.argv", ["dependabot-screenshots.py", "verify",
                                        str(self.directory)]):
                    SCREENSHOTS.main()
                checked.assert_called_once_with(str(self.directory))
            context.assert_not_called()

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

    def candidate_context(self, detail_changes=None, files=None):
        event = self.directory / "event.json"
        run = {"id": 42, "event": "pull_request", "path": ".github/workflows/ci.yml",
               "head_repository": {"full_name": "owner/repo"},
               "head_branch": "dependabot/npm", "head_sha": "current-head"}
        event.write_text(json.dumps({"workflow_run": run}))
        # Reproduce the real list response: no changed_files field.
        summary = {"number": 7, "user": {"login": "dependabot[bot]"},
                   "base": {"ref": "main"},
                   "head": {"repo": {"full_name": "owner/repo"}, "sha": "current-head"}}
        detail = copy.deepcopy(summary)
        detail.update(state="open", changed_files=2)
        if detail_changes:
            detail.update(detail_changes)
        if files is None:
            files = [{"filename": name, "status": "modified"} for name in SCREENSHOTS.DEPENDENCIES]
        artifacts = {"artifacts": [{"name": "dependabot-screenshots", "expired": False,
                                    "size_in_bytes": 1000}]}
        responses = {
            "repos/owner/repo/pulls?state=open&head=owner:dependabot/npm": [summary],
            "repos/owner/repo/pulls/7": detail,
            "repos/owner/repo/pulls/7/files?per_page=100": files,
            "repos/owner/repo/actions/runs/42/artifacts?per_page=100": artifacts,
        }
        with patch.dict(os.environ, {"GITHUB_REPOSITORY": "owner/repo", "GITHUB_EVENT_PATH": str(event)}):
            with patch.object(SCREENSHOTS, "api", side_effect=lambda endpoint: responses[endpoint]) as mocked:
                result = SCREENSHOTS.context()
                calls = [call.args[0] for call in mocked.call_args_list]
        return result, calls

    def test_context_fetches_detail_when_list_omits_changed_files(self):
        result, calls = self.candidate_context()
        self.assertEqual(result[2]["changed_files"], 2)
        self.assertEqual(calls[1], "repos/owner/repo/pulls/7")

    def test_context_rechecks_head_and_state_from_detail(self):
        for change in ({"state": "closed"}, {"head": {
                "repo": {"full_name": "owner/repo"}, "sha": "newer-head"}}):
            result, calls = self.candidate_context(change)
            self.assertIsNone(result)
            self.assertEqual(len(calls), 2)

    def test_context_rejects_incomplete_or_disallowed_file_list(self):
        result, calls = self.candidate_context({"changed_files": 101})
        self.assertIsNone(result)
        self.assertEqual(len(calls), 3)
        result, calls = self.candidate_context(files=[
            {"filename": "frontend/web/package.json", "status": "modified"},
            {"filename": ".github/workflows/ci.yml", "status": "modified"}])
        self.assertIsNone(result)
        self.assertEqual(len(calls), 3)


if __name__ == "__main__":
    unittest.main()
