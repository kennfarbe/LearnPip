#!/usr/bin/env python3
"""Lokale Ersteinrichtung über das bereits installierte Compose-Projekt."""

import getpass
import json
from pathlib import Path
import re
import shlex
import signal
import subprocess
import sys


class SetupError(Exception):
    """Verständliche eigene Diagnose ohne fremde Prozessausgaben."""


def read_value(prompt, secret=False):
    """TTY-Passwörter verdecken; bei EOF niemals erneut fragen."""
    if secret and sys.stdin.isatty():
        return getpass.getpass(prompt, stream=sys.stderr)
    print(prompt, end='', file=sys.stderr, flush=True)
    line = sys.stdin.readline()
    if not line:
        raise EOFError
    return line.removesuffix('\n')


def password_problem(value):
    """.NET zählt UTF-16-Codeeinheiten; keine zusätzlichen Zeichenklassen."""
    length = len(value.encode('utf-16-le')) // 2
    if not 12 <= length <= 128:
        return 1
    if '\r' in value or '\n' in value or '\0' in value:
        return 2
    # Entspricht Char.IsWhiteSpace; Python zählt zusätzlich U+001C–U+001F.
    if not value or all(c.isspace() and c not in '\x1c\x1d\x1e\x1f' for c in value):
        return 3
    return 0


def deployment(root):
    """Nur bestehende Einstellungen und Datenbank verwenden, nichts starten."""
    config = root / 'deploy/.env.production'
    for path in (config, root / 'deploy/compose.prod.yaml',
                 root / 'deploy/secrets/ConnectionStrings__LearnPip',
                 root / 'deploy/secrets/postgres_password'):
        if not path.is_file() or not path.stat().st_size:
            raise SetupError('Installation unvollständig: Deployment-Konfiguration oder Datenbank-Secret fehlt. Zuerst Installation/Update abschließen.')
    compose = ['docker', 'compose', '--env-file', str(config), '-f', str(root / 'deploy/compose.prod.yaml')]
    internal = 'false'
    for line in config.read_text().splitlines():
        match = re.match(r'^\s*(?:export\s+)?LEARNPIP_INTERNAL\s*=\s*(.*)$', line)
        if match:
            values = shlex.split(match[1], comments=True)
            internal = values[0] if len(values) == 1 else ''
    if internal not in ('true', 'false'):
        raise SetupError('LEARNPIP_INTERNAL muss true oder false sein.')
    if internal == 'true':
        compose.extend(['-f', str(root / 'deploy/compose.internal.yaml')])
    info = subprocess.run(['docker', 'info', '--format', '{{json .SecurityOptions}}'],
                          cwd=root, capture_output=True, text=True, check=True)
    if any('rootless' in option for option in json.loads(info.stdout)):
        compose.extend(['-f', str(root / 'deploy/compose.rootless.yaml')])
    subprocess.run([*compose, 'config', '--quiet'], cwd=root, capture_output=True, check=True)
    database = subprocess.run([*compose, 'ps', '--all', '--quiet', 'db'],
                              cwd=root, capture_output=True, text=True, check=True)
    if not database.stdout.strip():
        raise SetupError('Keine bestehende Datenbank im Compose-Projekt gefunden. Es wird kein zweites Deployment angelegt. Docker-Kontext und Installation prüfen.')
    return compose


def main():
    """Passwort prüfen und einmalig an den vorhandenen Bootstrap übertragen."""
    if sys.argv[1:] == ['--help']:
        print('Aufruf: ./scripts/setup-admin.sh\nAdministrator in der bestehenden Installation einrichten; Benutzername und Passwort werden abgefragt.')
        return 0
    if sys.argv[1:]:
        print('Keine Zugangsdaten als Argumente übergeben. Aufruf: ./scripts/setup-admin.sh', file=sys.stderr)
        return 1
    root = Path(__file__).resolve().parents[1]
    password = confirmation = payload = None
    try:
        compose = deployment(root)
        while True:
            username = read_value('Administrator-Benutzername [admin]: ').strip() or 'admin'
            if re.fullmatch(r'[a-zA-Z0-9_.-]{1,64}', username):
                break
            print('Benutzername: 1–64 ASCII-Buchstaben/Ziffern, Punkt, Unterstrich oder Bindestrich. Bitte erneut eingeben.', file=sys.stderr)
        print('Passwort: 12–128 Zeichen, nicht nur Leerraum; keine verpflichtenden Zeichenklassen.', file=sys.stderr)
        while True:
            password = read_value('Passwort: ', secret=True)
            problem = password_problem(password)
            if problem:
                if problem == 1:
                    print('Passwort muss 12–128 Zeichen (UTF-16-Codeeinheiten) lang sein. Bitte erneut eingeben.', file=sys.stderr)
                elif problem == 2:
                    print('Passwort darf keine Zeilenumbrüche oder Nullzeichen enthalten. Bitte erneut eingeben.', file=sys.stderr)
                else:
                    print('Passwort darf nicht ausschließlich aus Leerraum bestehen. Bitte erneut eingeben.', file=sys.stderr)
                password = None
                continue
            confirmation = read_value('Passwort wiederholen: ', secret=True)
            if password == confirmation:
                break
            print('Passwörter stimmen nicht überein. Bitte beide Eingaben wiederholen.', file=sys.stderr)
            password = confirmation = None
        payload = (username + '\n' + password + '\n').encode('utf-8')
        result = subprocess.run([*compose, '--profile', 'ops', 'run', '--rm', '--no-deps', '-T', 'initialize-admin'],
                                cwd=root, input=payload, capture_output=True)
        if result.returncode == 10:
            print('Administrator bereits eingerichtet; unverändert. Passwort-Reset ist eine getrennte Aktion.')
            return 0
        if result.returncode:
            print('Administrator-Einrichtung fehlgeschlagen (Docker/Backend). Datenbankzustand und Installation prüfen; keine Erfolgsmeldung.', file=sys.stderr)
            return 1
        print('Administrator eingerichtet. Anmeldung mit dem gewählten Benutzernamen und Passwort unter Einstellungen möglich.')
        return 0
    except (EOFError, KeyboardInterrupt):
        print('\nEingabe beendet oder Einrichtung abgebrochen. Bei bereits laufendem Ops-Aufruf den Administratorstatus prüfen.', file=sys.stderr)
        return 1
    except SetupError as error:
        print(str(error), file=sys.stderr)
        return 1
    except (OSError, ValueError, subprocess.SubprocessError):
        print('Administrator-Einrichtung nicht möglich. Installation, Deployment-Konfiguration, vorhandene Datenbank und Docker-Zugriff prüfen.', file=sys.stderr)
        return 1
    finally:
        # Keine Passwortvariablen an die Umgebung exportieren oder persistieren.
        del password, confirmation, payload


def interrupt(*_):
    """Auch SIGTERM durch den gemeinsamen Aufräumpfad führen."""
    raise KeyboardInterrupt


if __name__ == '__main__':
    signal.signal(signal.SIGTERM, interrupt)
    sys.exit(main())
