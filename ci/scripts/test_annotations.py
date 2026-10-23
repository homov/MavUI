#!/usr/bin/env python3
"""
Turns test results into GitHub annotations, so they can be read through the Checks API
(the raw job log and the uploaded artifacts are not reachable from the agent sandbox:
artifact downloads go to blob.core.windows.net, which is blocked).

usage:
    test_annotations.py <trx-folder> <ui-report-folder>
    test_annotations.py --png-manifest <ui-report-folder>
    test_annotations.py --png-batch N  <ui-report-folder>

- Every failed test becomes an ::error annotation with its message and the start of the stack trace.
- Every TRX file gets a ::notice with the outcome counts.
- Every UI report file (*.txt, written by the headless tests) is split into ::notice annotations.
- Screenshots (*.png) are shipped as base64 notices, one batch per workflow step. Two hard GitHub
  limits shape this: an annotation message is cut at 4096 characters, and a step keeps at most
  10 notices. So each step emits 10 chunks of 4000 base64 characters and `--png-manifest` lists
  every file with its byte size, chunk count and sha256. `tools/read_screenshots.py` joins the
  chunks back into the exact PNGs; the build.yml steps must match PNG_BATCHES below.
The script never fails the step: it only reports.
"""
import base64
import glob
import hashlib
import os
import sys
import xml.etree.ElementTree as ET

# GitHub truncates one annotation message at 4096 characters: keep a margin.
PNG_CHUNK = 4000
# GitHub keeps at most 10 notices per step (measured: the 11th was dropped).
PNG_PER_STEP = 10
# Number of `--png-batch` steps in .github/workflows/build.yml.
PNG_BATCHES = 3


def esc(text: str) -> str:
    """Escapes a message for a workflow command."""
    return text.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def title(text: str) -> str:
    """Titles cannot contain ':' or ',' inside a workflow command."""
    return esc(text.replace(":", " ").replace(",", " "))


def report_trx(folder: str) -> None:
    for path in sorted(glob.glob(os.path.join(folder, "**", "*.trx"), recursive=True)):
        counts: dict[str, int] = {}
        try:
            root = ET.parse(path).getroot()
        except ET.ParseError as error:
            print(f"::warning title=Unreadable TRX::{esc(os.path.basename(path))}: {esc(str(error))}")
            continue
        for result in root.iter():
            if not result.tag.endswith("UnitTestResult"):
                continue
            outcome = result.get("outcome", "unknown")
            counts[outcome] = counts.get(outcome, 0) + 1
            if outcome != "Failed":
                continue
            message = stack = ""
            for part in result.iter():
                if part.tag.endswith("Message"):
                    message = (part.text or "").strip()
                elif part.tag.endswith("StackTrace"):
                    stack = (part.text or "").strip()
            body = message[:1500]
            if stack:
                body += "\n" + stack[:800]
            name = result.get("testName", "unknown test")
            print(f"::error title=Failed: {title(name)}::{esc(body)}")
        summary = ", ".join(f"{key}: {value}" for key, value in sorted(counts.items())) or "no tests"
        print(f"::notice title={title(os.path.basename(path))}::{esc(summary)}")


def report_ui(folder: str, limit: int = 3000) -> None:
    for path in sorted(glob.glob(os.path.join(folder, "*.txt"))):
        with open(path, encoding="utf-8") as handle:
            lines = handle.read().splitlines()
        name = os.path.splitext(os.path.basename(path))[0]
        part, chunk, size = 1, [], 0
        for line in lines:
            if chunk and size + len(line) > limit:
                print(f"::notice title={title(f'UI {name} ({part})')}::{esc(chr(10).join(chunk))}")
                part, chunk, size = part + 1, [], 0
            chunk.append(line)
            size += len(line) + 1
        if chunk:
            print(f"::notice title={title(f'UI {name} ({part})')}::{esc(chr(10).join(chunk))}")


