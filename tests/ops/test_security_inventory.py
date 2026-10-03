"""Deterministic support-policy and immutable platform inventory regressions."""
import importlib.util
from pathlib import Path
import unittest
from unittest.mock import patch
import subprocess

spec = importlib.util.spec_from_file_location('inventory', Path(__file__).resolve().parents[2] / 'scripts/security-inventory.py')
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)


class InventoryTests(unittest.TestCase):
    def test_multiple_supported_releases_are_resolved(self):
        def fetch(endpoint):
            if endpoint.startswith('../'):
                return {'object': {'type': 'commit', 'sha': 'a' * 40}}
            return {'tag_name': 'v2.0.1' if endpoint == 'latest' else 'v1.9.3'}
        result = module.releases({'supported_releases': ['latest', 'v1.9.3']}, fetch)
        self.assertEqual([item['tag'] for item in result], ['v2.0.1', 'v1.9.3'])

    def test_invalid_or_unavailable_release_is_not_green(self):
        with self.assertRaises(ValueError):
            module.releases({'supported_releases': ['../evil']}, lambda _: {})
        with self.assertRaises(ValueError):
            module.releases({'supported_releases': ['latest']}, lambda _: {'tag_name': 'v1.0.0', 'prerelease': True})

    def test_all_components_and_platform_digests(self):
        def inspect(_):
            return {'manifests': [
                {'platform': {'os': 'linux', 'architecture': arch}, 'digest': 'sha256:' + char * 64}
                for arch, char in [('amd64', 'a'), ('arm64', 'b')]]}
        targets = module.image_targets('v1.0.0', ['linux/amd64', 'linux/arm64'], inspect)
        self.assertEqual(len(targets), 10)
        self.assertEqual({item['component'] for item in targets}, {'api', 'worker', 'web', 'db', 'proxy'})
        self.assertTrue(all('@sha256:' in item['ref'] for item in targets))

    def test_missing_supported_platform_blocks_inventory(self):
        with self.assertRaises(ValueError):
            module.image_targets('v1.0.0', ['linux/arm64'], lambda _: {'manifests': []})


class LookupTests(unittest.TestCase):
    def test_transient_registry_failure_is_retried_and_permanent_failure_is_incomplete(self):
        error = subprocess.CalledProcessError(1, ['synthetic'])
        with patch.object(module.subprocess, 'check_output', side_effect=[error, b'{"ok": true}']) as call, patch.object(module.time, 'sleep'):
            self.assertEqual(module.command_json(['synthetic']), {'ok': True})
            self.assertEqual(call.call_count, 2)
        with patch.object(module.subprocess, 'check_output', side_effect=error) as call, patch.object(module.time, 'sleep'):
            with self.assertRaises(ValueError):
                module.command_json(['synthetic'])
            self.assertEqual(call.call_count, 3)


class BuildInventoryTests(unittest.TestCase):
    def test_build_images_are_explicit_and_missing_stages_fail(self):
        self.assertEqual(module.build_images([('build-sdk', 'FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build\n')]), [('build-sdk', 'mcr.microsoft.com/dotnet/sdk:10.0')])
        self.assertEqual(module.build_images([('build-go', 'FROM --platform=$BUILDPLATFORM golang:1.26.8-alpine3.24 AS build\n')]), [('build-go', 'golang:1.26.8-alpine3.24')])
        with self.assertRaises(ValueError):
            module.build_images([('build-sdk', 'FROM missing AS runtime')])


if __name__ == '__main__':
    unittest.main()
