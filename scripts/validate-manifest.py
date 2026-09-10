#!/usr/bin/env python3
"""Validate the Jellyfin plugin repository manifest.json.

Structural checks always run. When --new-version (and optionally --zip)
is given, additionally asserts the newest entry matches the release
being cut and its checksum equals the md5 of the built zip.
"""

import argparse
import hashlib
import json
import re
import sys

EXPECTED_GUID = "e29b0e3d-f15a-47e6-9f05-d8f4e6260a4e"
REQUIRED_TOP_LEVEL = ["guid", "name", "overview", "description", "owner", "category", "versions"]
REQUIRED_VERSION_FIELDS = ["version", "targetAbi", "sourceUrl", "checksum", "timestamp", "changelog"]
CHECKSUM_RE = re.compile(r"^[a-f0-9]{32}$")
VERSION_RE = re.compile(r"^\d+\.\d+\.\d+\.\d+$")

errors = []


def fail(msg):
    errors.append(msg)


def version_tuple(v):
    return tuple(int(p) for p in v.split("."))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", default="manifest.json")
    parser.add_argument("--new-version", help="version tag being released; must be the newest entry")
    parser.add_argument("--zip", help="path to the built zip; its md5 must match the newest entry's checksum")
    args = parser.parse_args()

    try:
        with open(args.manifest, "r", encoding="utf-8") as f:
            data = json.load(f)
    except FileNotFoundError:
        print(f"ERROR: {args.manifest} not found", file=sys.stderr)
        sys.exit(1)
    except json.JSONDecodeError as e:
        print(f"ERROR: {args.manifest} is not valid JSON: {e}", file=sys.stderr)
        sys.exit(1)

    if not isinstance(data, list) or len(data) != 1:
        print("ERROR: manifest must be a JSON array containing exactly one plugin object", file=sys.stderr)
        sys.exit(1)

    plugin = data[0]

    for field in REQUIRED_TOP_LEVEL:
        if field not in plugin:
            fail(f"missing required top-level field: {field}")

    if plugin.get("guid") != EXPECTED_GUID:
        fail(f"guid mismatch: expected {EXPECTED_GUID}, got {plugin.get('guid')}")

    versions = plugin.get("versions", [])
    if not isinstance(versions, list) or not versions:
        fail("versions must be a non-empty array")
        versions = []

    for i, entry in enumerate(versions):
        for field in REQUIRED_VERSION_FIELDS:
            if not entry.get(field):
                fail(f"versions[{i}] missing/empty field: {field}")
        v = entry.get("version", "")
        if not VERSION_RE.match(v):
            fail(f"versions[{i}] version {v!r} is not a numeric 4-tuple")
        checksum = entry.get("checksum", "")
        if not CHECKSUM_RE.match(checksum):
            fail(f"versions[{i}] checksum {checksum!r} does not match ^[a-f0-9]{{32}}$")

    parseable = [e["version"] for e in versions if VERSION_RE.match(e.get("version", ""))]
    tuples = [version_tuple(v) for v in parseable]
    if len(set(tuples)) != len(tuples):
        fail("duplicate version entries found")
    if tuples != sorted(tuples, reverse=True):
        fail("versions are not strictly descending")

    if args.new_version:
        if not versions:
            fail("--new-version given but manifest has no version entries")
        else:
            newest = versions[0]
            if newest.get("version") != args.new_version:
                fail(f"newest entry is {newest.get('version')!r}, expected released tag {args.new_version!r}")
            if args.zip:
                with open(args.zip, "rb") as f:
                    md5 = hashlib.md5(f.read()).hexdigest()
                if newest.get("checksum") != md5:
                    fail(f"newest entry checksum {newest.get('checksum')!r} != md5 of {args.zip} ({md5})")

    if errors:
        for e in errors:
            print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(1)

    print(f"manifest.json OK: {len(versions)} version entries, newest {versions[0]['version'] if versions else 'n/a'}")


if __name__ == "__main__":
    main()