def png_plan(folder: str) -> tuple[list[dict], list[tuple[str, int, int, str]]]:
    """Describes every PNG and builds the global list of base64 chunks.

    Files are packed greedily into the budget of the workflow steps; a file that does not fit
    completely is skipped (a half image is useless) and reported in the manifest as skipped.
    """
    budget = PNG_BATCHES * PNG_PER_STEP
    entries: list[dict] = []
    chunks: list[tuple[str, int, int, str]] = []
    for path in sorted(glob.glob(os.path.join(folder, "*.png"))):
        name = os.path.splitext(os.path.basename(path))[0]
        with open(path, "rb") as handle:
            raw = handle.read()
        encoded = base64.b64encode(raw).decode("ascii")
        parts = [encoded[i:i + PNG_CHUNK] for i in range(0, len(encoded), PNG_CHUNK)] or [""]
        entry = {
            "name": name,
            "bytes": len(raw),
            "sha256": hashlib.sha256(raw).hexdigest(),
            "chunks": len(parts),
            "skipped": len(chunks) + len(parts) > budget,
        }
        entries.append(entry)
        if entry["skipped"]:
            continue
        for number, part in enumerate(parts, 1):
            chunks.append((name, number, len(parts), part))
    return entries, chunks


def report_png_manifest(folder: str) -> None:
    entries, chunks = png_plan(folder)
    if not entries:
        print(f"::notice title=No screenshots::{esc(folder)} has no *.png")
        return
    lines = [
        f"{len(entries)} file(s), {len(chunks)} of {PNG_BATCHES * PNG_PER_STEP} base64 chunks used; "
        f"read them with tools/read_screenshots.py",
    ]
    for entry in entries:
        state = "SKIPPED over budget" if entry["skipped"] else f'{entry["chunks"]} chunk(s)'
        lines.append(f'{entry["name"]}.png  {entry["bytes"]} bytes  {state}  sha256 {entry["sha256"]}')
    print(f"::notice title=PNG manifest::{esc(chr(10).join(lines))}")
    skipped = sum(1 for entry in entries if entry["skipped"])
    if skipped:
        hint = (f"{skipped} screenshot(s) did not fit into {PNG_BATCHES} batches; raise PNG_BATCHES in "
                "test_annotations.py and add the same number of --png-batch steps to build.yml, "
                "or capture fewer/smaller frames")
        print(f"::warning title=PNG budget exceeded::{esc(hint)}")


def report_png_batch(folder: str, batch: int) -> None:
    if not 0 <= batch < PNG_BATCHES:
        print(f"::warning title=Bad PNG batch::{esc(f'{batch} is outside 0..{PNG_BATCHES - 1}')}")
        return
    _, chunks = png_plan(folder)
    start = batch * PNG_PER_STEP
    selected = chunks[start:start + PNG_PER_STEP]
    for name, number, total, part in selected:
        print(f"::notice title={title(f'PNG {name} {number}/{total}')}::{part}")
    print(f"chunks {start}-{start + len(selected) - 1} of {len(chunks)} "
          f"({len(selected)} notice(s), {len(''.join(c[3] for c in selected))} characters)")


def main() -> int:
    if len(sys.argv) > 2 and sys.argv[1] == "--png-manifest":
        report_png_manifest(sys.argv[2])
        return 0
    if len(sys.argv) > 3 and sys.argv[1] == "--png-batch":
        try:
            batch = int(sys.argv[2])
        except ValueError:
            print(f"::warning title=Bad PNG batch::{esc(sys.argv[2])}")
            return 0
        report_png_batch(sys.argv[3], batch)
        return 0
    trx_folder = sys.argv[1] if len(sys.argv) > 1 else "test-results"
    ui_folder = sys.argv[2] if len(sys.argv) > 2 else "ui-report"
    if os.path.isdir(trx_folder):
        report_trx(trx_folder)
    else:
        print(f"::notice title=No test results::{esc(trx_folder)} does not exist")
    if os.path.isdir(ui_folder):
        report_ui(ui_folder)
    return 0


if __name__ == "__main__":
    sys.exit(main())
