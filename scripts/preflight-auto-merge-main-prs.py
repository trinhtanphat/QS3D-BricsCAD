#!/usr/bin/env python3
"""Fail closed if repository-wide Ready PR auto-update/auto-merge policy regresses."""

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
    "name: Auto-update and auto-merge ready main PRs",
    "pull_request_target:",
    "push:",
    "- main",
    "opened",
    "reopened",
    "synchronize",
    "ready_for_review",
    "converted_to_draft",
    "contents: write",
    "pull-requests: write",
    "github.event_name == 'pull_request_target'",
    "github.event.pull_request.base.ref == 'main'",
    "github.event_name == 'push'",
    "github.ref == 'refs/heads/main'",
    "GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}",
    "gh pr view",
    "state,isDraft,isCrossRepository,mergeStateStatus,headRefOid,autoMergeRequest,baseRefName",
    "is_draft",
    "is_cross_repository",
    "merge_state",
    "head_oid",
    "BEHIND",
    'gh api --method PUT "repos/$GH_REPO/pulls/$pr_number/update-branch"',
    'expected_head_sha="$head_oid"',
    "gh pr list",
    "--base main",
    "--state open",
    "--limit 1000",
    "gh pr merge --auto --merge",
):
    require(text, token)

for token in (
    "gh pr ready",
    "actions/checkout",
    "--admin",
    "github.event.pull_request.head.sha",
    "github.event.pull_request.head.ref",
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
    'expected = {"pull_request_target", "push"}',
    "github.event_name == 'pull_request_target'",
    "github.event.pull_request.base.ref == 'main'",
    "github.event_name == 'push'",
    "github.ref == 'refs/heads/main'",
    '"contents: write"',
    '"gh pr merge --auto --merge"',
    '"gh pr ready"',
    '"actions/checkout"',
    '"--admin"',
):
    require(policy, token, "CI policy guard")

for token in (
    'AUTO_MERGE_WORKFLOW = "auto-merge-main-prs.yml"',
    "workflow.name != AUTO_MERGE_WORKFLOW",
    "Auto-update and auto-merge ready main PRs",
    "contents: write",
    "update-branch",
    "expected_head_sha",
    '"gh pr merge --auto --merge"',
    '"gh pr ready"',
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
    "## Automatic Ready PR refresh/auto-merge arming",
    ".github/workflows/auto-merge-main-prs.yml",
    "Draft is a manual hold",
    "update-branch",
    "expected_head_sha",
    "contents: write",
    "does not perform the final merge",
    "Repository-wide native auto-merge arming is explicitly enabled",
    "branch protection and required checks remain authoritative",
):
    require(ci_policy, token, "CI policy document")

for token in (
    "automatically marks a draft PR Ready for review",
    "gh pr ready",
    "Automatic branch refresh/update-branch behavior is not part of this workflow",
    "Repository-wide blind auto-merge remains intentionally disabled",
    ".github/workflows/hybrid-pr-coordinator.yml` is the single owner-approved queue coordinator",
):
    forbid(ci_policy, token, "CI policy document")

print("PASS auto-update/auto-merge ready main PR policy source guard")
