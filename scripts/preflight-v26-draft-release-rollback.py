#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper_path = root / "scripts" / "rollback-v26-draft-release.ps1"
workflow_path = root / ".github" / "workflows" / "release-v26.yml"

helper = helper_path.read_text(encoding="utf-8")
workflow = workflow_path.read_text(encoding="utf-8")


def validate(helper_text: str, workflow_text: str) -> list[str]:
    errors: list[str] = []

    required_helper = [
        "[Parameter(Mandatory = $true)][bool]$TagWasAbsentBeforeCreate",
        "if (-not $TagWasAbsentBeforeCreate)",
        "Rollback requires proof that the release tag was absent immediately before this transaction created the draft.",
        "[long]$release.id -ne $ReleaseId",
        "release.url",
        "Release repository identity mismatch",
        "if ($release.draft -ne $true)",
        "Release $ReleaseId is not a draft; refusing destructive rollback.",
        "release.tag_name",
        "Resolve-ExactRemoteTagSha",
        "git ls-remote --tags origin $tagRef $peeledRef",
        "if (-not [string]::Equals($resolvedBefore, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))",
        "Remote tag $ReleaseTag moved to",
        "Invoke-RestMethod -Method Delete -Uri $releaseUri",
        "releases/tags/",
        "A release still owns tag $ReleaseTag after draft deletion; refusing tag deletion.",
        "if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))",
        "Remote tag $ReleaseTag changed during rollback; refusing tag deletion.",
        "git/refs/tags/",
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
    ]
    for needle in required_helper:
        if needle not in helper_text:
            errors.append(f"helper missing fail-closed contract: {needle}")

    for forbidden in [
        "git push --delete",
        "git push origin :refs/tags/",
        "-Force",
    ]:
        if forbidden in helper_text:
            errors.append(f"helper uses broad/destructive shortcut: {forbidden}")

    release_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $releaseUri")
    owner_check = helper_text.find("A release still owns tag $ReleaseTag after draft deletion; refusing tag deletion.")
    post_sha_check = helper_text.find("Remote tag $ReleaseTag changed during rollback; refusing tag deletion.")
    tag_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $tagRefUri")
    if min(release_delete, owner_check, post_sha_check, tag_delete) < 0 or not (
        release_delete < owner_check < post_sha_check < tag_delete
    ):
        errors.append("helper deletion order must be draft delete -> release-owner check -> exact-SHA recheck -> tag delete")

    required_workflow = [
        "$preCreateTagLines = @(git ls-remote --tags origin $preCreateTagRef $preCreatePeeledRef)",
        "if ($preCreateTagLines.Count -ne 0)",
        "Remote V26 release tag already existed before draft creation; refusing transaction ownership",
        "$tagWasAbsentBeforeCreate = $true",
        "$releaseCreatedByThisRun = $false",
        "$releaseCreatedByThisRun = $true",
        "try {",
        "catch {",
        "$publicationError = $_",
        "if (-not $releaseCreatedByThisRun -or $null -eq $release -or [long]$release.id -le 0)",
        "rollback-v26-draft-release.ps1",
        "-ReleaseId ([long]$release.id)",
        "-ReleaseTag $env:RELEASE_TAG",
        "-WorkflowSha $env:GITHUB_SHA",
        "-TagWasAbsentBeforeCreate $tagWasAbsentBeforeCreate",
        "-Token $env:GH_TOKEN",
        "Automatic V26 draft rollback failed",
        "Original publication error:",
        "Rollback error:",
        "Manual cleanup is required before retry.",
        "V26 publication failed after draft creation",
        "Automatic rollback completed; retry with the same tag is safe.",
    ]
    for needle in required_workflow:
        if needle not in workflow_text:
            errors.append(f"workflow missing restart-safe transaction contract: {needle}")

    precheck = workflow_text.find("$preCreateTagLines = @(git ls-remote --tags origin $preCreateTagRef $preCreatePeeledRef)")
    release_create = workflow_text.find('$release = Invoke-RestMethod -Method Post')
    owned = workflow_text.find("$releaseCreatedByThisRun = $true", release_create + 1)
    first_tag_check = workflow_text.find("Assert-RemoteReleaseTagTargetsWorkflowSha", owned + 1)
    held_local_hash = workflow_text.find("verify-v26-held-file.ps1 -Operation Hash -Path $localAsset", first_tag_check + 1)
    held_remote_hash = workflow_text.find("verify-v26-held-file.ps1 -Operation Hash -Path $downloadedAsset", held_local_hash + 1)
    second_tag_check = workflow_text.find("Assert-RemoteReleaseTagTargetsWorkflowSha", held_remote_hash + 1)
    publish_release = workflow_text.find("$published = Invoke-RestMethod -Method Patch", second_tag_check + 1)
    catch_block = workflow_text.find("$publicationError = $_", publish_release + 1)
    rollback_call = workflow_text.find("rollback-v26-draft-release.ps1", catch_block + 1)
    if min(precheck, release_create, owned, first_tag_check, held_local_hash, held_remote_hash, second_tag_check, publish_release, catch_block, rollback_call) < 0 or not (
        precheck < release_create < owned < first_tag_check < held_local_hash < held_remote_hash < second_tag_check < publish_release < catch_block < rollback_call
    ):
        errors.append("workflow order must be remote-absence proof -> draft create/ownership -> exact-SHA/assets -> publish -> bounded rollback catch")

    return errors


canonical_errors = validate(helper, workflow)
if canonical_errors:
    raise SystemExit("V26 draft rollback contract failed: " + "; ".join(canonical_errors))

# Mutation probes keep the guard meaningful: each critical safety property must fail
# independently if it is removed from otherwise-valid source.
mutations = {
    "preexisting-tag ownership": (helper, workflow.replace("if ($preCreateTagLines.Count -ne 0)", "if ($false)", 1)),
    "draft-only deletion": (helper.replace("if ($release.draft -ne $true)", "if ($false)", 1), workflow),
    "exact-SHA tag ownership": (helper.replace("if (-not [string]::Equals($resolvedBefore, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))", "if ($false)", 1), workflow),
    "post-delete exact-SHA recheck": (helper.replace("if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))", "if ($false)", 1), workflow),
    "transaction rollback wiring": (helper, workflow.replace("& .\\scripts\\rollback-v26-draft-release.ps1", "# rollback removed", 1)),
}
for label, (mutated_helper, mutated_workflow) in mutations.items():
    if not validate(mutated_helper, mutated_workflow):
        raise SystemExit(f"V26 draft rollback mutation probe did not fail closed: {label}")

print("PASS V26 draft release rollback contract")