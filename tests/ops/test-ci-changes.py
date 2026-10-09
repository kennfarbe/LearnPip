"""Regression tests for the fail-closed CI change classifier."""

import importlib.util
from pathlib import Path
import unittest

SCRIPT = Path(__file__).resolve().parents[2] / "scripts" / "ci-changes.py"
spec = importlib.util.spec_from_file_location("ci_changes", SCRIPT)
assert spec and spec.loader
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class ChangeClassifierTests(unittest.TestCase):
    def check(self, files, **expected):
        flags = module.classify(files)
        self.assertEqual(flags, {key: expected.get(key, False) for key in module.FLAGS})

    def test_docs_only(self):
        self.check(["README.md", "docs/API.md", "docs/CLA-ENTITY-DRAFT.md"],
                   markdown=True)

    def test_markdown_inside_code_directories_does_not_build(self):
        self.check(["backend/README.md", "frontend/web/README.md",
                    "deploy/NOTES.md"], markdown=True)

    def test_web_only(self):
        self.check(["frontend/web/src/app/app.ts"], web=True,
                   integration=True, release=True)

    def test_backend_only(self):
        self.check(["backend/src/LearnPip.Api/Program.cs"], backend=True,
                   integration=True, release=True)

    def test_both(self):
        self.check(["backend/src/x.cs", "frontend/web/src/x.ts"],
                   backend=True, web=True, integration=True, release=True)

    def test_markdown_and_web(self):
        self.check(["docs/INSTALL.md", "frontend/web/src/x.ts"],
                   markdown=True, web=True, integration=True, release=True)

    def test_docs_screenshots(self):
        self.check(["docs/screenshots/overview.png"], web=True)

    def test_installer(self):
        self.check(["scripts/install-release.sh"], installer=True, release=True)

    def test_admin_setup_uses_installer_checks(self):
        for path in ("scripts/setup-admin.sh", "scripts/setup-admin.py", "tests/ops/test_setup_admin.py"):
            with self.subTest(path=path):
                self.check([path], installer=True, release=True)

    def test_restore(self):
        self.check(["tests/ops/restore-smoke.sh"], restore=True, release=True)

    def test_deployment_affects_integration(self):
        self.check(["deploy/compose.yaml"], installer=True, restore=True,
                   integration=True, release=True)

    def test_markdown_tools(self):
        self.check([".markdownlint-cli2.jsonc"], markdown=True)

    def test_unknown_paths_fail_closed(self):
        self.assertEqual(module.classify([".github/workflows/ci.yml"]), module.ALL)
        self.assertEqual(module.classify(["some-new-directory/config.toml"]), module.ALL)

    def test_manual_and_unknown_diff_force_everything(self):
        self.assertEqual(module.classify([], force_all=True), module.ALL)
        self.assertEqual(module.classify(["README.md"], force_all=True), module.ALL)

    def test_empty_diff_runs_policy_only(self):
        self.check([])

    def test_deleted_file_still_triggers_checks(self):
        self.check(["backend/deleted.cs"], backend=True, integration=True, release=True)


if __name__ == "__main__":
    unittest.main()
