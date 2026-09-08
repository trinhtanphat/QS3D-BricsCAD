#!/usr/bin/env python3
"""Fail closed if repository-wide main PR auto-ready/auto-merge policy regresses."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "auto-merge-main-prs.yml"
CI_POLICY_GUARD = ROOT / "scripts" / "preflight-ci-manual-only.py"


def require(text: str, token: str, label: str = "workflow") -> None:
    if token not in text:
        raise SystemExit(f"auto-merge main PR preflight: {label} missing required token: {token}")


def forbid(text: str, token: str, label: str = "workflow") -> None:
    if token in text:
        raise SystemExit(f"auto-merge main PR preflight: {label} forbidden token present: {token}")


if not WORKFLOW.is_file():
    raise SystemExit(f"auto-merge main PR preflight: workflow missing: {WORKFLOW.relative_to(ROOT)}")
if not CI_POLICY_GUARD.is_file():
    raise SystemExit(f"auto-merge main PR preflight: policy guard missing: {CI_POLICY_GUARD.relative_to(ROOT)}")

text = WORKFLOW.read_text(encoding="utf-8")
policy = CI_POLICY_GUARD.read_text(encoding="utf-8")

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
    "contents: write",
    "actions: write",
    "issues: write",
    "packages: write",
    "id-token: write",
    "git push",
    "gh release",
    "gh workflow run",
):
    forbid(text, token)

for token in (
    'AUTO_MERGE_WORKFLOW = "auto-merge-main-prs.yml"',
    'expected = {"pull_request_target"}',
    'github.event.pull_request.base.ref == \'main\'',
    '"gh pr merge --auto --merge"',
    '"actions/checkout"',
    '"--admin"',
):
    require(policy, token, "CI policy guard")

print("PASS auto-merge main PR policy source guard")
