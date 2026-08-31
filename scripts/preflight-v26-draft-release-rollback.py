#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper = (root / "scripts" / "rollback-v26-draft-release.ps1").read_text(encoding="utf-8")
workflow = (root / ".github" / "workflows" / "release-v26.yml").read_text(encoding="utf-8")


def validate(helper_text: str, workflow_text: str) -> list[str]:
    errors: list[str] = []

    helper_contract = [
        "[Parameter(Mandatory = $true)][bool]$TagCreatedByThisRun",
        "if ($ReleaseId -lt 0)",
        "if ($ReleaseId -gt 0)",
        "if ($release.draft -ne $true)",
        "Resolve-ExactRemoteTagSha",
        "git ls-remote --tags origin $tagRef $peeledRef",
        "function Test-GitHubNotFound",
        "[int]$response.StatusCode -eq 404",
        "function Assert-DraftDeleteCommittedAfterError",
        "$remainingRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers",
        "Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri",
        "function Assert-NoReleaseOwnsTag",
        "releases?per_page=100&page=$page",
        "A release still owns tag $ReleaseTag; refusing tag deletion.",
        "if (-not $TagCreatedByThisRun)",
        "Preserving exact V26 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.",
        "TagDeleted = $false",
        "Remote tag $ReleaseTag changed during rollback; refusing tag deletion.",
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
        "function Assert-TagDeleteCommittedAfterError",
        "$remainingTag = Invoke-RestMethod -Method Get -Uri $TagGetUri -Headers $headers",
        "Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri",
    ]
    for token in helper_contract:
        if token not in helper_text:
            errors.append(f"helper missing fail-closed contract: {token}")

    for forbidden in ["TagWasAbsentBeforeCreate", "releases/tags/", "git push --delete", "git push origin :refs/tags/", "-Force"]:
        if forbidden in helper_text:
            errors.append(f"helper uses stale/broad destructive contract: {forbidden}")

    release_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $releaseUri")
    release_reconcile = helper_text.find("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", release_delete + 1)
    owner_check = helper_text.find("Assert-NoReleaseOwnsTag", release_reconcile + 1)
    preserve = helper_text.find("Preserving exact V26 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.", owner_check + 1)
    sha_recheck = helper_text.find("Remote tag $ReleaseTag changed during rollback; refusing tag deletion.", preserve + 1)
    tag_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $tagRefUri", sha_recheck + 1)
    tag_reconcile = helper_text.find("Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri", tag_delete + 1)
    if min(release_delete, release_reconcile, owner_check, preserve, sha_recheck, tag_delete, tag_reconcile) < 0 or not (
        release_delete < release_reconcile < owner_check < preserve < sha_recheck < tag_delete < tag_reconcile
    ):
        errors.append("rollback order must remain draft-delete/reconcile -> release-owner scan -> non-owned preservation -> exact-SHA recheck -> tag-delete/reconcile")

    workflow_contract = [
        '$tagRef = "refs/tags/$env:RELEASE_TAG"',
        "function Test-GitHubNotFound",
        "function Get-ExactReusableReleaseTag",
        "snapshot.ref, $tagRef",
        "snapshot.object.type, 'commit'",
        "snapshot.object.sha, $env:GITHUB_SHA",
        "$existingTag = Get-ExactReusableReleaseTag",
        "Reusing exact V26 lightweight tag $env:RELEASE_TAG at workflow SHA without claiming deletion ownership.",
        "$tagReadyForRelease = $false",
        "$tagReadyForRelease = $true",
        "$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri",
        "createdTag.ref, $tagRef",
        "createdTag.object.type, 'commit'",
        "createdTag.object.sha, $env:GITHUB_SHA",
        "$tagCreatedByThisRun = $false",
        "$tagCreatedByThisRun = $true",
        "$tagCreateError = $_",
        "$reconciledTag = Get-ExactReusableReleaseTag",
        "tag-create acknowledgement failed and the exact release tag is authoritatively absent",
        "tag-create acknowledgement was ambiguous, but the exact lightweight tag now exists at workflow SHA; reusing it without deletion ownership.",
        "$releaseId = [long]0",
        "$releaseId = [long]$release.id",
        "$verifiedAssetIds = @{}",
        "$verifiedAssetIds[$expectedAsset] = $uploadedAssetId",
        "$publishPatchAttempted = $false",
        "$publishPatchAttempted = $true",
        "function Assert-PublishedReleaseMatchesVerifiedTransaction",
        "$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        "if ($reconciledRelease.draft -eq $false)",
        "if (-not $publishPatchAttempted)",
        "Assert-PublishedReleaseMatchesVerifiedTransaction",
        "authoritative release state confirms the exact qualified release is already published; treating publication as committed.",
        "if (-not $tagReadyForRelease)",
        "rollback-v26-draft-release.ps1",
        "-TagCreatedByThisRun $tagCreatedByThisRun",
        "Automatic V26 draft rollback failed",
        "Automatic rollback completed; retry with the same tag is safe.",
    ]
    for token in workflow_contract:
        if token not in workflow_text:
            errors.append(f"workflow missing restart-safe tag/acknowledgement contract: {token}")

    for stale in ["$preCreateTagLines", "$tagWasAbsentBeforeCreate", "$releaseCreatedByThisRun", "-TagWasAbsentBeforeCreate", "if (git tag --list $env:RELEASE_TAG) { throw"]:
        if stale in workflow_text:
            errors.append(f"workflow retains stale reject/absence-based ownership contract: {stale}")

    if workflow_text.count("$tagReadyForRelease = $true") < 2:
        errors.append("workflow must admit both acknowledged/reusable exact-tag paths before publication")

    reusable = workflow_text.find("$existingTag = Get-ExactReusableReleaseTag")
    create = workflow_text.find("$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri", reusable + 1)
    owned = workflow_text.find("$tagCreatedByThisRun = $true", create + 1)
    create_error = workflow_text.find("$tagCreateError = $_", owned + 1)
    reconcile = workflow_text.find("$reconciledTag = Get-ExactReusableReleaseTag", create_error + 1)
    ambiguous = workflow_text.find("tag-create acknowledgement was ambiguous, but the exact lightweight tag now exists at workflow SHA; reusing it without deletion ownership.", reconcile + 1)
    ambiguous_non_owned = workflow_text.find("$tagCreatedByThisRun = $false", ambiguous + 1)
    ambiguous_ready = workflow_text.find("$tagReadyForRelease = $true", ambiguous_non_owned + 1)
    release_create = workflow_text.find("$release = Invoke-RestMethod -Method Post", ambiguous_ready + 1)
    asset_verify = workflow_text.find("$verifiedAssetIds[$expectedAsset] = $uploadedAssetId", release_create + 1)
    patch_proof = workflow_text.find("$publishPatchAttempted = $true", asset_verify + 1)
    publish = workflow_text.find("$published = Invoke-RestMethod -Method Patch", patch_proof + 1)
    publication_error = workflow_text.find("$publicationError = $_", publish + 1)
    ready_check = workflow_text.find("if (-not $tagReadyForRelease)", publication_error + 1)
    published_reconcile = workflow_text.find("$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", ready_check + 1)
    rollback = workflow_text.find("rollback-v26-draft-release.ps1", published_reconcile + 1)
    if min(reusable, create, owned, create_error, reconcile, ambiguous, ambiguous_non_owned, ambiguous_ready, release_create, asset_verify, patch_proof, publish, publication_error, ready_check, published_reconcile, rollback) < 0 or not (
        reusable < create < owned < create_error < reconcile < ambiguous < ambiguous_non_owned < ambiguous_ready < release_create < asset_verify < patch_proof < publish < publication_error < ready_check < published_reconcile < rollback
    ):
        errors.append("workflow order must remain exact-tag admission -> acknowledged ownership -> ambiguous-create reconciliation/non-ownership -> draft/assets -> publish acknowledgement reconciliation -> bounded rollback")

    return errors


