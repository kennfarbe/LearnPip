"""Ersteinrichtung mit synthetischem Docker; keine echte Installation ändern."""

import hashlib
import importlib.util
import json
import os
from pathlib import Path
import pty
import select
import shutil
import signal
import subprocess
import tempfile
import time
import unittest


REPO = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location('setup_admin', REPO / 'scripts/setup-admin.py')
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)

DOCKER = '''#!/usr/bin/env python3
import hashlib,json,os,sys
args = sys.argv[1:]
record = {'args': args, 'cwd': os.getcwd()}
if args[0] == 'info':
    print(json.dumps(['name=rootless'] if os.environ.get('MOCK_ROOTLESS') == '1' else ['name=seccomp']))
elif 'config' in args:
    if os.environ.get('MOCK_DOCKER_FAIL') == '1': sys.exit(1)
elif 'ps' in args:
    if os.environ.get('MOCK_NO_DATABASE') != '1': print('existing-database')
elif args[-1] == 'initialize-admin':
    payload = sys.stdin.buffer.read()
    record['stdin_sha256'] = hashlib.sha256(payload).hexdigest()
    password = payload.decode().split('\\n')[1]
    record['password_in_env'] = any(password in value for value in os.environ.values())
    # Selbst fremde Container-Ausgaben dürfen keinen Klartext weiterreichen.
    print(password)
    print(password, file=sys.stderr)
with open(os.environ['MOCK_LOG'], 'a') as stream: stream.write(json.dumps(record) + '\\n')
if args[-1] == 'initialize-admin': sys.exit(int(os.environ.get('MOCK_STATUS', '0')))
'''


class SetupAdminTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='learnpip-admin-test-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name) / 'custom installation' / 'releases' / 'v1.0.0'
        (self.root / 'scripts').mkdir(parents=True)
        (self.root / 'deploy/secrets').mkdir(parents=True)
        for name in ('setup-admin.sh', 'setup-admin.py'):
            shutil.copy2(REPO / 'scripts' / name, self.root / 'scripts' / name)
        for name in ('compose.prod.yaml', 'compose.internal.yaml', 'compose.rootless.yaml'):
            (self.root / 'deploy' / name).write_text('name: learnpip\n')
        for name in ('ConnectionStrings__LearnPip', 'postgres_password'):
            (self.root / 'deploy/secrets' / name).write_text('synthetic-fixture')
        self.config = self.root / 'deploy/.env.production'
        self.config.write_text('LEARNPIP_INTERNAL=false\nCOMPOSE_PROJECT_NAME=learnpip\n')
        self.current = self.root.parents[1] / 'current'
        self.current.symlink_to(self.root, target_is_directory=True)
        binary = Path(self.temporary.name) / 'bin'
        binary.mkdir()
        docker = binary / 'docker'
        docker.write_text(DOCKER)
        docker.chmod(0o755)
        self.log = Path(self.temporary.name) / 'commands.jsonl'
        self.env = dict(os.environ, PATH=str(binary) + ':' + os.environ['PATH'], MOCK_LOG=str(self.log))
        self.password = '  synthetic-password-123  '

    def run_script(self, text, **extra):
        result = subprocess.run([str(self.current / 'scripts/setup-admin.sh')], cwd='/tmp',
                                env=self.env | extra, input=text, text=True, capture_output=True, timeout=10)
        self.assertNotIn(self.password, result.stdout + result.stderr)
        return result

    def calls(self):
        return [json.loads(line) for line in self.log.read_text().splitlines()] if self.log.exists() else []

    def bootstrap(self):
        return [call for call in self.calls() if call['args'][-1] == 'initialize-admin']

    def assert_created(self, result, username='admin'):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        call, = self.bootstrap()
        expected = (username + '\n' + self.password + '\n').encode()
        self.assertEqual(call['stdin_sha256'], hashlib.sha256(expected).hexdigest())
        self.assertFalse(call['password_in_env'])
        self.assertEqual(call['cwd'], str(self.root))
        self.assertIn('--no-deps', call['args'])
        self.assertNotIn(self.password, json.dumps(self.calls()))
        self.assertEqual(list(self.root.rglob('*password*')), [self.root / 'deploy/secrets/postgres_password'])

    def test_invalid_password_retries_and_preserves_spaces(self):
        result = self.run_script('\nshort\n' + self.password + '\n' + self.password + '\n')
        self.assertIn('12–128', result.stderr)
        self.assert_created(result)

    def test_mismatched_confirmation_retries_both_inputs(self):
        result = self.run_script('\nvalid-first-password\ndifferent-password\n' + self.password + '\n' + self.password + '\n')
        self.assertIn('stimmen nicht', result.stderr)
        self.assert_created(result)

    def test_invalid_username_retries(self):
        result = self.run_script('invalid name\nTech.Admin-1\n' + self.password + '\n' + self.password + '\n')
        self.assertIn('ASCII', result.stderr)
        self.assert_created(result, 'Tech.Admin-1')

    def test_existing_admin_is_successful_noop(self):
        result = self.run_script('\n' + self.password + '\n' + self.password + '\n', MOCK_STATUS='10')
        self.assertEqual(result.returncode, 0)
        self.assertIn('bereits eingerichtet; unverändert', result.stdout)
        self.assertEqual(len(self.bootstrap()), 1)

    def test_backend_failure_never_claims_success(self):
        result = self.run_script('\n' + self.password + '\n' + self.password + '\n', MOCK_STATUS='1')
        self.assertNotEqual(result.returncode, 0)
        self.assertNotIn('Administrator eingerichtet.', result.stdout)
        self.assertEqual(len(self.bootstrap()), 1)

    def test_eof_during_each_prompt_never_bootstraps(self):
        for text in ('', '\n', '\n' + self.password + '\n', '\nshort\n'):
            with self.subTest(text_length=len(text)):
                result = self.run_script(text)
                self.assertNotEqual(result.returncode, 0)
                self.assertIn('Eingabe beendet', result.stderr)
        self.assertEqual(self.bootstrap(), [])

    def test_missing_installation_and_wrong_project_are_rejected(self):
        self.config.unlink()
        result = self.run_script('')
        self.assertIn('Installation unvollständig', result.stderr)
        self.config.write_text('LEARNPIP_INTERNAL=false\n')
        result = self.run_script('', MOCK_NO_DATABASE='1')
        self.assertIn('kein zweites Deployment', result.stderr)
        self.assertEqual(self.bootstrap(), [])

    def test_docker_failure_stops_before_credentials(self):
        result = self.run_script('', MOCK_DOCKER_FAIL='1')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('Docker-Zugriff', result.stderr)
        self.assertNotIn('Passwort:', result.stderr)
        self.assertEqual(self.bootstrap(), [])

    def test_internal_rootless_and_custom_project_settings(self):
        self.config.write_text('LEARNPIP_INTERNAL="true" # LAN\nCOMPOSE_PROJECT_NAME=custom-project\n')
        result = self.run_script('\n' + self.password + '\n' + self.password + '\n', MOCK_ROOTLESS='1', COMPOSE_PROJECT_NAME='custom-project')
        self.assert_created(result)
        args = self.bootstrap()[0]['args']
        for name in ('compose.prod.yaml', 'compose.internal.yaml', 'compose.rootless.yaml'):
            self.assertIn(str(self.root / 'deploy' / name), args)
        self.assertIn(str(self.config), args)

    def test_backend_password_boundaries_unicode_and_control_characters(self):
        for value in ('a' * 12, 'a' * 128, '😀' * 6, '😀' * 64, '\x1c' * 12):
            self.assertIsNone(MODULE.password_error(value))
        for value in ('a' * 11, 'a' * 129, '😀' * 65, ' ' * 12, '\u2003' * 12, 'valid-password\r', 'valid-password\0'):
            self.assertIsNotNone(MODULE.password_error(value))

    def test_terminal_password_is_hidden_and_abort_restores_terminal(self):
        master, slave = pty.openpty()
        self.addCleanup(os.close, master)
        self.addCleanup(os.close, slave)
        process = subprocess.Popen([str(self.current / 'scripts/setup-admin.sh')], stdin=slave,
                                   stderr=slave, stdout=subprocess.PIPE, env=self.env, start_new_session=True)
        self.addCleanup(lambda: process.kill() if process.poll() is None else None)
        transcript = b''

        def wait_for(marker):
            nonlocal transcript
            deadline = time.monotonic() + 5
            while marker not in transcript and time.monotonic() < deadline:
                if select.select([master], [], [], 0.1)[0]:
                    transcript += os.read(master, 4096)
            self.assertIn(marker, transcript)

        wait_for(b'Administrator-Benutzername')
        os.write(master, b'\n')
        wait_for(b'Zeichenklassen.\r\nPasswort: ')
        # Abbruch während verdeckter Eingabe darf keine Einrichtung starten.
        os.write(master, self.password.encode())
        process.send_signal(signal.SIGTERM)
        output, _ = process.communicate(timeout=5)
        while select.select([master], [], [], 0)[0]:
            transcript += os.read(master, 4096)
        self.assertNotIn(self.password.encode(), transcript + output)
        self.assertNotEqual(process.returncode, 0)
        self.assertEqual(self.bootstrap(), [])
        import termios
        self.assertTrue(termios.tcgetattr(slave)[3] & termios.ECHO)


if __name__ == '__main__':
    unittest.main()
