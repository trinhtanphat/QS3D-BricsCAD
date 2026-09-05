#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v26-cloud.yml"


def fail(message: str) -> None:
    print(f"ERROR: V26 cloud concurrency preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


if not WORKFLOW.is_file():
    fail(f"missing required workflow: {WORKFLOW.relative_to(ROOT)}")

workflow = WORKFLOW.read_text(encoding="utf-8")

if "group: qs3d-cloud-v26-preview-release" not in workflow:
    fail("V26 cloud workflow must retain the canonical concurrency group")
if "cancel-in-progress: true" not in workflow:
    fail("a fresh V26 cloud dispatch must supersede a stale in-progress run")
if "cancel-in-progress: false" in workflow:
    fail("V26 cloud workflow must not queue indefinitely behind stale installer acquisition")
if "workflow_dispatch:" not in workflow:
    fail("V26 cloud workflow must remain manually dispatched")

print("V26 cloud concurrency preflight passed.")
