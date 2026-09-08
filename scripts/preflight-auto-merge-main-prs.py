#!/usr/bin/env python3
"""Fail closed if repository-wide main PR auto-ready/auto-merge policy regresses."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "auto-merge-main-prs.yml"


def require(text: str, token: str) -> None:
    if token not in text:
        raise SystemExit(f"auto-merge main PR preflight: missing required token: {token}")


def forbid(text: str, token: str) -> None:
    if token in text:
        raise SystemExit(f"auto-merge main PR preflight: forbidden token present: {token}")


if not WORKFLOW.is_file():
    raise SystemExit(f"auto-merge main PR preflight: workflow missing: {WORKFLOW.relative_to(ROOT)}")

text = WORKFLOW.read_text(encoding="utf-8")

for token in (
    "pull_request_target:",
    "opened",
    "reopened",
    "synchronize",
    "ready_for_review",
    "converted_to_draft",
    "contents: read",
    "pull-requests: write",
    "github.event.pull_request.base.ref == 'main'",
    "GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}",
    "gh pr view",
    "isDraft",
    "autoMergeRequest",
    "gh pr ready",
    "gh pr merge --auto --merge",
):
    require(text, token)

for token in (
    "actions/checkout",
    "--admin",
    "github.event.pull_request.head.sha",
    "github.event.pull_request.head.ref",
):
    forbid(text, token)

print("PASS auto-merge main PR policy source guard")
