#!/usr/bin/env python3
"""Restricted LearnPip update operator. Run as the rootless Docker owner, never in the API container."""
import fcntl, json, os, re, shutil, subprocess, sys, time, urllib.request
from datetime import datetime, timezone
from pathlib import Path

STABLE = re.compile(r"^v\d+\.\d+\.\d+$")
REPO = "kennfarbe/LearnPip"
HOME = Path(os.environ.get("LEARNPIP_HOME", str(Path.home() / "learnpip"))).resolve()
QUEUE = Path(os.environ.get("LEARNPIP_UPDATE_QUEUE", str(HOME / "shared/update-queue"))).resolve()
STATUS = Path(os.environ.get("LEARNPIP_UPDATE_STATUS", str(HOME / "shared/update-status"))).resolve()
MIN_FREE = int(os.environ.get("LEARNPIP_UPDATE_MIN_FREE_BYTES", str(2 * 1024**3)))

def write(job, state, phase, message=None):
    job.update(state=state, phase=phase, message=message, updatedAtUtc=datetime.now(timezone.utc).isoformat())
    STATUS.mkdir(parents=True, exist_ok=True)
    tmp = STATUS / ("." + job["id"].replace("-", "") + ".tmp")
    out = STATUS / (job["id"].replace("-", "") + ".json")
    tmp.write_text(json.dumps(job, separators=(",", ":")))
    os.chmod(tmp, 0o600)
    tmp.replace(out)

def run(command, cwd=None):
    result = subprocess.run(command, cwd=cwd, text=True, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, timeout=3600)
    if result.returncode:
        tail = "\n".join(result.stdout.splitlines()[-12:])
        raise RuntimeError(tail or f"command failed ({result.returncode})")

def validate_release(version):
    if not STABLE.fullmatch(version): raise RuntimeError("invalid stable version")
    request = urllib.request.Request(
        f"https://api.github.com/repos/{REPO}/releases/tags/{version}",
        headers={"User-Agent": "LearnPip-update-operator/1"})
    with urllib.request.urlopen(request, timeout=20) as response:
        release = json.load(response)
    if release.get("draft") or release.get("prerelease") or release.get("tag_name") != version:
        raise RuntimeError("release is not a verified stable tag")
    if not str(release.get("html_url", "")).startswith(f"https://github.com/{REPO}/releases/"):
        raise RuntimeError("unexpected release source")

def process(path):
    job = json.loads(path.read_text())
    version = str(job.get("targetVersion", ""))
    if not STABLE.fullmatch(version): raise RuntimeError("invalid target version")
    current = (HOME / "current").resolve(strict=True)
    installer = current / "scripts/install-release.sh"
    backup = current / "scripts/backup-prod.sh"
    if not installer.is_file() or not backup.is_file(): raise RuntimeError("release tools missing")
    write(job, "running", "preflight", "Voraussetzungen werden geprüft.")
    validate_release(version)
    if os.geteuid() == 0: raise RuntimeError("operator must not run as root")
    info = subprocess.run(["docker", "info", "--format", "{{json .SecurityOptions}}"],
                          text=True, capture_output=True, timeout=20)
    if info.returncode or "rootless" not in info.stdout: raise RuntimeError("rootless Docker required")
    if shutil.disk_usage(HOME).free < MIN_FREE: raise RuntimeError("insufficient free disk space")
    write(job, "running", "backup", "Konsistentes Backup wird erstellt.")
    run([str(backup)], cwd=current)
    write(job, "running", "install", "Release wird installiert und migriert.")
    run([str(installer), "update", "--version", version, "--yes"], cwd=current)
    active = (HOME / "current" / ".learnpip-release")
    if not active.is_file() or active.read_text().strip() != version:
        raise RuntimeError("active version was not confirmed")
    write(job, "succeeded", "complete", "Update und Healthcheck erfolgreich.")

def main():
    QUEUE.mkdir(parents=True, exist_ok=True); STATUS.mkdir(parents=True, exist_ok=True)
    lock_path = HOME / "shared/update-operator.lock"
    lock_path.parent.mkdir(parents=True, exist_ok=True)
    with lock_path.open("w") as lock:
        fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        while True:
            jobs = sorted(QUEUE.glob("*.json"), key=lambda p: p.stat().st_mtime)
            if not jobs: time.sleep(3); continue
            path = jobs[0]
            try: process(path)
            except Exception as exc:
                try:
                    job = json.loads(path.read_text())
                    write(job, "failed", "failed", str(exc)[-1000:])
                except Exception: pass
            finally:
                path.unlink(missing_ok=True)

if __name__ == "__main__":
    try: main()
    except BlockingIOError: sys.exit("another update operator is already running")
