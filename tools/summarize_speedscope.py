#!/usr/bin/env python3
"""Summarize a speedscope JSON (from dotnet-trace convert) into a small text report.

Usage: summarize_speedscope.py <file.speedscope.json> [top_n]
Self time = time where the function is the leaf frame of the sample.
Inclusive time = time where the function appears anywhere on the stack.
"""
import json
import sys
from collections import Counter


def name_of(frame):
    name = frame.get("name", "?")
    file = frame.get("file")
    return f"{name} ({file})" if file else name


def main():
    path = sys.argv[1]
    top_n = int(sys.argv[2]) if len(sys.argv) > 2 else 40
    data = json.load(open(path, encoding="utf-8"))
    frames = data["shared"]["frames"]

    self_w = Counter()
    incl_w = Counter()
    total = 0.0
    for prof in data.get("profiles", []):
        samples = prof.get("samples", [])
        weights = prof.get("weights") or [1] * len(samples)
        for stack, w in zip(samples, weights):
            if not stack:
                continue
            total += w
            leaf = name_of(frames[stack[-1]])
            self_w[leaf] += w
            for idx in set(stack):
                incl_w[name_of(frames[idx])] += w

    def pct(v):
        return 100.0 * v / total if total else 0.0

    out = [f"total samples weight: {total:.0f}", ""]
    out.append(f"== Top {top_n} by SELF time")
    for name, w in self_w.most_common(top_n):
        out.append(f"{pct(w):6.2f}%  {name}")
    out.append("")
    out.append(f"== Top {top_n} by INCLUSIVE time (Mav/Avalonia frames only)")
    shown = 0
    for name, w in incl_w.most_common():
        if "Mav" in name or "Avalonia" in name:
            out.append(f"{pct(w):6.2f}%  {name}")
            shown += 1
            if shown >= top_n:
                break
    print("\n".join(out))


if __name__ == "__main__":
    main()
