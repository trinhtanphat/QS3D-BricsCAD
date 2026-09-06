#!/usr/bin/env python3
"""Fail closed if V25 cloud release pending dispatches can replace one another."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25-cloud.yml"


def concurrency_block(text: str) -> str | None:
    match = re.search(r"(?ms)^concurrency:\s*\n((?:^[ \t]+.*(?:\n|$))+)", text)
    return None if match is None else match.group(1)


def validate(text: str) -> list[str]:
    errors: list[str] = []
    block = concurrency_block(text)
    if block is None:
        errors.append("V25 cloud release must declare workflow-level concurrency")
    else:
        if not re.search(r"(?m)^  group:\s*qs3d-cloud-v25-preview-release\s*$", block):
            errors.append("V25 cloud release must retain one stable workflow concurrency group")
        cancel = re.search(r"(?m)^  cancel-in-progress:\s*(true|false)\s*$", block)
        if cancel is None:
            errors.append("V25 cloud release must declare explicit workflow-level cancel-in-progress policy")
        elif cancel.group(1) != "false":
            errors.append("V25 cloud release transaction must remain non-preemptible")
        if not re.search(r"(?m)^  queue:\s*max\s*$", block):
            errors.append("V25 cloud release must retain multiple pending dispatches instead of replacing them")

    if not re.search(r"(?m)^  release:\s*$", text):
        errors.append("V25 cloud release workflow must retain the release job guarded by this policy")
    return errors


def safe_baseline(text: str) -> str:
    safe = re.sub(
        r"(?m)^(  cancel-in-progress:)\s*(?:true|false)\s*$",
        r"\1 false",
        text,
        count=1,
    )
    block = concurrency_block(safe)
    if block is not None and not re.search(r"(?m)^  queue:\s*\S+\s*$", block):
        safe = safe.replace("  cancel-in-progress: false\n", "  cancel-in-progress: false\n  queue: max\n", 1)
    else:
        safe = re.sub(r"(?m)^(  queue:)\s*\S+\s*$", r"\1 max", safe, count=1)
    return safe


def require_mutation_rejection(source: str, mutated: str, label: str) -> None:
    if mutated == source:
        raise AssertionError("mutation probe could not modify safe concurrency baseline: " + label)
    if not validate(mutated):
        raise AssertionError("mutation probe was not rejected: " + label)


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    errors = validate(text)

    safe = safe_baseline(text)
    if validate(safe):
        errors.append("mutation harness could not synthesize a safe V25 cloud concurrency baseline")
    else:
        require_mutation_rejection(
            safe,
            safe.replace("  queue: max\n", "", 1),
            "default single pending slot",
        )
        require_mutation_rejection(
            safe,
            safe.replace("  queue: max", "  queue: single", 1),
            "explicit single pending slot",
        )
        require_mutation_rejection(
            safe,
            safe.replace("  cancel-in-progress: false", "  cancel-in-progress: true", 1),
            "active release preemption",
        )

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("PASS: V25 cloud release concurrency retains multiple pending dispatches without replacing active or pending release work.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
