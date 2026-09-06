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

    # Both event families remain intentional: push provides exact-head branch evidence and PR
    # provides protected-branch required contexts. Their required check identities must not share
    # a cancellation domain, because GitHub records the losing run's jobs as cancelled check-runs
    # on the same candidate SHA and repository rules reject that SHA even if the survivor passes.
    require(r'^\s*"push":\s*$', text, "Shared CI must retain task/integration branch push validation")
    require(r'^\s*"pull_request":\s*$', text, "Shared CI must retain pull-request validation")

    concurrency = re.search(r"(?ms)^concurrency:\s*\n(?P<body>.*?)(?=^jobs:\s*$)", text)
    if concurrency is None:
        fail("Shared CI concurrency block is missing")
    body = concurrency.group("body")
    if "cancel-in-progress: true" not in body:
        fail("same-event superseded validation must remain cancellable")
    if "github.event_name" not in body:
        fail("concurrency identity must include event class so push cannot cancel PR required contexts")
    if "'push'" not in body or "'pull_request'" not in body:
        fail("concurrency identity must distinguish push and pull_request code validation")
    if "'metadata'" not in body:
        fail("pull_request edited metadata must remain isolated from code validation")

    # Required PR contexts stay stable for rulesets; push jobs get distinct display names so a
    # cancelled/successful branch run can neither block nor satisfy the PR-only required contexts.
    require(
        r"(?ms)^\s{2}preflight:\s*\n\s{4}name:\s*\$\{\{\s*github\.event_name\s*==\s*'push'\s*&&\s*'branch-preflight'\s*\|\|\s*'preflight'\s*\}\}",
        text,
        "preflight job must expose branch-preflight for push and stable preflight for PR",
    )
    require(
        r"(?ms)^\s{2}core:\s*\n\s{4}name:\s*\$\{\{\s*github\.event_name\s*==\s*'push'\s*&&\s*'branch-core'\s*\|\|\s*'core'\s*\}\}",
        text,
        "core job must expose branch-core for push and stable core for PR",
    )

    print("PASS: Shared CI separates push cancellation/check identities from PR required contexts.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
