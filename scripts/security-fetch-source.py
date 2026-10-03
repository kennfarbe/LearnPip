#!/usr/bin/env python3
"""Fetch source as data, without checking out executable release code on the runner."""
import argparse
import os
from pathlib import Path, PurePosixPath
import re
import subprocess
import tarfile
import tempfile


def extract(archive, destination):
    if destination.is_symlink() or destination.exists() and any(destination.iterdir()):
        raise ValueError("Source destination must be an empty directory")
    destination.mkdir(parents=True, exist_ok=True)
    count = total = 0
    prefix = None
    with tarfile.open(archive, 'r:gz') as package:
        for member in package:
            count += 1
            total += member.size
            parts = PurePosixPath(member.name).parts
            if count > 10000 or total > 100_000_000 or member.size > 25_000_000:
                raise ValueError("Source archive exceeds audit limits")
            if not parts or member.name.startswith('/') or '..' in parts or not (member.isfile() or member.isdir()):
                raise ValueError("Unsafe source archive entry")
            prefix = prefix or parts[0]
            if parts[0] != prefix:
                raise ValueError("Source archive has multiple roots")
            target = destination.joinpath(*parts[1:])
            if member.isdir():
                target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                with package.extractfile(member) as source, target.open('xb') as output:
                    while chunk := source.read(65536):
                        output.write(chunk)
                target.chmod(member.mode & 0o777)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ref', required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    ref = args.ref
    if not ref or ref == 'main' and os.environ.get('GITHUB_EVENT_NAME') in ('pull_request', 'push'):
        ref = os.environ['GITHUB_SHA']
    if ref != 'main' and not re.fullmatch(r'[a-f0-9]{40}', ref):
        raise ValueError("Unverifiable audit source")
    with tempfile.TemporaryDirectory() as temporary:
        archive = Path(temporary) / 'source.tar.gz'
        with archive.open('wb') as output:
            subprocess.run(['gh', 'api', 'repos/kennfarbe/LearnPip/tarball/' + ref], stdout=output, check=True, timeout=60)
        if archive.stat().st_size > 100_000_000:
            raise ValueError("Source download exceeds audit limits")
        extract(archive, args.output)


if __name__ == '__main__':
    main()
