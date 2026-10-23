#!/usr/bin/env python3
"""Fetches the screenshots and the build log of a CI run back into the workspace.

Why not the run artifacts: `gh run download` (or any GET of the artifact zip) is answered with a
signed link to productionresultssa*.blob.core.windows.net, and that host is outside the sandbox
allowlist — the TLS handshake is reset, so the bytes never arrive. Annotations only carry ~300 KB
of notice text per job, and the gallery alone makes 1.7 MB. So CI force-pushes the reports to the
single `ci/reports` branch, one commit per run, and this script copies them out through
api.github.com and verifies every file against SHA256SUMS.

    python3 tools/read_screenshots.py                     # newest reports in ci/reports
    python3 tools/read_screenshots.py --list
    python3 tools/read_screenshots.py --run 40036005969   # only if that run is the one on the branch
    python3 tools/read_screenshots.py --out /tmp/shots

Runs from before the branch existed kept the images as base64 annotations; --annotations reads
those from the check run instead.

Requires `gh` (authenticated) and network access to api.github.com.
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
import re
import subprocess
import sys

BRANCH = "ci/reports"
PART_RE = re.compile(r"^PNG (.+) (\d+)/(\d+)$")
REPO = os.environ.get("GITHUB_REPOSITORY") or None


def gh(*args: str) -> str:
    result = subprocess.run(["gh", *args], capture_output=True, text=True)
    if result.returncode != 0:
        sys.exit(f"gh {' '.join(args)} failed:\n{result.stderr.strip()}")
    return result.stdout


def gh_json(*args: str):
    return json.loads(gh(*args))


def repo_name() -> str:
    global REPO
    if REPO is None:
        REPO = gh("repo", "view", "--json", "nameWithOwner", "--jq", ".nameWithOwner").strip()
    return REPO


# ---------------------------------------------------------------- androids: annotations (legacy)


def run_info(run_id: int) -> dict:
    return gh_json("run", "view", str(run_id), "--json", "databaseId,headSha,status,conclusion,displayTitle,headBranch")


def job_ids(run_id: int) -> list[int]:
    data = gh_json("api", f"repos/{repo_name()}/actions/runs/{run_id}/jobs?per_page=100")
    return [job["id"] for job in data["jobs"]]


def annotations(job_id: int) -> list[dict]:
    out: list[dict] = []
    page = 1
    while True:
        data = gh_json("api", f"repos/{repo_name()}/check-runs/{job_id}/annotations?per_page=100&page={page}")
        out.extend(data)
        if len(data) < 100:
            return out
        page += 1


def read_annotations(run_id: int, out: str) -> int:
    items: list[dict] = []
    for job in job_ids(run_id):
        items.extend(annotations(job))

    parts: dict[str, dict[int, str]] = {}
    manifest: dict[str, dict] = {}
    for item in items:
        title = item.get("title") or ""
        match = PART_RE.match(title)
        if match:
            parts.setdefault(match.group(1), {})[int(match.group(2))] = item.get("message") or ""
        elif title == "PNG manifest":
            for line in (item.get("message") or "").splitlines():
                found = re.match(r"^(\S+)\.png +(\d+) bytes +(\d+) chunk\(s\) +sha256 ([0-9a-f]{64})$", line)
                if found:
                    manifest[found.group(1)] = {"bytes": int(found.group(2)), "sha256": found.group(4)}

    if not parts:
        print("no PNG annotations in this run")
        return 1

    os.makedirs(out, exist_ok=True)
    failures = 0
    for name, have in sorted(parts.items()):
        total = max(have)
        if len(have) != total:
            print(f"  {name}: {len(have)}/{total} chunk(s), incomplete")
            failures += 1
            continue
        try:
            raw = base64.b64decode("".join(have[i] for i in range(1, total + 1)), validate=True)
        except ValueError as error:
            print(f"  {name}: base64 error {error}")
            failures += 1
            continue
        expected = manifest.get(name)
        problem = ""
        if expected and len(raw) != expected["bytes"]:
            problem = f'size {len(raw)} != {expected["bytes"]}'
        elif expected and hashlib.sha256(raw).hexdigest() != expected["sha256"]:
            problem = "sha256 mismatch"
        with open(os.path.join(out, name + ".png"), "wb") as handle:
            handle.write(raw)
        print(f"  wrote {name}.png  {len(raw)} bytes  {problem or 'ok'}")
        failures += 1 if problem else 0
    return 1 if failures else 0


# ------------------------------------------------------------------- branch (current channel)


def branch_state() -> tuple[str, str, str]:
    """Returns (commit sha, commit message, tree sha) of the screenshots branch."""
    data = gh_json("api", f"repos/{repo_name()}/branches/{BRANCH}")
    return data["commit"]["sha"], data["commit"]["commit"]["message"], data["commit"]["commit"]["tree"]["sha"]


def branch_files(tree_sha: str) -> list[dict]:
    data = gh_json("api", f"repos/{repo_name()}/git/trees/{tree_sha}?recursive=1")
    return [entry for entry in data["tree"] if entry["type"] == "blob"]


def blob(path: str) -> bytes:
    """Reads one file of the branch. The contents API returns base64 for files up to 1 MB."""
    data = gh_json("api", f"repos/{repo_name()}/contents/{path}?ref={BRANCH}")
    return base64.b64decode(data["content"])


def read_branch(run_id: int | None, out: str, list_only: bool) -> int:
    sha, message, tree = branch_state()
    match = re.search(r"run (\d+)", message)
    published = int(match.group(1)) if match else 0
    print(f"branch {BRANCH} @ {sha[:8]}: {message.strip()}")
    if run_id is not None and published != run_id:
        print(f"the branch holds the screenshots of run {published}, not {run_id}; "
              f"re-run the workflow or pass --annotations for an old run")
        return 1

    files = sorted((f for f in branch_files(tree) if not f["path"].endswith("/")), key=lambda f: f["path"])
    images = [f for f in files if f["path"].endswith(".png")]
    print(f"run {published}: {len(images)} image(s), {len(files)} file(s)")
    for entry in files:
        print(f"  {entry['path']}  {entry['size'] / 1024.0:0.1f} KB")
    if list_only:
        return 0

    os.makedirs(out, exist_ok=True)
    checksums = ""
    if any(f["path"] == "SHA256SUMS.txt" for f in files):
        checksums = blob("SHA256SUMS.txt").decode("utf-8", "replace")

    failures = 0
    for entry in files:
        # Everything the branch holds except the bookkeeping files themselves.
        if entry["path"] in ("SHA256SUMS.txt", "README.md"):
            continue
        raw = blob(entry["path"])
        problem = ""
        expected = re.search(rf"^([0-9a-f]{{64}}) +{re.escape(entry['path'])}$", checksums, re.MULTILINE)
        if expected and hashlib.sha256(raw).hexdigest() != expected.group(1):
            problem = "sha256 mismatch"
        elif expected is None and entry["path"] != "gallery.txt":
            problem = "missing from SHA256SUMS"
        with open(os.path.join(out, entry["path"]), "wb") as handle:
            handle.write(raw)
        print(f"  wrote {entry['path']}  {len(raw)} bytes  {problem or 'ok'}")
        failures += 1 if problem else 0

    print(f"output: {out}")
    return 1 if failures else 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--run", type=int, help="expected run id (checked against the branch message)")
    parser.add_argument("--out", help="output folder; default ci/reports")
    parser.add_argument("--list", action="store_true", help="only list what is published")
    parser.add_argument("--annotations", action="store_true",
                        help="read the base64 annotations of an old run instead of the branch")
    args = parser.parse_args()

    if args.annotations:
        if args.run is None:
            sys.exit("--annotations needs --run <id>")
        out = args.out or os.path.join("ci", "reports", f"run-{args.run}")
        return read_annotations(args.run, out)

    if args.run is not None:
        print(f"run {args.run}  {run_info(args.run).get('displayTitle')}")
    out = args.out or os.path.join("ci", "reports")
    return read_branch(args.run, out, args.list)


if __name__ == "__main__":
    sys.exit(main())
