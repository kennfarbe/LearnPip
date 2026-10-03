#!/usr/bin/env python3
"""Resolve the explicit support policy and concrete image digests; fail closed."""
import argparse
import base64
from datetime import datetime, timezone
import json
from pathlib import Path
import re
import subprocess


def releases(policy, fetch):
    refs = policy.get("supported_releases")
    if not isinstance(refs, list) or not refs or len(refs) != len(set(refs)):
        raise ValueError("Invalid support inventory")
    result = []
    for ref in refs:
        if ref != "latest" and not re.fullmatch(r"v[0-9]+\.[0-9]+\.[0-9]+", ref):
            raise ValueError("Invalid release reference")
        release = fetch("latest" if ref == "latest" else "tags/" + ref)
        tag = release.get("tag_name", "")
        obj = fetch("../git/ref/tags/" + tag).get("object", {})
        if obj.get("type") == "tag":
            obj = fetch("../git/tags/" + obj["sha"]).get("object", {})
        commit = obj.get("sha", "")
        if release.get("draft") or release.get("prerelease") or not re.fullmatch(r"v[0-9]+\.[0-9]+\.[0-9]+", tag) or not re.fullmatch(r"[a-f0-9]{40}", commit):
            raise ValueError("Release not verifiable")
        if tag not in [item["tag"] for item in result]:
            result.append({"tag": tag, "commit": commit})
    return result


def image_targets(tag, platforms, inspect, runtime_images=None):
    images = [(component, "kennfarbe/learnpip:" + component + "-" + tag) for component in ("api", "worker", "web")]
    # The deployed Compose stack also distributes these runtime components.
    images.extend(runtime_images or [("db", "postgres:18-alpine"), ("proxy", "caddy:2-alpine")])
    targets = []
    for component, image in images:
        document = inspect(image)
        manifests = document.get("manifests")
        if not isinstance(manifests, list):
            raise ValueError("Expected multi-platform image index")
        for platform in platforms:
            os_name, arch = platform.split("/")
            matching = [item for item in manifests if item.get("platform", {}).get("os") == os_name and item.get("platform", {}).get("architecture") == arch]
            if len(matching) != 1 or not re.fullmatch(r"sha256:[a-f0-9]{64}", matching[0].get("digest", "")):
                raise ValueError("Supported image platform missing or ambiguous")
            repository = image.rsplit(":", 1)[0]
            targets.append({"tag": tag, "component": component, "platform": platform, "ref": repository + "@" + matching[0]["digest"], "label": tag + "-" + component + "-" + arch})
    return targets


def build_images(dockerfiles):
    images = []
    for component, text in dockerfiles:
        match = re.search(r'^FROM (?:--platform=\$BUILDPLATFORM )?([a-zA-Z0-9./_-]+:[a-zA-Z0-9._-]+) AS build$', text, re.MULTILINE)
        if not match:
            raise ValueError("Build image inventory incomplete")
        images.append((component, match[1]))
    return images


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--policy", type=Path, default=Path("security/supported-releases.json"))
    parser.add_argument("--images", action="store_true")
    parser.add_argument("--resolved-sources", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    policy = json.loads(args.policy.read_text())
    def fetch(endpoint):
        prefix = "repos/kennfarbe/LearnPip/releases/"
        if endpoint.startswith("../"):
            prefix = "repos/kennfarbe/LearnPip/"
            endpoint = endpoint[3:]
        return json.loads(subprocess.check_output(["gh", "api", prefix + endpoint], stderr=subprocess.DEVNULL))
    if args.resolved_sources:
        inventory = json.loads(args.resolved_sources.read_text())
        resolved = [{"tag": item["label"], "commit": item["ref"]} for item in inventory["include"] if item["label"] != "main"]
        if not resolved or any(not re.fullmatch(r"v[0-9]+\.[0-9]+\.[0-9]+", item["tag"]) or not re.fullmatch(r"[a-f0-9]{40}", item["commit"]) for item in resolved):
            raise ValueError("Invalid resolved source inventory")
    else:
        resolved = releases(policy, fetch)
    targets = [{"ref": "main", "label": "main"}] + [{"ref": item["commit"], "label": item["tag"]} for item in resolved]
    if args.images:
        def inspect(image):
            return json.loads(subprocess.check_output(["docker", "buildx", "imagetools", "inspect", "--raw", image], stderr=subprocess.DEVNULL))
        def runtime_images(tag):
            response = json.loads(subprocess.check_output(["gh", "api", "repos/kennfarbe/LearnPip/contents/deploy/compose.prod.yaml?ref=" + tag], stderr=subprocess.DEVNULL))
            compose = base64.b64decode(response["content"]).decode()
            found = []
            service = None
            for line in compose.splitlines():
                match = re.fullmatch(r"  ([a-z-]+):", line)
                if match:
                    service = match[1]
                if service in ("db", "proxy") and line.startswith("    image: "):
                    image = line.split("image: ", 1)[1].strip().replace("${LEARNPIP_VERSION:?Set LEARNPIP_VERSION in deploy/.env.production}", tag)
                    if not re.fullmatch(r"[a-zA-Z0-9./_-]+:[a-zA-Z0-9._-]+", image):
                        raise ValueError("Runtime image cannot be resolved safely")
                    found.append((service, image))
            if {item[0] for item in found} != {"db", "proxy"}:
                raise ValueError("Published runtime inventory incomplete")
            dockerfiles = []
            for component, path in (("build-sdk", "backend/Dockerfile"), ("build-node", "frontend/web/Dockerfile")):
                response = json.loads(subprocess.check_output(["gh", "api", "repos/kennfarbe/LearnPip/contents/" + path + "?ref=" + tag], stderr=subprocess.DEVNULL))
                dockerfiles.append((component, base64.b64decode(response["content"]).decode()))
            if any(component == "db" and image.startswith("kennfarbe/learnpip:db-") for component, image in found):
                response = json.loads(subprocess.check_output(["gh", "api", "repos/kennfarbe/LearnPip/contents/deploy/postgres.Dockerfile?ref=" + tag], stderr=subprocess.DEVNULL))
                dockerfiles.append(("build-go", base64.b64decode(response["content"]).decode()))
            return found + build_images(dockerfiles)
        targets = [target for item in resolved for target in image_targets(item["tag"], policy["platforms"], inspect, runtime_images(item["tag"]))]
    args.output.write_text(json.dumps({"checked_at": datetime.now(timezone.utc).isoformat(), "include": targets}, sort_keys=True))
    # Only non-sensitive inventory metadata enters workflow outputs.
    print("matrix=" + json.dumps({"include": targets}, separators=(",", ":")))


if __name__ == "__main__":
    main()
