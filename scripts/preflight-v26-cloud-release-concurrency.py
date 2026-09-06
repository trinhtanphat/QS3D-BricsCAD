#!/usr/bin/env python3
"""Fail closed if the V26 cloud release workflow can preempt an in-flight release transaction."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"


def validate(text: str) -> list[str]:
    errors: list[str] = []
    if "group: qs3d-cloud-v26-preview-release" not in text:
        errors.append("V26 cloud release must retain one stable workflow concurrency group")

    match = re.search(
        r"(?ms)^concurrency:\s*\n(?:^[ \t]+.*\n)*?^[ \t]+cancel-in-progress:\s*(true|false)\s*$",
        text,
    )
    if match is None:
        errors.append("V26 cloud release must declare explicit workflow-level cancel-in-progress policy")
    elif match.group(1) != "false":
        errors.append("V26 cloud release transaction must serialize without cancelling an in-flight run")

    if not re.search(r"(?m)^  release:\s*$", text):
        errors.append("V26 cloud release workflow must retain the release job guarded by this policy")
    return errors


def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    errors = validate(text)

    # Prove the guard catches the unsafe regression instead of merely matching today's file.
    safe = re.sub(
        r"(?m)^(  cancel-in-progress:)\s*(?:true|false)\s*$",
        r"\1 false",
        text,
        count=1,
    )
    mutated = safe.replace("  cancel-in-progress: false", "  cancel-in-progress: true", 1)
    if not validate(safe):
        if not validate(mutated):
            errors.append("mutation probe failed: preemptible release concurrency was not rejected")
    else:
        # Production is intentionally RED until the workflow is fixed; still validate the mutation harness
        # against a synthesized safe baseline.
        if not validate(mutated):
            errors.append("mutation probe failed: synthesized preemptible concurrency was not rejected")

    if errors:
        for error in errors:
            print(f"ERROR: {error}", file=sys.stderr)
        return 1

    print("PASS: V26 cloud release concurrency is serialized and non-preemptible.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
