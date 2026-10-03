"""Regression fixtures for archive traversal and report-based runner exfiltration."""
import importlib.util
import io
from pathlib import Path
import tarfile
import tempfile
import unittest


def load(name):
    spec = importlib.util.spec_from_file_location(name, Path(__file__).resolve().parents[2] / 'scripts' / (name + '.py'))
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


fetch = load('security-fetch-source')
reports = load('security-copy-reports')


class SandboxTests(unittest.TestCase):
    def archive(self, directory, name, kind=tarfile.REGTYPE, link=''):
        path = directory / 'source.tar.gz'
        with tarfile.open(path, 'w:gz') as package:
            member = tarfile.TarInfo(name)
            member.type = kind
            member.linkname = link
            member.size = 2 if kind == tarfile.REGTYPE else 0
            package.addfile(member, io.BytesIO(b'{}') if member.size else None)
        return path

    def test_source_archive_is_data_and_strips_only_the_repository_prefix(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            fetch.extract(self.archive(root, 'repo/source/package.json'), root / 'output')
            self.assertEqual((root / 'output/source/package.json').read_bytes(), b'{}')

    def test_source_traversal_links_and_special_files_are_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            for index, (name, kind) in enumerate((('repo/../../escape', tarfile.REGTYPE), ('/absolute', tarfile.REGTYPE), ('repo/link', tarfile.SYMTYPE), ('repo/hardlink', tarfile.LNKTYPE), ('repo/fifo', tarfile.FIFOTYPE))):
                with self.subTest(name=name):
                    with self.assertRaises(ValueError):
                        fetch.extract(self.archive(root, name, kind, '/runner/secret'), root / str(index))
            self.assertFalse((root / 'escape').exists())

    def test_regular_report_is_copied(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / 'report.json'
            source.write_bytes(b'{}')
            reports.copy_report(source, root / 'clean/report.json')
            self.assertEqual((root / 'clean/report.json').read_bytes(), b'{}')

    def test_report_cannot_exfiltrate_runner_file_through_symlink(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            (root / 'secret').write_text('synthetic-runner-secret')
            (root / 'report.json').symlink_to(root / 'secret')
            with self.assertRaises(ValueError):
                reports.copy_report(root / 'report.json', root / 'clean.json')
            self.assertFalse((root / 'clean.json').exists())
            (root / 'parent').symlink_to(root, target_is_directory=True)
            with self.assertRaises(ValueError):
                reports.copy_report(root / 'parent/secret', root / 'clean.json')

    def test_oversized_report_is_rejected(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / 'report.json'
            with source.open('wb') as stream:
                stream.truncate(25_000_001)
            with self.assertRaises(ValueError):
                reports.copy_report(source, root / 'clean.json')
