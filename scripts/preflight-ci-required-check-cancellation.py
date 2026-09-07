#!/usr/bin/env python3
"""Fail closed if Shared CI can attach cancelled duplicate required contexts to a PR SHA."""

from __future__ import annotations

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "ci.yml"


def fail(message: str) -> None:
    print(f"ERROR: Shared CI required-check cancellation preflight failed closed: {message}")
    raise SystemExit(1)


def require(pattern: str, text: str, message: str) -> None:
    if re.search(pattern, text, re.MULTILINE) is None:
        fail(message)


def main() -> int:
    try:
        text = WORKFLOW.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        fail(f"could not read {WORKFLOW.relative_to(ROOT)} safely: {exc}")

    require(r'^\s*"push":\s*$', text, "Shared CI must retain task/integration branch push validation")
    require(r'^\s*"pull_request":\s*$', text, "Shared CI must retain pull-request validation")

    concurrency = re.search(r"(?ms)^concurrency:\s*\n(?P<body>.*?)(?=^jobs:\s*$)", text)
    if concurrency is None:
        fail("Shared CI concurrency block is missing")
    body = concurrency.group("body")
    if "cancel-in-progress: true" not in body:
        fail("same-event superseded validation must remain cancellable")
    for token in ("github.event_name", "'push'", "'pull_request'", "'metadata'", "'dispatch'"):
        if token not in body:
            fail(f"concurrency identity is missing required event-class token {token}")

    # Only pull_request code validation may own the protected ruleset's stable required names.
    # Push, PR metadata edits and workflow_dispatch can be cancelled or rerun for the same SHA;
    # their names must therefore be non-required so they cannot satisfy or poison PR admission.
    preflight_name = (
        "name: ${{ github.event_name == 'push' && 'branch-preflight' || "
        "github.event_name == 'pull_request' && github.event.action == 'edited' && 'metadata-preflight' || "
        "github.event_name == 'pull_request' && 'preflight' || 'dispatch-preflight' }}"
    )
    core_name = (
        "name: ${{ github.event_name == 'push' && 'branch-core' || "
        "github.event_name == 'pull_request' && github.event.action == 'edited' && 'metadata-core' || "
        "github.event_name == 'pull_request' && 'core' || 'dispatch-core' }}"
    )
    if text.count(preflight_name) != 1:
        fail("preflight required context must be exclusive to pull_request code validation")
    if text.count(core_name) != 1:
        fail("core required context must be exclusive to pull_request code validation")

    print("PASS: only PR code validation owns required preflight/core contexts; other cancellable event classes are isolated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
