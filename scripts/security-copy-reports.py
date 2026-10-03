#!/usr/bin/env python3
"""Copy only bounded regular report files out of an untrusted audit sandbox."""
import argparse
import os
from pathlib import Path
import stat


def copy_report(source, destination):
    for parent in (source, *source.parents):
        if parent.is_symlink():
            raise ValueError("Sandbox report symlink rejected")
    if not source.exists():
        return
    with os.fdopen(os.open(source, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK), 'rb') as stream:
        info = os.fstat(stream.fileno())
        if not stat.S_ISREG(info.st_mode) or info.st_size > 25_000_000:
            raise ValueError("Sandbox report is not a bounded regular file")
        data = stream.read(25_000_001)
        if len(data) > 25_000_000:
            raise ValueError("Sandbox report exceeds audit limits")
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_bytes(data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('kind', choices=('npm', 'nuget'))
    parser.add_argument('raw', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    if args.kind == 'nuget':
        paths = ['nuget.json']
    else:
        paths = [f'learnpip-security-{component}-{scope}/{name}.json'
                 for component in ('root', 'web') for scope in ('production', 'development')
                 for name in ('npm-audit-private', 'npm-after-fix-private', 'npm-fix-private')]
        paths += ['learnpip-override-trial/' + name for name in ('package.json', 'package-lock.json', 'override-audit.json')]
    for path in paths:
        copy_report(args.raw / path, args.output / path)


if __name__ == '__main__':
    main()