canonical_errors = validate(helper, workflow)
if canonical_errors:
    raise SystemExit("V26 draft rollback contract failed: " + "; ".join(canonical_errors))

mutations = {
    "reusable lookup": (helper, workflow.replace("$existingTag = Get-ExactReusableReleaseTag", "$existingTag = $null", 1)),
    "tag-ready proof": (helper, workflow.replace("$tagReadyForRelease = $true", "$tagReadyForRelease = $false")),
    "positive ownership": (helper, workflow.replace("$tagCreatedByThisRun = $true", "$tagCreatedByThisRun = $false", 1)),
    "created ref": (helper, workflow.replace("createdTag.ref, $tagRef", "createdTag.ref, 'refs/tags/other'", 1)),
    "created type": (helper, workflow.replace("createdTag.object.type, 'commit'", "createdTag.object.type, 'tag'", 1)),
    "created SHA": (helper, workflow.replace("createdTag.object.sha, $env:GITHUB_SHA", "createdTag.object.sha, ('0' * 40)", 1)),
    "ambiguous create reconciliation": (helper, workflow.replace("$reconciledTag = Get-ExactReusableReleaseTag", "$reconciledTag = $null", 1)),
    "ambiguous create non-ownership": (helper, workflow.replace("$tagCreatedByThisRun = $false\n                $tagReadyForRelease = $true", "$tagCreatedByThisRun = $true\n                $tagReadyForRelease = $true", 1)),
    "draft delete reconciliation": (helper.replace("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", "# removed", 1), workflow),
    "release-owner scan": (helper.replace("Assert-NoReleaseOwnsTag", "# removed", 1), workflow),
    "non-owned preservation": (helper.replace("if (-not $TagCreatedByThisRun)", "if ($false)", 1), workflow),
    "tag delete reconciliation": (helper.replace("Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri", "# removed", 1), workflow),
    "asset identity": (helper, workflow.replace("$verifiedAssetIds[$expectedAsset] = $uploadedAssetId", "# removed", 1)),
    "publish attempt proof": (helper, workflow.replace("$publishPatchAttempted = $true", "$publishPatchAttempted = $false", 1)),
    "published reconciliation": (helper, workflow.replace("$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", "$reconciledRelease = $null", 1)),
    "rollback wiring": (helper, workflow.replace("& .\\scripts\\rollback-v26-draft-release.ps1", "# removed", 1)),
}
for label, (mutated_helper, mutated_workflow) in mutations.items():
    if not validate(mutated_helper, mutated_workflow):
        raise SystemExit(f"V26 draft rollback mutation probe did not fail closed: {label}")

print("PASS V26 restart-safe tag admission, publish reconciliation, draft rollback, and non-owned tag preservation contract")
