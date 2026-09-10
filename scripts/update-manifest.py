#!/usr/bin/env python3
"""Prepend a new version entry to manifest.json for a release.

Rules:
- Reads manifest.json from the git checkout (git is the source of truth).
- Missing file: start from an empty skeleton, but print a loud warning.
- Invalid JSON: fail hard (never silently discard history).
- targetAbi is read from build.yaml (single source of truth).
- Existing entries are never rewritten; dedupe by version (new wins);
  entries sorted descending by numeric 4-tuple.

Usage: update-manifest.py --version 0.0.0.16 --zip path/to/plugin.zip --repo owner/name
"""

import argparse
import datetime
import hashlib
import json
import re
import sys

MANIFEST_PATH = "manifest.json"
BUILD_YAML_PATH = "build.yaml"

SKELETON = {
    "guid": "e29b0e3d-f15a-47e6-9f05-d8f4e6260a4e",
    "name": "Top Ten",
    "overview": "Creates and maintains a collection of the top 10 most watched movies and TV shows",
    "description": "Creates a scheduled task that runs every 24 hours to identify the top 10 movies and series watched on the Jellyfin server.",
    "owner": "johnpc",
    "category": "General",
    "versions": [],
}


def read_target_abi():
    with open(BUILD_YAML_PATH, "r", encoding="utf-8") as f:
        for line in f:
            m = re.match(r'^\s*targetAbi:\s*"?([0-9.]+)"?\s*$', line)
            if m:
                return m.group(1)
    print(f"ERROR: targetAbi not found in {BUILD_YAML_PATH}", file=sys.stderr)
    sys.exit(1)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True)
    parser.add_argument("--zip", required=True)
    parser.add_argument("--repo", required=True, help="owner/name, e.g. johnpc/jellyfin-plugin-top-ten")
    args = parser.parse_args()

    try:
        with open(MANIFEST_PATH, "r", encoding="utf-8") as f:
            data = json.load(f)
        plugin = data[0]
    except FileNotFoundError:
        print("=" * 72, file=sys.stderr)
        print("WARNING: manifest.json not found in checkout! Starting from an", file=sys.stderr)
        print("EMPTY manifest — all release history will be missing. If this is", file=sys.stderr)
        print("not a brand-new repository, STOP and restore manifest.json.", file=sys.stderr)
        print("=" * 72, file=sys.stderr)
        plugin = dict(SKELETON)
        data = [plugin]
    except (json.JSONDecodeError, IndexError, TypeError) as e:
        print(f"ERROR: manifest.json exists but is invalid ({e}). Refusing to overwrite history; fix it manually.", file=sys.stderr)
        sys.exit(1)

    with open(args.zip, "rb") as f:
        md5 = hashlib.md5(f.read()).hexdigest()

    zip_name = args.zip.rsplit("/", 1)[-1]
    new_entry = {
        "version": args.version,
        "changelog": f"https://github.com/{args.repo}/releases/tag/{args.version}",
        "targetAbi": read_target_abi(),
        "sourceUrl": f"https://github.com/{args.repo}/releases/download/{args.version}/{zip_name}",
        "checksum": md5,
        "timestamp": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
    }

    # Dedupe by version (new wins); existing entries pass through untouched.
    versions = [new_entry] + [v for v in plugin.get("versions", []) if v.get("version") != args.version]
    versions.sort(key=lambda v: tuple(int(p) for p in v["version"].split(".")), reverse=True)
    plugin["versions"] = versions

    with open(MANIFEST_PATH, "w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
        f.write("\n")

    print(f"manifest.json updated: added {args.version} (targetAbi {new_entry['targetAbi']}, md5 {md5}); {len(versions)} entries total")


if __name__ == "__main__":
    main()
