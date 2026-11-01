"""Guard against allowing a green PR with an untested release security gate."""
from pathlib import Path
import unittest

ROOT = Path(__file__).resolve().parents[2]


class SecurityGateWiringTests(unittest.TestCase):
    def test_pr_and_main_use_same_release_gate(self):
        ci = (ROOT / ".github/workflows/ci.yml").read_text()
        self.assertIn("github.event_name == 'pull_request'", ci)
        self.assertIn("uses: ./.github/workflows/security.yml", ci)
        # Policy can execute in parallel, but the final required status must
        # depend on both policy and the release security workflow.
        self.assertIn("repository-policy:\n    # Run repository policy alongside", ci)
        self.assertIn("needs: [changes]", ci)
        self.assertIn("needs: [changes, repository-policy, markdown, backend, web,", ci)
        self.assertIn("integration-smoke, release-security]", ci)
        self.assertIn("expected_release_gate = 'success' if flags.get('release') == 'true' else 'skipped'", ci)
        self.assertIn("if results['release-security']['result'] != expected_release_gate:", ci)
        self.assertIn("if results['changes']['result'] != 'success' or results['repository-policy']['result'] != 'success':", ci)

    def test_security_candidates_match_release_conditions(self):
        security = (ROOT / ".github/workflows/security.yml").read_text()
        self.assertIn("workflow_call:", security)
        self.assertIn("arch: [amd64, arm64]", security)
        self.assertIn("TRIVY_PLATFORM: linux/${{ matrix.arch }}", security)
        self.assertIn("Audit candidate before publishing", security)
        self.assertIn("Enforce release gate", security)
        self.assertIn("Require complete scans and passing release gates", security)


if __name__ == "__main__":
    unittest.main()
