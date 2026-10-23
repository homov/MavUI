#!/usr/bin/env python3
"""Fetches the screenshots of a CI run back into the workspace.

The self-hosted runner publishes every PNG twice: as a normal artifact (not reachable from an
agent sandbox, the download redirects to blob.core.windows.net) and as base64 annotations on the
job's check run (reachable through api.github.com). This script reads the annotations, joins the
chunks, checks the size and the sha256 from the `PNG manifest` notice and writes real files.

    python3 tools/read_screenshots.py                     # latest run of the current branch
    python3 tools/read_screenshots.py --branch main
    python3 tools/read_screenshots.py --run 40031288662
    python3 tools/read_screenshots.py --run 40031288662 --out /tmp/shots --list

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
        REPO = gh_json("repo", "view", "--json", "nameWithOwner", "--jq", ".nameWithOwner")
    return REPO


def latest_run(branch: str) -> dict:
    runs = gh_json("run", "list", "--branch", branch, "--limit", "1", "--json", "databaseId,headSha,status,conclusion,displayTitle")
    if not runs:
        sys.exit(f"no runs on branch {branch}")
    return runs[0]


def run_info(run_id: int) -> dict:
    return gh_json("run", "view", str(run_id), "--json", "databaseId,headSha,status,conclusion,displayTitle,headBranch")


def job_ids(run_id: int) -> list[tuple[int, str, str]]:
    data = gh_json("api", f"repos/{repo_name()}/actions/runs/{run_id}/jobs?per_page=100")
    return [(job["id"], job["name"], job.get("conclusion") or job.get("status") or "?") for job in data["jobs"]]


def annotations(job_id: int) -> list[dict]:
    out: list[dict] = []
    page = 1
    while True:
        data = gh_json("api", f"repos/{repo_name()}/check-runs/{job_id}/annotations?per_page=100&page={page}")
        if not data:
            return out
        out.extend(data)
        if len(data) < 100:
            return out
        page += 1


def manifest_of(items: list[dict]) -> dict[str, dict]:
    files: dict[str, dict] = {}
    for item in items:
        if (item.get("title") or "") == "PNG manifest":
            for line in (item.get("message") or "").splitlines():
                match = re.match(r"^(\S+)\.png +(\d+) bytes +(\d+) chunk\(s\) +sha256 ([0-9a-f]{64})$", line)
                if match:
                    files[match.group(1)] = {
                        "bytes": int(match.group(2)),
                        "chunks": int(match.group(3)),
                        "sha256": match.group(4),
                    }
    return files


def collect(items: list[dict]) -> dict[str, dict[int, tuple[int, str]]]:
    parts: dict[str, dict[int, tuple[int, str]]] = {}
    for item in items:
        match = PART_RE.match(item.get("title") or "")
        if match:
            parts.setdefault(match.group(1), {})[int(match.group(2))] = (int(match.group(3)), item.get("message") or "")
    return parts


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--run", type=int, help="run id; default: the latest run of --branch")
    parser.add_argument("--branch", help="branch name; default: the current git branch")
    parser.add_argument("--out", help="output folder; default: ci/screenshots/run-<id>")
    parser.add_argument("--list", action="store_true", help="only list what the run published")
    args = parser.parse_args()

    if args.run:
        info = run_info(args.run)
    else:
        branch = args.branch or gh("rev-parse", "--abbrev-ref", "HEAD").strip()
        info = latest_run(branch)
    run_id = info["databaseId"]
    print(f"run {run_id}  {info.get('headBranch')}  {str(info.get('headSha'))[:8]}  "
          f"{info.get('status')}/{info.get('conclusion')}  {info.get('displayTitle')}")

    items: list[dict] = []
    for job_id, name, conclusion in job_ids(run_id):
        job_items = annotations(job_id)
        print(f"  job {job_id} {name} ({conclusion}): {len(job_items)} annotation(s)")
        items.extend(job_items)

    parts = collect(items)
    manifest = manifest_of(items)
    if not parts:
        print("no PNG annotations in this run")
        return 1
    for name in sorted(parts):
        have = parts[name]
        total = next(iter(have.values()))[0]
        missing = sorted(set(range(1, total + 1)) - set(have))
        state = "complete" if not missing else f"missing {missing}"
        print(f"  {name}.png: {len(have)}/{total} chunk(s) {state}")
    if args.list:
        return 0

    out = args.out or os.path.join("ci", "screenshots", f"run-{run_id}")
    os.makedirs(out, exist_ok=True)
    failures = 0
    for name in sorted(parts):
        have = parts[name]
        total = next(iter(have.values()))[0]
        if len(have) != total:
            failures += 1
            continue
        blob = "".join(have[number][1] for number in range(1, total + 1))
        try:
            raw = base64.b64decode(blob, validate=True)
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
        path = os.path.join(out, name + ".png")
        with open(path, "wb") as handle:
            handle.write(raw)
        print(f"  wrote {path}  {len(raw)} bytes  {'BAD: ' + problem if problem else 'ok'}")
        failures += 1 if problem else 0
    print(f"output: {out}")
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
