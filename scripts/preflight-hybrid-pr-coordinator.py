#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "hybrid-pr-coordinator.yml"

errors: list[str] = []


def fail(message: str) -> None:
    errors.append(message)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label} missing required token: {token}")


def reject(text: str, token: str, label: str) -> None:
    if token.lower() in text.lower():
        fail(f"{label} contains forbidden token: {token}")


def job_block(text: str, job_name: str) -> str:
    lines = text.splitlines()
    jobs_index = next((i for i, line in enumerate(lines) if line == "jobs:"), None)
    if jobs_index is None:
        return ""
    current = None
    block: list[str] = []
    for line in lines[jobs_index + 1 :]:
        if line and not line.startswith((" ", "\t", "#")):
            break
        match = re.match(r"^  ([A-Za-z0-9_-]+):\s*(?:#.*)?$", line)
        if match:
            if current == job_name:
                break
            current = match.group(1)
            block = []
            continue
        if current == job_name:
            block.append(line)
    return "\n".join(block)


def require_exact_job_if(block: str, purpose: str, expected: str) -> None:
    matches = re.findall(r"(?m)^    if:\s*(.*?)\s*$", block)
    if len(matches) != 1:
        fail(f"{purpose} must expose exactly one top-level job if guard")
        return
    expression = matches[0].strip()
    if expression.startswith("${{") and expression.endswith("}}"):
        expression = expression[3:-2].strip()
    expression = re.sub(r"\s+", " ", expression)
    if expression != expected:
        fail(f"{purpose} must use the exact fail-closed event guard: {expected}")


def require_fail_closed_secret_gate(block: str, purpose: str, message: str) -> None:
    pattern = (
        r'if \[\[ -z "\$\{GH_TOKEN:-\}" \]\]; then\s*\n'
        + r'\s*echo "::error::'
        + re.escape(message)
        + r'"\s*\n'
        + r'\s*exit 1\s*\n'
        + r'\s*fi'
    )
    if not re.search(pattern, block):
        fail(f"{purpose} must fail closed with exit 1 when QS3D_AUTOMERGE_TOKEN is unavailable")


if not WORKFLOW.is_file():
    fail("missing .github/workflows/hybrid-pr-coordinator.yml")
    text = ""
else:
    try:
        text = WORKFLOW.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        fail(f"cannot read coordinator as strict UTF-8: {exc}")
        text = ""

