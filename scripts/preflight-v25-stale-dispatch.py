#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/dispatch-v25-cloud-after-main-integration.yml"
PREPARE = ROOT / "scripts" / "prepare-v25-cloud-release.ps1"
errors = []

surface_contract = (
    ("src/**", "'src/'"),
    ("tests/**", "'tests/'"),
    ("scripts/**", "'scripts/'"),
    ("external/QS3D-Platform", "'external/QS3D-Platform'"),
    (".gitmodules", "'.gitmodules'"),
    ("Directory.Build.props", "'Directory.Build.props'"),
    ("QS3D.sln", "'QS3D.sln'"),
    ("QS3D.V26.sln", "'QS3D.V26.sln'"),
    (".github/workflows/release-v25-cloud.yml", "'.github/workflows/release-v25-cloud.yml'"),
    (
        ".github/workflows/dispatch-v25-cloud-after-main-integration.yml",
        "'.github/workflows/dispatch-v25-cloud-after-main-integration.yml'",
    ),
)


def contains_executable_line(text: str, token: str) -> bool:
    token_lower = token.lower()
    return any(
        token_lower in line.lower()
        for line in text.splitlines()
        if not line.lstrip().startswith("#")
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
    "release_relevant_pathspecs=(",
    'git diff --quiet --no-ext-diff "${source_sha}..${current_main}" -- "${release_relevant_pathspecs[@]}"',
    "release_drift_status=$?",
    "if (( release_drift_status == 1 )); then",
    "if (( release_drift_status != 0 )); then",
    "superseded dispatcher ${source_sha} exits before reservation/dispatch",
    "main advanced only through non-release paths",
    "committed_preview_ordinal=",
    'reservation="${reservation_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"',
    "gh workflow run release-v25-cloud.yml",
    '-f source_sha="${source_sha}"',
)
for token in workflow_tokens:
    if token not in workflow:
        errors.append("dispatcher drift contract missing token: " + token)

prepare_tokens = (
    "$releaseRelevantPathspecs = @(",
    "'external/QS3D-Platform'",
    "function Test-ReleaseRelevantDrift",
    "git merge-base --is-ancestor $dispatch $TargetSha",
    "git diff --quiet --no-ext-diff $range -- @releaseRelevantPathspecs",
    "$diffExit = $LASTEXITCODE",
    "function Assert-ReleaseBaseIsSafe",
    "main moved after dispatch with release-relevant changes",
    "$maxAttempts = 12",
    "git reset --hard",
    "git checkout --detach $releaseBase",
    "preflight-runtime-product-version-identity.py",
    "$workspaceVersionPaths = @(",
    "function Set-WorkspaceProductVersion",
    "$productVersion = $tag.Substring(1)",
    "Set-WorkspaceProductVersion -ReleaseTagValue $tag",
    "Runtime product-version identity preflight failed after workspace synchronization.",
    "$expectedProductVersion = $tag.Substring(1)",
    "$finalStatus.Count -ne 0 -and $finalStatus.Count -ne $workspaceVersionPaths.Count",
    "Workspace version synchronization must either be a no-op or produce exactly three bounded project modifications.",
    "Workspace ProductVersion is already synchronized",
    "if ($finalStatus.Count -eq $workspaceVersionPaths.Count)",
    "Unexpected release-preparation workspace change",
    "Release workspace HEAD must remain the protected-main source commit",
    "$latestMain = Get-RemoteMain",
    "main advanced through additional non-release paths while validating release source",
    "No commit, push, branch-protection bypass, or protected-main mutation was performed by release preparation.",
    "Write-Output $releaseBase",
)
for token in prepare_tokens:
    if token not in prepare:
        errors.append("release preparation drift contract missing token: " + token)

for stale in (
    "function Get-CommittedProductVersion",
    "$committedProductVersion = Get-CommittedProductVersion",
    "Merge the version update to protected main before publishing.",
):
    if stale in prepare:
        errors.append("manual release retained stale committed-version admission: " + stale)

