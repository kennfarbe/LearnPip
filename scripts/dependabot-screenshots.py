"""Publish only validated screenshot data from an unprivileged Dependabot CI run."""

import base64
import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import sys

NAMES = ("overview", "question-editor", "catalogs", "learning-mobile-dark", "admin-password")
GENERATED = {f"docs/screenshots/{name}.png" for name in NAMES} | {"docs/screenshots/manifest.json"}
DEPENDENCIES = {"frontend/web/package.json", "frontend/web/package-lock.json"}


def api(endpoint, method="GET", payload=None):
    args = ["gh", "api", endpoint, "--method", method]
    if payload is not None:
        args += ["--input", "-"]
    env = os.environ.copy()
    if method == "GET" and env.get("GH_TOKEN_READ"):
        env["GH_TOKEN"] = env["GH_TOKEN_READ"]
    result = subprocess.run(args, input=json.dumps(payload) if payload is not None else None,
                            text=True, capture_output=True, check=True, env=env)
    return json.loads(result.stdout)


def eligible_files(files):
    paths = {item["filename"] for item in files}
    return bool(paths & DEPENDENCIES) and paths <= DEPENDENCIES | GENERATED and all(
        item["status"] == "modified" for item in files)


def context():
    repo = os.environ["GITHUB_REPOSITORY"]
    event = json.loads(Path(os.environ["GITHUB_EVENT_PATH"]).read_text())
    run = event["workflow_run"]
    if run["event"] != "pull_request" or run["path"] != ".github/workflows/ci.yml":
        return None
    if run["head_repository"]["full_name"] != repo:
        return None
    prs = api(f"repos/{repo}/pulls?state=open&head={repo.split('/')[0]}:{run['head_branch']}")
    for pr in prs:
        if (pr["user"]["login"] != "dependabot[bot]" or pr["base"]["ref"] != "main"
                or pr["head"]["repo"]["full_name"] != repo
                or pr["head"]["sha"] != run["head_sha"]):
            continue
        # The list endpoint omits changed_files. Fetch the full PR and recheck
        # its identity/head because Dependabot can update it between requests.
        pr = api(f"repos/{repo}/pulls/{pr['number']}")
        if (pr["state"] != "open" or pr["user"]["login"] != "dependabot[bot]"
                or pr["base"]["ref"] != "main"
                or pr["head"]["repo"]["full_name"] != repo
                or pr["head"]["sha"] != run["head_sha"]):
            continue
        files = api(f"repos/{repo}/pulls/{pr['number']}/files?per_page=100")
        if pr["changed_files"] != len(files) or not eligible_files(files):
            continue
        artifacts = api(f"repos/{repo}/actions/runs/{run['id']}/artifacts?per_page=100")
        matches = [a for a in artifacts["artifacts"]
                   if a["name"] == "dependabot-screenshots" and not a["expired"]]
        if len(matches) == 1 and matches[0]["size_in_bytes"] <= 10 * 1024 * 1024:
            return repo, run, pr
    return None


def validate(directory):
    directory = Path(directory)
    expected = {f"{name}.png" for name in NAMES} | {"manifest.json"}
    if {p.name for p in directory.iterdir()} != expected:
        raise ValueError("Unexpected screenshot artifact files")
    if any(p.is_symlink() or not p.is_file() or p.stat().st_size > 5 * 1024 * 1024
           for p in directory.iterdir()):
        raise ValueError("Invalid screenshot artifact entry")
    manifest = json.loads((directory / "manifest.json").read_text())
    if set(manifest) != {"sourceHash", "images"}:
        raise ValueError("Invalid screenshot manifest")
    if (not isinstance(manifest["sourceHash"], str) or len(manifest["sourceHash"]) != 64
            or any(c not in "0123456789abcdef" for c in manifest["sourceHash"])):
        raise ValueError("Invalid source hash")
    if set(manifest["images"]) != {f"{name}.png" for name in NAMES}:
        raise ValueError("Unexpected manifest images")
    for name in NAMES:
        data = (directory / f"{name}.png").read_bytes()
        if len(data) < 33 or data[:8] != bytes.fromhex("89504e470d0a1a0a") or data[12:16] != b"IHDR":
            raise ValueError("Invalid PNG")
        width, height = struct.unpack(">II", data[16:24])
        if not (320 <= width <= 4000 and 400 <= height <= 10000):
            raise ValueError("Invalid screenshot dimensions")
        if manifest["images"][f"{name}.png"] != {
                "sha256": hashlib.sha256(data).hexdigest(), "width": width, "height": height}:
            raise ValueError("Screenshot hash or dimensions do not match")
    return directory


def publish(directory, ctx):
    repo, run, pr = ctx
    directory = validate(directory)
    head = pr["head"]["sha"]
    parent = api(f"repos/{repo}/git/commits/{head}")
    entries = []
    for name in sorted(directory.iterdir()):
        data = name.read_bytes()
        # Git blob hashes let unchanged images remain untouched.
        blob_sha = hashlib.sha1(f"blob {len(data)}\0".encode() + data).hexdigest()
        current = api(f"repos/{repo}/contents/docs/screenshots/{name.name}?ref={head}")
        if current["sha"] == blob_sha:
            continue
        blob = api(f"repos/{repo}/git/blobs", "POST", {
            "content": base64.b64encode(data).decode(), "encoding": "base64"})
        entries.append({"path": f"docs/screenshots/{name.name}", "mode": "100644",
                        "type": "blob", "sha": blob["sha"]})
    if not entries:
        print("Documentation screenshots are already current.")
        return
    tree = api(f"repos/{repo}/git/trees", "POST", {
        "base_tree": parent["tree"]["sha"], "tree": entries})
    commit = api(f"repos/{repo}/git/commits", "POST", {
        "message": "docs(web): refresh screenshots after dependency updates",
        "tree": tree["sha"], "parents": [head]})
    # Never overwrite a newer Dependabot or maintainer commit.
    fresh = api(f"repos/{repo}/pulls/{pr['number']}")
    if fresh["state"] != "open" or fresh["head"]["sha"] != head:
        raise ValueError("PR changed while screenshots were prepared; wait for its next CI run")
    api(f"repos/{repo}/git/refs/heads/{pr['head']['ref']}", "PATCH", {
        "sha": commit["sha"], "force": False})
    print(f"Updated documentation screenshots in PR #{pr['number']}.")


def main():
    ctx = context()
    if sys.argv[1] == "prepare":
        with open(os.environ["GITHUB_OUTPUT"], "a") as output:
            output.write(f"eligible={'true' if ctx else 'false'}\n")
    elif sys.argv[1] == "publish" and ctx:
        publish(sys.argv[2], ctx)


if __name__ == "__main__":
    main()
