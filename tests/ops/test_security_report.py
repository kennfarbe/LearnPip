"""Synthetic tests for fail-closed aggregate-only vulnerability reporting."""

from pathlib import Path
import runpy
import unittest


MODULE = runpy.run_path(str(Path(__file__).resolve().parents[2] /
                            "scripts" / "security-report.py"))
evaluate = MODULE["evaluate"]
InvalidScan = MODULE["InvalidScan"]


class SecurityReportTests(unittest.TestCase):
    def test_clean_npm_report(self):
        self.assertEqual(evaluate("npm", {"auditReportVersion": 2,
                         "metadata": {"vulnerabilities": {"high": 0, "critical": 0}}}), (0, 0))

    def test_npm_finding(self):
        self.assertEqual(evaluate("npm", {"auditReportVersion": 2,
                         "metadata": {"vulnerabilities": {"high": 2, "critical": 1}}}), (2, 1))

    def test_npm_scanner_failure_is_not_green(self):
        with self.assertRaises(InvalidScan):
            evaluate("npm", {"auditReportVersion": 2, "error": {"code": "ENOAUDIT"}})

    def test_clean_nuget_report(self):
        self.assertEqual(evaluate("nuget", {"projects": [{"frameworks": [
            {"topLevelPackages": [], "transitivePackages": []}]}]}), (0, 0))

    def test_nuget_transitive_finding(self):
        report = {"projects": [{"frameworks": [{"transitivePackages": [
            {"id": "synthetic", "vulnerabilities": [{"severity": "High"}]}]}]}]}
        self.assertEqual(evaluate("nuget", report), (1, 0))

    def test_no_vulnerable_nuget_frameworks_is_green(self):
        self.assertEqual(evaluate("nuget", {"version": 1, "projects": [
            {"path": "synthetic.csproj"}]}), (0, 0))

    def test_missing_nuget_projects_is_not_green(self):
        with self.assertRaises(InvalidScan):
            evaluate("nuget", {"version": 1, "projects": []})

    def test_trivy_image_finding(self):
        report = {"SchemaVersion": 2, "Results": [{"Vulnerabilities": [
            {"Severity": "CRITICAL"}, {"Severity": "HIGH"}]}]}
        self.assertEqual(evaluate("trivy", report), (1, 1))

    def test_trivy_invalid_report_is_not_green(self):
        with self.assertRaises(InvalidScan):
            evaluate("trivy", {"SchemaVersion": 2})


if __name__ == "__main__":
    unittest.main()
