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
    "superseded dispatcher ${source_sha} exits before reservation/dispatch",
    "main advanced only through non-release paths",
    'reservation="${reservation_prefix} ordinal=${preview} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
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
    "git reset --hard",
    "git checkout --detach $releaseBase",
    "sync-preview-release-version.ps1",
    "preflight-runtime-product-version-identity.py",
    "Release workspace HEAD must remain the protected-main source commit",
    "$latestMain = Get-RemoteMain",
    "main advanced through additional non-release paths while preparing the workspace",
    "No commit, push, branch-protection bypass, or main mutation was performed by release preparation.",
    "Write-Output $releaseBase",
)
for token in prepare_tokens:
    if token not in prepare:
        errors.append("release preparation drift contract missing token: " + token)

for forbidden in (
    "git push",
    "git commit",
    "git add",
    "HEAD:refs/heads/main",
):
    if forbidden.lower() in prepare.lower():
        errors.append("protected-main release preparation must not contain write primitive: " + forbidden)

if workflow:
    drift_guard = workflow.find('if [[ "${current_main}" != "${source_sha}" ]]; then')
    relevant_exit_guard = workflow.find("if (( release_relevant_drift != 0 )); then", drift_guard)
    exit_index = workflow.find("exit 0", relevant_exit_guard)
    inert_continue = workflow.find("main advanced only through non-release paths", exit_index)
    reservation = workflow.find('reservation="${reservation_prefix} ordinal=${preview} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"')
    dispatch = workflow.find("gh workflow run release-v25-cloud.yml")
    indexes = (drift_guard, relevant_exit_guard, exit_index, inert_continue, reservation, dispatch)
    if min(indexes) < 0 or not (
        drift_guard < relevant_exit_guard < exit_index < inert_continue < reservation < dispatch
    ):
        errors.append(
            "dispatcher ordering must classify drift, exit only for release-relevant drift, continue inert drift, then reserve and dispatch"
        )

if prepare:
    checkout_index = prepare.find("git checkout --detach $releaseBase")
    sync_index = prepare.find("sync-preview-release-version.ps1")
    head_guard_index = prepare.find("Release workspace HEAD must remain the protected-main source commit")
    refetch_index = prepare.find("$latestMain = Get-RemoteMain")
    retry_index = prepare.find("main advanced through additional non-release paths while preparing the workspace")
    output_index = prepare.find("Write-Output $releaseBase")
    indexes = (checkout_index, sync_index, head_guard_index, refetch_index, retry_index, output_index)
    if min(indexes) < 0 or not (
        checkout_index < sync_index < head_guard_index < refetch_index < retry_index < output_index
    ):
        errors.append(
            "release preparation must select a safe base, sync workspace-only identity, preserve HEAD, recheck main drift, then output the exact source SHA"
        )
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
    "absorbs only non-release main drift, and retries workspace-only release preparation without writing protected main."
)
