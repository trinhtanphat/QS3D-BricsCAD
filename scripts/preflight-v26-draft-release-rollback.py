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
        "[Parameter(Mandatory = $true)][bool]$TagCreatedByThisRun",
        "if (-not $TagCreatedByThisRun)",
        "Rollback requires positive proof that this workflow run created the exact release tag ref.",
        "if ($ReleaseId -lt 0)",
        "if ($ReleaseId -gt 0)",
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
        "function Assert-NoReleaseOwnsTag",
        "releases?per_page=100&page=$page",
        "if ([string]::Equals([string]$candidate.tag_name, $ReleaseTag, [StringComparison]::Ordinal))",
        "A release still owns tag $ReleaseTag; refusing tag deletion.",
        "Release enumeration exceeded $maxPages pages",
        "Assert-NoReleaseOwnsTag",
        "if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))",
        "Remote tag $ReleaseTag changed during rollback; refusing tag deletion.",
        "git/refs/tags/",
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
    ]
    for needle in required_helper:
        if needle not in helper_text:
            errors.append(f"helper missing fail-closed contract: {needle}")

    for forbidden in [
        "TagWasAbsentBeforeCreate",
        "releases/tags/",
        "git push --delete",
        "git push origin :refs/tags/",
        "-Force",
    ]:
        if forbidden in helper_text:
            errors.append(f"helper uses stale/broad destructive contract: {forbidden}")

    release_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $releaseUri")
    owner_check = helper_text.find("Assert-NoReleaseOwnsTag", release_delete + 1)
    post_sha_check = helper_text.find("Remote tag $ReleaseTag changed during rollback; refusing tag deletion.")
    tag_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $tagRefUri")
    if min(release_delete, owner_check, post_sha_check, tag_delete) < 0 or not (
        release_delete < owner_check < post_sha_check < tag_delete
    ):
        errors.append("helper deletion order must be optional draft delete -> exhaustive release-owner check -> exact-SHA recheck -> tag delete")

    required_workflow = [
        '$tagRefUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/git/refs"',
        'ref = "refs/tags/$env:RELEASE_TAG"',
        "sha = $env:GITHUB_SHA",
        "$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri",
        'createdTag.ref, "refs/tags/$env:RELEASE_TAG"',
        "createdTag.object.sha, $env:GITHUB_SHA",
        "$tagCreatedByThisRun = $false",
        "$tagCreatedByThisRun = $true",
        "$releaseId = [long]0",
        "$releaseId = [long]$release.id",
        "try {",
        "catch {",
        "$publicationError = $_",
        "if (-not $tagCreatedByThisRun)",
        "rollback-v26-draft-release.ps1",
        "-ReleaseId $releaseId",
        "-ReleaseTag $env:RELEASE_TAG",
        "-WorkflowSha $env:GITHUB_SHA",
        "-TagCreatedByThisRun $tagCreatedByThisRun",
        "-Token $env:GH_TOKEN",
        "Automatic V26 draft rollback failed",
        "Original publication error:",
        "Rollback error:",
        "Manual cleanup is required before retry.",
        "V26 publication failed after transaction tag creation",
        "Automatic rollback completed; retry with the same tag is safe.",
    ]
    for needle in required_workflow:
        if needle not in workflow_text:
            errors.append(f"workflow missing positive-ownership transaction contract: {needle}")

    for stale in [
        "$preCreateTagLines",
        "$tagWasAbsentBeforeCreate",
        "$releaseCreatedByThisRun",
        "-TagWasAbsentBeforeCreate",
    ]:
        if stale in workflow_text:
            errors.append(f"workflow retains stale absence-based ownership contract: {stale}")

    tag_create = workflow_text.find("$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri")
    tag_owned = workflow_text.find("$tagCreatedByThisRun = $true", tag_create + 1)
    ownership_sha = workflow_text.find("createdTag.object.sha, $env:GITHUB_SHA", tag_create + 1)
    release_create = workflow_text.find('$release = Invoke-RestMethod -Method Post', tag_owned + 1)
    release_id = workflow_text.find("$releaseId = [long]$release.id", release_create + 1)
    first_tag_check = workflow_text.find("Assert-RemoteReleaseTagTargetsWorkflowSha", release_id + 1)
    held_local_hash = workflow_text.find("verify-v26-held-file.ps1 -Operation Hash -Path $localAsset", first_tag_check + 1)
    held_remote_hash = workflow_text.find("verify-v26-held-file.ps1 -Operation Hash -Path $downloadedAsset", held_local_hash + 1)
    second_tag_check = workflow_text.find("Assert-RemoteReleaseTagTargetsWorkflowSha", held_remote_hash + 1)
    publish_release = workflow_text.find("$published = Invoke-RestMethod -Method Patch", second_tag_check + 1)
    catch_block = workflow_text.find("$publicationError = $_", publish_release + 1)
    rollback_call = workflow_text.find("rollback-v26-draft-release.ps1", catch_block + 1)
    if min(tag_create, tag_owned, ownership_sha, release_create, release_id, first_tag_check, held_local_hash, held_remote_hash, second_tag_check, publish_release, catch_block, rollback_call) < 0 or not (
        tag_create < ownership_sha < tag_owned < release_create < release_id < first_tag_check < held_local_hash < held_remote_hash < second_tag_check < publish_release < catch_block < rollback_call
    ):
        errors.append("workflow order must be exact-ref create/validate/ownership -> draft create -> exact-SHA/assets -> publish -> bounded rollback catch")

    return errors


canonical_errors = validate(helper, workflow)
if canonical_errors:
    raise SystemExit("V26 draft rollback contract failed: " + "; ".join(canonical_errors))

mutations = {
    "positive tag ownership": (helper, workflow.replace("$tagCreatedByThisRun = $true", "$tagCreatedByThisRun = $false", 1)),
    "created-ref identity": (helper, workflow.replace('createdTag.ref, "refs/tags/$env:RELEASE_TAG"', 'createdTag.ref, "refs/tags/other"', 1)),
    "created-ref SHA": (helper, workflow.replace("createdTag.object.sha, $env:GITHUB_SHA", "createdTag.object.sha, ('0' * 40)", 1)),
    "draft-only deletion": (helper.replace("if ($release.draft -ne $true)", "if ($false)", 1), workflow),
    "exhaustive release owner check": (helper.replace("Assert-NoReleaseOwnsTag\n\n$resolvedAfter", "$resolvedAfter", 1), workflow),
    "exact-SHA tag ownership": (helper.replace("if (-not [string]::Equals($resolvedBefore, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))", "if ($false)", 1), workflow),
    "post-delete exact-SHA recheck": (helper.replace("if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))", "if ($false)", 1), workflow),
    "transaction rollback wiring": (helper, workflow.replace("& .\\scripts\\rollback-v26-draft-release.ps1", "# rollback removed", 1)),
}
for label, (mutated_helper, mutated_workflow) in mutations.items():
    if not validate(mutated_helper, mutated_workflow):
        raise SystemExit(f"V26 draft rollback mutation probe did not fail closed: {label}")

print("PASS V26 draft release rollback contract")
