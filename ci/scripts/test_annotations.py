#!/usr/bin/env python3
"""
Turns test results into GitHub annotations, so they can be read through the Checks API
(the raw job log is not downloadable from the sandbox).

usage: test_annotations.py <trx-folder> <ui-report-folder>

- Every failed test becomes an ::error annotation with its message and the start of the stack trace.
- Every TRX file gets a ::notice with the outcome counts.
- Every UI report file (*.txt, written by the headless tests) is split into ::notice annotations.
- With --png: every PNG screenshot is written as base64 in numbered ::notice parts (k/N), in its own step.
The script never fails the step: it only reports.
"""
import glob
import os
import sys
import xml.etree.ElementTree as ET


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


def report_png(folder: str, limit: int = 12000) -> None:
    """Writes each PNG as base64 in numbered notices (k/N). Joined in order, the text decodes to the same PNG."""
    import base64

    for path in sorted(glob.glob(os.path.join(folder, "*.png"))):
        with open(path, "rb") as handle:
            encoded = base64.b64encode(handle.read()).decode("ascii")
        name = os.path.splitext(os.path.basename(path))[0]
        parts = [encoded[i:i + limit] for i in range(0, len(encoded), limit)] or [""]
        print(f"::notice title={title(f'PNG {name} size')}::{len(parts)} parts, {len(encoded)} base64 characters")
        for number, part in enumerate(parts, 1):
            print(f"::notice title={title(f'PNG {name} {number}/{len(parts)}')}::{part}")


def main() -> int:
    # --png: screenshots only. GitHub keeps only a few notices per step, so they run in their own step.
    if len(sys.argv) > 2 and sys.argv[1] == "--png":
        report_png(sys.argv[2])
        return 0
    trx_folder = sys.argv[1] if len(sys.argv) > 1 else "test-results"
    ui_folder = sys.argv[2] if len(sys.argv) > 2 else "ui-report"
    if os.path.isdir(trx_folder):
        report_trx(trx_folder)
    else:
        print(f"::notice title=No test results::{esc(trx_folder)} does not exist")
    if os.path.isdir(ui_folder):
        report_ui(ui_folder)
        report_png(ui_folder)
    return 0


if __name__ == "__main__":
    sys.exit(main())
