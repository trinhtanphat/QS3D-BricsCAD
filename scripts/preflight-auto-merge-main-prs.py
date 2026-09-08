#!/usr/bin/env python3
"""Fail closed if repository-wide main PR auto-ready/auto-merge policy regresses."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "auto-merge-main-prs.yml"
CI_POLICY_GUARD = ROOT / "scripts" / "preflight-ci-manual-only.py"
PROFESSIONALISM_GUARD = ROOT / "scripts" / "preflight-repository-professionalism.py"
ACTIONS_SUPPLY_CHAIN_GUARD = ROOT / "scripts" / "check-actions-pinned.py"
CI_POLICY_DOC = ROOT / "CI_POLICY.md"


def require(text: str, token: str, label: str = "workflow") -> None:
    if token not in text:
        raise SystemExit(f"auto-merge main PR preflight: {label} missing required token: {token}")


def forbid(text: str, token: str, label: str = "workflow") -> None:
    if token in text:
        raise SystemExit(f"auto-merge main PR preflight: {label} forbidden token present: {token}")


for path, label in (
    (WORKFLOW, "workflow"),
    (CI_POLICY_GUARD, "CI policy guard"),
    (PROFESSIONALISM_GUARD, "professionalism guard"),
    (ACTIONS_SUPPLY_CHAIN_GUARD, "Actions supply-chain guard"),
    (CI_POLICY_DOC, "CI policy document"),
):
    if not path.is_file():
        raise SystemExit(f"auto-merge main PR preflight: {label} missing: {path.relative_to(ROOT)}")

text = WORKFLOW.read_text(encoding="utf-8")
policy = CI_POLICY_GUARD.read_text(encoding="utf-8")
professionalism = PROFESSIONALISM_GUARD.read_text(encoding="utf-8")
supply_chain = ACTIONS_SUPPLY_CHAIN_GUARD.read_text(encoding="utf-8")
ci_policy = CI_POLICY_DOC.read_text(encoding="utf-8")

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

for token in (
    'AUTO_MERGE_WORKFLOW = "auto-merge-main-prs.yml"',
    "workflow.name != AUTO_MERGE_WORKFLOW",
    '"gh pr merge --auto --merge"',
    '"--admin"',
    "retired Hybrid PR Coordinator workflow must remain removed",
):
    require(professionalism, token, "professionalism guard")

for token in (
    'AUTO_MERGE_WORKFLOW = "auto-merge-main-prs.yml"',
    "workflow_name != AUTO_MERGE_WORKFLOW",
    "pull_request_target is forbidden for repository workflows except the owner-approved PR metadata automation",
    "every external workflow action is pinned",
):
    require(supply_chain, token, "Actions supply-chain guard")

for token in (
    "## Automatic PR ready/auto-merge arming",
    ".github/workflows/auto-merge-main-prs.yml",
    "does not perform the final merge",
    "Repository-wide native auto-merge arming is explicitly enabled",
    "branch protection and required checks remain authoritative",
):
    require(ci_policy, token, "CI policy document")

for token in (
    "Repository-wide blind auto-merge remains intentionally disabled",
    ".github/workflows/hybrid-pr-coordinator.yml` is the single owner-approved queue coordinator",
):
    forbid(ci_policy, token, "CI policy document")

print("PASS auto-merge main PR policy source guard")
