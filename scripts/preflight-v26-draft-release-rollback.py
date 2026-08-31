#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper = (root / "scripts" / "rollback-v26-draft-release.ps1").read_text(encoding="utf-8")
workflow = (root / ".github" / "workflows" / "release-v26.yml").read_text(encoding="utf-8")


def require_all(text: str, tokens: list[str], label: str, errors: list[str]) -> None:
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing fail-closed contract: {token}")


def validate(helper_text: str, workflow_text: str) -> list[str]:
    errors: list[str] = []

    helper_contract = [
        "[Parameter(Mandatory = $true)][bool]$TagCreatedByThisRun",
        "if ($ReleaseId -lt 0)",
        "if ($ReleaseId -gt 0)",
        "if ($release.draft -ne $true)",
        "Resolve-ExactRemoteTagSha",
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
    require_all(helper_text, helper_contract, "helper", errors)

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
    ordered_helper = [release_delete, release_reconcile, owner_check, preserve, sha_recheck, tag_delete, tag_reconcile]
    if min(ordered_helper) < 0 or ordered_helper != sorted(ordered_helper):
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
    require_all(workflow_text, workflow_contract, "workflow", errors)

    draft_create_workflow_contract = [
        "$draftTransactionMarker =",
        "QS3D-DRAFT-CREATE-V26:",
        "$expectedReleaseName = \"QS3D for BricsCAD V26 $env:RELEASE_TAG\"",
        "$draftCreateError = $_",
        "$reconciledDraft = Resolve-AmbiguousDraftCreate",
        "draft-create acknowledgement was ambiguous, but exactly one transaction-owned draft was recovered",
        "$release = $reconciledDraft",
    ]
    require_all(workflow_text, draft_create_workflow_contract, "workflow draft-create acknowledgement", errors)

    draft_create_start = workflow_text.find("function Resolve-AmbiguousDraftCreate")
    draft_create_end = workflow_text.find("function Assert-PublishedReleaseMatchesVerifiedTransaction", draft_create_start + 1)
    if draft_create_start < 0 or draft_create_end <= draft_create_start:
        errors.append("workflow draft-create acknowledgement missing bounded Resolve-AmbiguousDraftCreate function scope")
        draft_create_scope = ""
    else:
        draft_create_scope = workflow_text[draft_create_start:draft_create_end]
    draft_create_function_contract = [
        "function Resolve-AmbiguousDraftCreate",
        "releases?per_page=100&page=$page",
        "$maxPages = 20",
        "ReleaseSnapshot.draft -ne $true",
        "ReleaseSnapshot.tag_name, $env:RELEASE_TAG",
        "ReleaseSnapshot.target_commitish, $env:GITHUB_SHA",
        "ReleaseSnapshot.name, $ExpectedReleaseName",
        "ReleaseSnapshot.prerelease -ne $IsPrerelease",
        "ReleaseSnapshot.body",
        "$body.IndexOf($TransactionMarker, [StringComparison]::Ordinal)",
        "matching V26 draft-create transaction marker",
    ]
    require_all(draft_create_scope, draft_create_function_contract, "workflow Resolve-AmbiguousDraftCreate", errors)

    for stale in ["$preCreateTagLines", "$tagWasAbsentBeforeCreate", "$releaseCreatedByThisRun", "-TagWasAbsentBeforeCreate", "if (git tag --list $env:RELEASE_TAG) { throw"]:
        if stale in workflow_text:
            errors.append(f"workflow retains stale reject/absence-based ownership contract: {stale}")

    if workflow_text.count("$tagReadyForRelease = $true") < 2:
        errors.append("workflow must admit both acknowledged/reusable exact-tag paths before publication")

    marker = workflow_text.find("$draftTransactionMarker =")
    request = workflow_text.find("$request = @{", marker + 1)
    create = workflow_text.find("Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"", request + 1)
    create_error = workflow_text.find("$draftCreateError = $_", create + 1)
    reconcile = workflow_text.find("$reconciledDraft = Resolve-AmbiguousDraftCreate", create_error + 1)
    recovered = workflow_text.find("$release = $reconciledDraft", reconcile + 1)
    release_id = workflow_text.find("$releaseId = [long]$release.id", recovered + 1)
    draft_order = [marker, request, create, create_error, reconcile, recovered, release_id]
    if min(draft_order) < 0 or draft_order != sorted(draft_order):
        errors.append("V26 draft-create order must be marker -> request -> one POST -> catch -> bounded reconciliation -> recovered identity -> releaseId assignment")

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
    "ambiguous tag-create reconciliation": (helper, workflow.replace("$reconciledTag = Get-ExactReusableReleaseTag", "$reconciledTag = $null", 1)),
    "draft transaction marker": (helper, workflow.replace("QS3D-DRAFT-CREATE-V26:", "QS3D-DRAFT-CREATE-REMOVED:", 1)),
    "draft reconciliation": (helper, workflow.replace("$reconciledDraft = Resolve-AmbiguousDraftCreate", "$reconciledDraft = $null", 1)),
    "draft exact tag": (helper, workflow.replace("ReleaseSnapshot.tag_name, $env:RELEASE_TAG", "ReleaseSnapshot.tag_name, 'other'", 1)),
    "draft exact SHA": (helper, workflow.replace("ReleaseSnapshot.target_commitish, $env:GITHUB_SHA", "ReleaseSnapshot.target_commitish, ('0' * 40)", 1)),
    "draft exact name": (helper, workflow.replace("ReleaseSnapshot.name, $ExpectedReleaseName", "ReleaseSnapshot.name, 'other'", 1)),
    "draft prerelease": (helper, workflow.replace("ReleaseSnapshot.prerelease -ne $IsPrerelease", "$false", 1)),
    "draft marker identity": (helper, workflow.replace("$body.IndexOf($TransactionMarker, [StringComparison]::Ordinal)", "$false", 1)),
    "bounded enumeration": (helper, workflow.replace("$maxPages = 20", "$maxPages = [int]::MaxValue", 1)),
    "draft delete reconciliation": (helper.replace("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", "# removed", 1), workflow),
    "release-owner scan": (helper.replace("\nAssert-NoReleaseOwnsTag\n", "\n# removed\n", 1), workflow),
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

print("PASS V26 restart-safe tag admission, draft-create acknowledgement recovery, publish reconciliation, rollback, and non-owned tag preservation contract")