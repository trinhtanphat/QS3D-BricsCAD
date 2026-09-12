#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/dispatch-v25-cloud-after-main-integration.yml"
errors = []

if not WORKFLOW.is_file():
    errors.append("missing V25 post-main dispatcher workflow")
    workflow = ""
else:
    workflow = WORKFLOW.read_text(encoding="utf-8")

required = (
    "dispatch_source_ref='refs/remotes/origin/qs3d-v25-dispatch-source'",
    'git fetch --no-tags origin "+refs/heads/main:${dispatch_source_ref}"',
    'fetched_dispatch_main="$(git rev-parse "${dispatch_source_ref}^{commit}")"',
    'git merge-base --is-ancestor "${source_sha}" "${fetched_dispatch_main}"',
    'git checkout --detach "${source_sha}"',
    'checked_out_source="$(git rev-parse HEAD)"',
    'if [[ "${checked_out_source,,}" != "${source_sha}" ]]; then',
)
for token in required:
    if token not in workflow:
        errors.append("dispatcher exact-source worktree contract missing token: " + token)

if workflow:
    source_guard = workflow.find('if [[ ! "${source_sha}" =~ ^[0-9a-f]{40}$ ]]; then')
    source_fetch = workflow.find("dispatch_source_ref='refs/remotes/origin/qs3d-v25-dispatch-source'", source_guard)
    source_ancestry = workflow.find('git merge-base --is-ancestor "${source_sha}" "${fetched_dispatch_main}"', source_fetch)
    source_checkout = workflow.find('git checkout --detach "${source_sha}"', source_ancestry)
    head_capture = workflow.find('checked_out_source="$(git rev-parse HEAD)"', source_checkout)
    head_compare = workflow.find('if [[ "${checked_out_source,,}" != "${source_sha}" ]]; then', head_capture)
    release_workflow_read = workflow.find("release_workflow='.github/workflows/release-v25-cloud.yml'", head_compare)
    version_read = workflow.find('version_project="src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"', head_compare)
    batch_gate = workflow.find("scripts/v25-release-batch-gate.py", head_compare)
    indexes = (
        source_guard,
        source_fetch,
        source_ancestry,
        source_checkout,
        head_capture,
        head_compare,
        release_workflow_read,
        version_read,
        batch_gate,
    )
    if min(indexes) < 0 or not (
        source_guard < source_fetch < source_ancestry < source_checkout
        < head_capture < head_compare < release_workflow_read < version_read < batch_gate
    ):
        errors.append(
            "dispatcher must fetch protected main, prove admitted source ancestry, detach to source_sha, verify HEAD equality, then inspect release bytes"
        )

    workflow_run_rebind = workflow.find('if [[ "${GITHUB_EVENT_NAME}" == "workflow_run" ]]; then')
    current_main_rebind = workflow.find('source_sha="${current_main,,}"', workflow_run_rebind)
    if workflow_run_rebind < 0 or current_main_rebind < 0:
        errors.append("workflow_run path must explicitly rebind source_sha to current protected main before exact-source checkout")

print("QS3D V25 dispatcher exact-source worktree binding preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: V25 dispatcher proves ancestry and detaches to exact admitted source_sha before release inspection and batch-gate execution.")
