#!/usr/bin/env python3
"""Fail closed if Shared CI can leave stale or cancelled required contexts authoritative."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"

def fail(message: str) -> None:
    print(f"ERROR: Shared CI required-check cancellation preflight failed closed: {message}")
    raise SystemExit(1)

def main() -> int:
    text = WORKFLOW.read_text(encoding="utf-8")
    concurrency = re.search(r"(?ms)^concurrency:\s*\n(?P<body>.*?)(?=^jobs:\s*$)", text)
    if concurrency is None:
        fail("Shared CI concurrency block is missing")
    body = concurrency.group("body")
    for token in ("github.event_name", "'push'", "'pull_request'", "'dispatch'", "cancel-in-progress: true"):
        if token not in body:
            fail(f"concurrency identity is missing required token {token}")
    for token in ("'metadata'", "github.event.action == 'edited'", "github.run_attempt", "github.run_id"):
        if token in body:
            fail(f"concurrency identity retains stale split/rerun token {token}")

    preflight_name = (
        "name: ${{ github.event_name == 'push' && 'branch-preflight' || "
        "github.event_name == 'pull_request' && 'preflight' || 'dispatch-preflight' }}"
    )
    core_name = (
        "name: ${{ github.event_name == 'push' && 'branch-core' || "
        "github.event_name == 'pull_request' && 'core' || 'dispatch-core' }}"
    )
    if text.count(preflight_name) != 1 or text.count(core_name) != 1:
        fail("all PR actions must own exactly one stable preflight/core check-run identity")

    core_if = "if: ${{ !cancelled() && (github.event_name == 'workflow_dispatch' || github.event_name == 'push' || github.event_name == 'pull_request') }}"
    if text.count(core_if) != 1:
        fail("core must materialize after preflight failure but remain cancellation-aware")
    for forbidden in ("statuses: write", "/statuses/", "always() && (github.event_name"):
        if forbidden in text:
            fail(f"mutable or cancellation-insensitive required-context path remains: {forbidden}")

    print("PASS: unified PR cancellation plus stable GitHub-owned preflight/core prevents stale required-context authority.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
