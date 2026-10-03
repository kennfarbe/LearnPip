"""Installer tests with fake HTTPS/Docker, never touching a real deployment."""
import io
import os
import pwd
from pathlib import Path
import subprocess
import tarfile
import tempfile

repo = Path(__file__).resolve().parents[2]
assert os.geteuid() == 0, 'Run with sudo python3 tests/ops/install-release-smoke.py'
with tempfile.TemporaryDirectory(prefix='learnpip-installer-test-') as temporary:
    work = Path(temporary)
    fixture = work / 'release.tar.gz'
    with tarfile.open(fixture, 'w:gz') as archive:
        for name in ['deploy/compose.prod.yaml', 'deploy/compose.rootless.yaml', 'deploy/compose.internal.yaml', 'deploy/Caddyfile.internal', 'deploy/.env.production.example', 'scripts/prod-init.sh']:
            archive.add(repo / name, arcname='LearnPip-1.0.0/' + name)
        data = b'#!/bin/bash\necho backup >> "$MOCK_LOG"\n'
        entry = tarfile.TarInfo('LearnPip-1.0.0/scripts/backup-prod.sh')
        entry.size = len(data)
        entry.mode = 0o755
        archive.addfile(entry, io.BytesIO(data))
    binaries = work / 'bin'
    binaries.mkdir()
    scripts = {
        'curl': '''#!/usr/bin/env python3
import json,os,shutil,sys
if '-o' in sys.argv: shutil.copyfile(os.environ['MOCK_ARCHIVE'],sys.argv[sys.argv.index('-o')+1])
else:
    version = sys.argv[-1].rsplit('/',1)[-1] if '/tags/' in sys.argv[-1] else 'v1.0.0'
    print(json.dumps({'draft':False,'prerelease':os.environ.get('MOCK_PRERELEASE')=='1','tag_name':version}))
''',
        'docker': '''#!/bin/bash
echo "docker $*" >> "$MOCK_LOG"
if [[ "$*" == 'info --format '* ]]; then
  if [[ "${MOCK_ROOTFUL:-}" == 1 ]]; then echo '[]'; else echo '["name=rootless"]'; fi
fi
if [[ "$*" == *'run --rm migrate'* && "${MOCK_FAIL_MIGRATE:-}" == 1 ]]; then exit 1; fi
''',
    }
    for name, content in scripts.items():
        path = binaries / name
        path.write_text(content)
        path.chmod(0o755)
    log = work / 'commands.log'
    env = dict(os.environ, PATH=str(binaries) + ':' + os.environ['PATH'],
               MOCK_ARCHIVE=str(fixture), MOCK_LOG=str(log))
    target = work / 'installation'

    def run(*args, directory=target, extra=None, success=True):
        result = subprocess.run(['bash', str(repo / 'scripts/install-release.sh'), *args,
                                 '--directory', str(directory)], env=env | (extra or {}),
                                text=True, capture_output=True)
        assert (result.returncode == 0) == success, result.stdout + result.stderr
        return result

    # A release upgrading an older installation must mount the same persistent
    # key volume for API startup and for the preceding migration process.
    compose_text = (repo / 'deploy/compose.prod.yaml').read_text()
    migration = compose_text.split('  migrate:', 1)[1].split('  api:', 1)[0]
    api = compose_text.split('  api:', 1)[1].split('  worker:', 1)[0]
    volumes = compose_text.split('volumes:', 1)[-1]
    mount = 'data-protection-keys:/var/lib/learnpip/data-protection'
    assert mount in migration and mount in api
    assert '  data-protection-keys:' in volumes

    compose_text = (repo / 'deploy/compose.prod.yaml').read_text()
    for provider in ('apple', 'microsoft'):
        secret = f'Oidc__Providers__{provider}__ClientSecret'
        assert f'      - {secret}' in compose_text
        assert f'  {secret}:\n    file: ./secrets/{secret}' in compose_text
        assert secret in (repo / 'scripts/prod-init.sh').read_text()

    run('prepare')
    assert not (target / 'current').exists()
    assert not log.exists(), 'prepare must not invoke Docker'
    password = (target / 'shared/secrets/postgres_password').read_bytes()
    assert password
    run('prepare')  # reuse only verified prepared releases
    assert (target / 'shared/secrets/postgres_password').read_bytes() == password
    run('install', '--domain', 'learn.test.invalid', '--internal', '--yes')
    assert (target / 'current').resolve() == target / 'releases/v1.0.0'
    assert 'LEARNPIP_INTERNAL=true' in (target / 'shared/.env.production').read_text()
    assert 'LEARNPIP_HTTP_PORT=8080' in (target / 'shared/.env.production').read_text()
    assert 'LEARNPIP_HTTPS_PORT=8443' in (target / 'shared/.env.production').read_text()
    assert '-f deploy/compose.internal.yaml' in log.read_text()
    run('install', '--yes', success=False)
    run('update', '--yes')  # same version is a no-op
    run('update', '--version', 'v1.0.1', '--yes')
    assert (target / 'current').resolve() == target / 'releases/v1.0.1'
    assert (target / 'shared/secrets/postgres_password').read_bytes() == password
    assert 'LEARNPIP_INTERNAL=true' in (target / 'shared/.env.production').read_text()
    assert 'LEARNPIP_HTTP_PORT=8080' in (target / 'shared/.env.production').read_text()
    assert 'LEARNPIP_HTTPS_PORT=8443' in (target / 'shared/.env.production').read_text()
    commands = log.read_text()
    command_lines = commands.splitlines()
    backup_index = max(i for i, line in enumerate(command_lines) if line == 'backup')
    pull_index = max(i for i, line in enumerate(command_lines) if line.startswith('docker compose ') and line.endswith(' pull'))
    assert backup_index < pull_index
    assert not any(line.startswith('docker compose ') and line.endswith(' build') for line in command_lines)
    env_text = (target / 'shared/.env.production').read_text()
    assert 'LEARNPIP_VERSION=v1.0.1' in env_text
    run('update', '--version', 'v1.0.2', '--yes', extra={'MOCK_FAIL_MIGRATE': '1'}, success=False)
    assert (target / 'current').resolve() == target / 'releases/v1.0.1'
    run('prepare', directory=work / 'prerelease', extra={'MOCK_PRERELEASE': '1'}, success=False)
    run('prepare', '--version', '../evil', directory=work / 'invalid', success=False)
    malicious = work / 'malicious.tar.gz'
    with tarfile.open(malicious, 'w:gz') as archive:
        entry = tarfile.TarInfo('../escaped')
        entry.size = 1
        archive.addfile(entry, io.BytesIO(b'x'))
    run('prepare', directory=work / 'unsafe', extra={'MOCK_ARCHIVE': str(malicious)}, success=False)
    assert not (work / 'escaped').exists()
    incomplete = work / 'incomplete'
    (incomplete / 'releases/v1.0.0').mkdir(parents=True)
    run('prepare', directory=incomplete, success=False)
    # Run the actual script as an unprivileged account, not a fake EUID.
    user = pwd.getpwnam('nobody')
    mapped = any(int(line.split()[0]) <= user.pw_uid < int(line.split()[0]) + int(line.split()[2])
                 for line in Path('/proc/self/uid_map').read_text().splitlines())
    if mapped:
        work.chmod(0o755)
        user_area = work / 'user-area'
        user_area.mkdir()
        os.chown(user_area, user.pw_uid, user.pw_gid)
        user_script = work / 'installer.sh'
        user_script.write_text((repo / 'scripts/install-release.sh').read_text())
        user_script.chmod(0o644)
        user_env = env | {'MOCK_LOG': str(user_area / 'commands.log')}
        command = ['runuser', '-u', 'nobody', '--', 'bash', str(user_script)]
        for args in [('install', '--version', 'v1.0.0', '--domain', 'learn.test.invalid'),
                     ('update', '--version', 'v1.0.1')]:
            result = subprocess.run(command + list(args) + ['--directory', str(user_area / 'learnpip'), '--yes'],
                                    env=user_env, text=True, capture_output=True)
            assert result.returncode == 0, result.stdout + result.stderr
        assert '-f deploy/compose.rootless.yaml' in (user_area / 'commands.log').read_text()
        result = subprocess.run(command + ['update', '--version', 'v1.0.2', '--directory', str(user_area / 'learnpip'), '--yes'],
                                env=user_env | {'MOCK_ROOTFUL': '1'}, text=True, capture_output=True)
        assert result.returncode != 0 and 'effectively root' in result.stderr
        print('PASS: install/update as nobody with rootless Docker mock; rootful daemon rejected for non-root user')
    else:
        print('SKIP: unprivileged account is not mapped in this user namespace (runs on GitHub-hosted CI)')
    print('PASS: prepare, install, update, secret preservation, backup ordering, migration failure, stable-only metadata, path/archive validation and overwrite refusal')
