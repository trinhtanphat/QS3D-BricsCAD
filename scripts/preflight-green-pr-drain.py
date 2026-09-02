#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "green-pr-drain.yml"
POLICY_SCANNER = ROOT / "scripts" / "preflight-ci-manual-only.py"
CI_POLICY = ROOT / "CI_POLICY.md"
MAIN_WRITE = ROOT / "docs" / "MAIN-WRITE-AUTHORIZATION.md"
REGISTRATION = ROOT / "docs" / "AGENT-WORK-REGISTRATION.md"

errors = []


def read_required(path):
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        errors.append(f"cannot read {path.relative_to(ROOT)} as UTF-8: {exc}")
        return ""


def require_tokens(text, tokens, label):
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing required token: {token}")


workflow = read_required(WORKFLOW)
policy_scanner = read_required(POLICY_SCANNER)
ci_policy = read_required(CI_POLICY)
main_write = read_required(MAIN_WRITE)
registration = read_required(REGISTRATION)

require_tokens(
    workflow,
    (
        "name: QS3D Green PR Drain",
        "  workflow_run:",
        "      - QS3D Shared Branch and Integration CI",
        "      - completed",
        "  contents: read",
        "  pull-requests: read",
        "group: qs3d-green-pr-drain",
        "cancel-in-progress: false",
        "if: ${{ github.event.workflow_run.conclusion == 'success' && github.event.workflow_run.event == 'pull_request' }}",
        "GH_TOKEN: ${{ secrets.QS3D_AUTOMERGE_TOKEN }}",
        "RUN_HEAD_SHA: ${{ github.event.workflow_run.head_sha }}",
        "RUN_PRS_JSON: ${{ toJson(github.event.workflow_run.pull_requests) }}",
        '[[ "${state}" != "open" ]]',
        '[[ "${draft}" != "false" ]]',
        '.labels | any(.name == "no-automerge")',
        '[[ "${base_ref}" != "main" ]]',
        '[[ "${head_repo}" != "${GITHUB_REPOSITORY}" ]]',
        '[[ "${head_sha}" != "${RUN_HEAD_SHA}" ]]',
        '"/repos/${GITHUB_REPOSITORY}/branches/main"',
        '"/repos/${GITHUB_REPOSITORY}/compare/${main_sha}...${RUN_HEAD_SHA}"',
        '[[ "${merge_base_sha}" != "${main_sha}"',
        '"${compare_status}" != "ahead"',
        '"${compare_status}" != "identical"',
        '"/repos/${GITHUB_REPOSITORY}/pulls/${pr_number}/merge"',
        '-f merge_method=merge',
        '-f sha="${RUN_HEAD_SHA}"',
        '"/repos/${GITHUB_REPOSITORY}/pulls/${other_number}/update-branch"',
        '-f expected_head_sha="${other_head_sha}"',
    ),
    "green-pr-drain.yml",
)

for forbidden in (
    "${{ github.token }}",
    "${{ secrets.GITHUB_TOKEN }}",
    "actions/checkout@",
    "git push",
    "--force",
    "force-push",
):
    if forbidden in workflow:
        errors.append(f"green-pr-drain.yml contains forbidden token: {forbidden}")

require_tokens(
    policy_scanner,
    (
        'GREEN_PR_DRAIN = "green-pr-drain.yml"',
        "elif path.name == GREEN_PR_DRAIN:",
        'expected = {"workflow_run"}',
        "green PR drain",
        "QS3D_AUTOMERGE_TOKEN",
        "no-automerge",
    ),
    "preflight-ci-manual-only.py",
)

require_tokens(
    ci_policy,
    (
        "green-pr-drain.yml",
        "exact-head",
        "current `main`",
        "QS3D_AUTOMERGE_TOKEN",
        "no-automerge",
        "no force",
    ),
    "CI_POLICY.md",
)

require_tokens(
    main_write,
    (
        "green-pr-drain.yml",
        "repository-wide merge coordinator",
        "no-automerge",
        "normal agents",
    ),
    "docs/MAIN-WRITE-AUTHORIZATION.md",
)

require_tokens(
    registration,
    (
        "green-pr-drain.yml",
        "owner-authorized green PR drain",
    ),
    "docs/AGENT-WORK-REGISTRATION.md",
)

print("QS3D green PR drain source preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: the owner-approved green PR drain is exact-head/current-main guarded, same-repository only, serialized, opt-out aware, dedicated-token authenticated, and refreshes without force rewriting."
)