if text:
    required_tokens = (
        "name: QS3D Hybrid PR Coordinator",
        '  "pull_request":',
        '  "push":',
        '  "workflow_run":',
        '      - "QS3D Shared Branch and Integration CI"',
        "      - completed",
        "      - opened",
        "      - reopened",
        "      - ready_for_review",
        "      - converted_to_draft",
        "      - synchronize",
        "      - labeled",
        "      - unlabeled",
        "      - main",
        "permissions:",
        "  contents: read",
        "  actions: read",
        "  pull-requests: write",
        "concurrency:",
        "  group: qs3d-hybrid-pr-coordinator",
        "  cancel-in-progress: false",
        "arm-native-automerge:",
        "promote-green-draft:",
        "refresh-branches:",
        "enablePullRequestAutoMerge",
        "disablePullRequestAutoMerge",
        "markPullRequestReadyForReview",
        "pullRequestId",
        "autoMergeRequest",
        "no-automerge",
        "event_head_sha",
        "api_head_sha",
        "head.repo.full_name",
        "base.ref",
        "draft",
        "dependabot[bot]",
        "QS3D_AUTOMERGE_TOKEN",
        "/update-branch",
        "expected_head_sha",
    )
    for token in required_tokens:
        require(text, token, "hybrid coordinator")

    forbidden_tokens = (
        "pull_request_target:",
        "gh pr merge",
        "git push",
        "git reset",
        "--force",
        "gh workflow run",
        "gh release",
        "contents: write",
        "actions: write",
        "::warning::QS3D_AUTOMERGE_TOKEN is unavailable",
    )
    for token in forbidden_tokens:
        reject(text, token, "hybrid coordinator")

    if re.search(r"repos/[^\s\"']+/pulls/[^\s\"']+/merge(?:[\s\"']|$)", text, re.IGNORECASE):
        fail("hybrid coordinator contains a direct pull-request merge endpoint")

    arm = job_block(text, "arm-native-automerge")
    promote = job_block(text, "promote-green-draft")
    refresh = job_block(text, "refresh-branches")
    if not arm:
        fail("hybrid coordinator missing arm-native-automerge job block")
    if not promote:
        fail("hybrid coordinator missing promote-green-draft job block")
    if not refresh:
        fail("hybrid coordinator missing refresh-branches job block")

    require_exact_job_if(arm, "arm-native-automerge", "github.event_name == 'pull_request'")
    require_exact_job_if(
        promote,
        "promote-green-draft",
        "github.event_name == 'workflow_run' && github.event.workflow_run.conclusion == 'success' && github.event.workflow_run.event == 'pull_request'",
    )
    require_exact_job_if(refresh, "refresh-branches", "github.event_name == 'push' && github.ref == 'refs/heads/main'")

    arm_secret_error = "QS3D_AUTOMERGE_TOKEN is required; native auto-merge coordination cannot run."
    promote_secret_error = "QS3D_AUTOMERGE_TOKEN is required; GREEN draft promotion cannot run."
    refresh_secret_error = "QS3D_AUTOMERGE_TOKEN is required; branch refresh cannot run."

    for token in (
        "github.event_name == 'pull_request'",
        "GH_TOKEN: ${{ secrets.QS3D_AUTOMERGE_TOKEN }}",
        "event_head_sha",
        "api_head_sha",
        "enablePullRequestAutoMerge",
        "disablePullRequestAutoMerge",
        "pullRequestId",
        "autoMergeRequest",
        "no-automerge",
        "head.repo.full_name",
        "base.ref",
        "draft",
        "dependabot[bot]",
        arm_secret_error,
    ):
        require(arm, token, "arm-native-automerge")
    reject(arm, "markPullRequestReadyForReview", "arm-native-automerge")

    for token in (
        "github.event_name == 'workflow_run'",
        "github.event.workflow_run.conclusion == 'success'",
        "github.event.workflow_run.event == 'pull_request'",
        "GH_TOKEN: ${{ secrets.QS3D_AUTOMERGE_TOKEN }}",
        "github.event.workflow_run.head_sha",
        "github.event.workflow_run.pull_requests",
        "event_head_sha",
        "api_head_sha",
        "head.repo.full_name",
        "base.ref",
        "draft",
        "no-automerge",
        "dependabot[bot]",
        "/compare/",
        "/update-branch",
        "expected_head_sha",
        "markPullRequestReadyForReview",
        "enablePullRequestAutoMerge",
        "autoMergeRequest",
        promote_secret_error,
    ):
        require(promote, token, "promote-green-draft")
    reject(promote, "disablePullRequestAutoMerge", "promote-green-draft")

    for token in (
        "github.event_name == 'push'",
        "github.ref == 'refs/heads/main'",
        "GH_TOKEN: ${{ secrets.QS3D_AUTOMERGE_TOKEN }}",
        "QS3D_AUTOMERGE_TOKEN",
        "/update-branch",
        "expected_head_sha",
        "no-automerge",
        "head.repo.full_name",
        "draft",
        "dependabot[bot]",
        "gh api graphql",
        "enablePullRequestAutoMerge",
        "autoMergeRequest",
        refresh_secret_error,
    ):
        require(refresh, token, "refresh-branches")
    reject(refresh, "disablePullRequestAutoMerge", "refresh-branches")
    reject(refresh, "markPullRequestReadyForReview", "refresh-branches")

    require_fail_closed_secret_gate(arm, "arm-native-automerge", arm_secret_error)
    require_fail_closed_secret_gate(promote, "promote-green-draft", promote_secret_error)
    require_fail_closed_secret_gate(refresh, "refresh-branches", refresh_secret_error)

    for job_name, block in (("arm-native-automerge", arm), ("promote-green-draft", promote), ("refresh-branches", refresh)):
        if "github.token" in block:
            fail(f"{job_name} must not fall back to github.token")
        if "secrets.QS3D_AUTOMERGE_TOKEN" not in block:
            fail(f"{job_name} must use QS3D_AUTOMERGE_TOKEN")

print("QS3D hybrid PR coordinator preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: draft promotion is gated by successful exact-head Shared CI, while PR events only reconcile native auto-merge and main pushes refresh eligible branches.")