for forbidden in (
    "git push",
    "git commit",
    "git add",
    "HEAD:refs/heads/main",
    "sync-preview-release-version.ps1",
    "git diff --name-only",
):
    if contains_executable_line(prepare, forbidden):
        errors.append("protected-main release preparation must not contain write/line-parser primitive: " + forbidden)

for forbidden in (
    "max_preview=",
    "preview=$((max_preview + 1))",
    'reservation="${reservation_prefix} ordinal=${preview}',
):
    if forbidden in workflow:
        errors.append("dispatcher stale-drift path must not reintroduce independent preview allocation: " + forbidden)

if workflow:
    pathspecs = workflow.find("release_relevant_pathspecs=(")
    drift_guard = workflow.find('if [[ "${current_main}" != "${source_sha}" ]]; then')
    diff_guard = workflow.find('git diff --quiet --no-ext-diff "${source_sha}..${current_main}" -- "${release_relevant_pathspecs[@]}"', drift_guard)
    relevant_exit_guard = workflow.find("if (( release_drift_status == 1 )); then", diff_guard)
    exit_index = workflow.find("exit 0", relevant_exit_guard)
    error_guard = workflow.find("if (( release_drift_status != 0 )); then", exit_index)
    inert_continue = workflow.find("main advanced only through non-release paths", error_guard)
    committed_identity = workflow.find("committed_preview_ordinal=", inert_continue)
    reservation = workflow.find('reservation="${reservation_prefix} ordinal=${committed_preview_ordinal} source_sha=${source_sha} run_id=${GITHUB_RUN_ID}"', committed_identity)
    dispatch = workflow.find("gh workflow run release-v25-cloud.yml", reservation)
    indexes = (
        pathspecs, drift_guard, diff_guard, relevant_exit_guard, exit_index,
        error_guard, inert_continue, committed_identity, reservation, dispatch,
    )
    if min(indexes) < 0 or not (
        pathspecs < drift_guard < diff_guard < relevant_exit_guard < exit_index < error_guard
        < inert_continue < committed_identity < reservation < dispatch
    ):
        errors.append(
            "dispatcher ordering must classify drift safely, keep automatic committed identity allocation, then reserve and dispatch"
        )
    if contains_executable_line(workflow, "git diff --name-only"):
        errors.append("dispatcher must not classify release drift from line-oriented pathname output")

if prepare:
    checkout_index = prepare.find("git checkout --detach $releaseBase")
    runtime_identity_index = prepare.find("preflight-runtime-product-version-identity.py")
    sync_index = prepare.find("Set-WorkspaceProductVersion -ReleaseTagValue $tag")
    post_sync_index = prepare.find("Runtime product-version identity preflight failed after workspace synchronization.")
    expected_index = prepare.find("$expectedProductVersion = $tag.Substring(1)")
    head_guard_index = prepare.find("Release workspace HEAD must remain the protected-main source commit")
    bounded_status_index = prepare.find("$finalStatus = @(Get-ReleaseStatusEntries)")
    refetch_index = prepare.find("$latestMain = Get-RemoteMain")
    retry_index = prepare.find("main advanced through additional non-release paths while validating release source")
    output_index = prepare.find("Write-Output $releaseBase")
    indexes = (
        checkout_index, runtime_identity_index, sync_index, post_sync_index, expected_index,
        head_guard_index, bounded_status_index, refetch_index, retry_index, output_index,
    )
    if min(indexes) < 0 or not (
        checkout_index < runtime_identity_index < sync_index < post_sync_index < expected_index
        < head_guard_index < bounded_status_index < refetch_index < retry_index < output_index
    ):
        errors.append(
            "manual release preparation must select safe base, sync bounded tag identity, validate it, preserve HEAD, bound dirty paths, recheck drift, then output exact source SHA"
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
    "PASS: V25 automation preserves triggering source provenance and pathname-safe drift handling; automatic dispatch keeps committed reservation identity while manual release accepts an already-synchronized preview identity or derives the requested preview identity only in the bounded workspace without writing protected main."
)
