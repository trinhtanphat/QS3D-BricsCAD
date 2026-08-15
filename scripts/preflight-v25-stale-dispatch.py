#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "dispatch-v25-cloud-after-main-integration.yml"
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
errors = []

surface_contract = (
    ("src/*", "'src/'"),
    ("tests/*", "'tests/'"),
    ("scripts/*", "'scripts/'"),
    ("Directory.Build.props", "'Directory.Build.props'"),
    ("QS3D.sln", "'QS3D.sln'"),
    ("QS3D.V26.sln", "'QS3D.V26.sln'"),
    (".github/workflows/release-v25-cloud.yml", "'.github/workflows/release-v25-cloud.yml'"),
    (
        ".github/workflows/dispatch-v25-cloud-after-main-integration.yml",
        "'.github/workflows/dispatch-v25-cloud-after-main-integration.yml'",
    ),
)

if not WORKFLOW.is_file():
    errors.append("missing V25 post-main dispatcher workflow")
    workflow = ""
else:
    workflow = WORKFLOW.read_text(encoding="utf-8")

if not PREPARE.is_file():
    errors.append("missing V25 release preparation script")
    prepare = ""
else:
    prepare = PREPARE.read_text(encoding="utf-8")

for dispatcher_surface, prepare_surface in surface_contract:
    if dispatcher_surface not in workflow:
        errors.append("dispatcher release-relevant surface missing: " + dispatcher_surface)
    if prepare_surface not in prepare:
        errors.append("release preparation surface classifier missing: " + prepare_surface)

workflow_tokens = (
    'if [[ "${current_main}" != "${source_sha}" ]]; then',
    'git merge-base --is-ancestor "${source_sha}" "${current_main}"',
    'git diff --name-only "${source_sha}..${current_main}" --',
    "release_relevant_drift=0",
    "release_relevant_drift=1",
    "if (( release_relevant_drift != 0 )); then",
    "superseded dispatcher ${source_sha} exits before release dispatch",
    "main advanced only through non-release paths",
    'actions/workflows/release-v25-cloud.yml/runs?per_page=100',
    'select(.status != "completed")',
    "if (( active_runs == 0 )); then",
    "git fetch --force --tags origin",
    "gh workflow run release-v25-cloud.yml",
    '-f source_sha="${source_sha}"',
)
for token in workflow_tokens:
    if token not in workflow:
        errors.append("dispatcher drift contract missing token: " + token)

prepare_tokens = (
    "$releaseRelevantPrefixes = @(",
    "$releaseRelevantExactPaths = @(",
    "function Get-ReleaseRelevantDriftPaths",
    "git merge-base --is-ancestor $dispatch $TargetSha",
    "git diff --name-only $range --",
    "function Assert-ReleaseBaseIsSafe",
    "main moved after dispatch with release-relevant changes",
    "$maxAttempts = 12",
    "git checkout --detach $releaseBase",
    "sync-preview-release-version.ps1",
    "git push origin 'HEAD:refs/heads/main'",
    "CAS lost to non-release main drift",
    "Prepared exact release source commit",
)
for token in prepare_tokens:
    if token not in prepare:
        errors.append("release preparation drift contract missing token: " + token)

if workflow:
    drift_guard = workflow.find('if [[ "${current_main}" != "${source_sha}" ]]; then')
    relevant_exit_guard = workflow.find("if (( release_relevant_drift != 0 )); then", drift_guard)
    exit_index = workflow.find("exit 0", relevant_exit_guard)
    inert_continue = workflow.find("main advanced only through non-release paths", exit_index)
    wait_index = workflow.find('actions/workflows/release-v25-cloud.yml/runs?per_page=100', inert_continue)
    no_active_index = workflow.find("if (( active_runs == 0 )); then", wait_index)
    tag_refresh_index = workflow.find("git fetch --force --tags origin", no_active_index)
    dispatch = workflow.find("gh workflow run release-v25-cloud.yml", tag_refresh_index)
    indexes = (
        drift_guard,
        relevant_exit_guard,
        exit_index,
        inert_continue,
        wait_index,
        no_active_index,
        tag_refresh_index,
        dispatch,
    )
    if min(indexes) < 0 or not (
        drift_guard
        < relevant_exit_guard
        < exit_index
        < inert_continue
        < wait_index
        < no_active_index
        < tag_refresh_index
        < dispatch
    ):
        errors.append(
            "dispatcher ordering must classify drift, exit only for release-relevant drift, continue inert drift, "
            "wait for prior V25 children, refresh published tags, then dispatch"
        )

    for forbidden in (
        "QS3D_V25_PREVIEW_RESERVATION",
        "reservation_issue=",
        "reservation_prefix=",
    ):
        if forbidden in workflow:
            errors.append("stale-dispatch contract must not depend on burned ordinal reservations: " + forbidden)

if prepare:
    checkout_index = prepare.find("git checkout --detach $releaseBase")
    sync_index = prepare.find("sync-preview-release-version.ps1")
    push_index = prepare.find("git push origin 'HEAD:refs/heads/main'")
    retry_index = prepare.find("CAS lost to non-release main drift")
    if min(checkout_index, sync_index, push_index, retry_index) < 0 or not (
        checkout_index < sync_index < push_index < retry_index
    ):
        errors.append("release preparation must select safe base before sync, push by CAS, then retry inert drift")
    if "Start a fresh release run instead of overwriting concurrent work." in prepare:
        errors.append("legacy unconditional main-drift failure must not remain in release preparation")

print("QS3D V25 main-drift preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print(
    "PASS: V25 automation preserves triggering source provenance, skips superseded release-relevant dispatches, "
    "waits for prior release children before published-tag allocation, absorbs only non-release main drift, "
    "and retries release preparation without overwriting concurrent work."
)
