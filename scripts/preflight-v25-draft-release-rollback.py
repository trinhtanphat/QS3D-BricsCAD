#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper_path = root / "scripts" / "rollback-v25-draft-release.ps1"
workflow_path = root / ".github" / "workflows" / "release-v25.yml"
helper = helper_path.read_text(encoding="utf-8")
workflow = workflow_path.read_text(encoding="utf-8")


def require_all(text: str, tokens: list[str], label: str, errors: list[str]) -> None:
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing fail-closed contract: {token}")


def validate(helper_text: str, workflow_text: str) -> list[str]:
    errors: list[str] = []

    required_helper = [
        "[Parameter(Mandatory = $true)][bool]$TagCreatedByThisRun",
        "if (-not $TagCreatedByThisRun)",
        "if ($ReleaseId -lt 0)",
        "if ($ReleaseId -gt 0)",
        "Resolve-ExactRemoteTagSha",
        "git ls-remote --tags origin $tagRef $peeledRef",
        "if ($release.draft -ne $true)",
        "function Test-GitHubNotFound",
        "[int]$response.StatusCode -eq 404",
        "function Assert-DraftDeleteCommittedAfterError",
        "$remainingRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers",
        "Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri",
        "function Assert-NoReleaseOwnsTag",
        "releases?per_page=100&page=$page",
        "A release still owns tag $ReleaseTag; refusing tag deletion.",
        "Preserving exact V25 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.",
        "TagDeleted = $false",
        "Remote tag $ReleaseTag changed during rollback; refusing tag deletion.",
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
        "function Assert-TagDeleteCommittedAfterError",
        "$remainingTag = Invoke-RestMethod -Method Get -Uri $TagGetUri -Headers $headers",
        "Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri",
    ]
    require_all(helper_text, required_helper, "helper", errors)

    for forbidden in ["TagWasAbsentBeforeCreate", "releases/tags/", "git push --delete", "git push origin :refs/tags/", "-Force"]:
        if forbidden in helper_text:
            errors.append(f"helper uses stale/broad destructive contract: {forbidden}")

    release_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $releaseUri")
    release_reconcile = helper_text.find("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", release_delete + 1)
    owner_check = helper_text.find("Assert-NoReleaseOwnsTag", release_reconcile + 1)
    non_owned = helper_text.find("if (-not $TagCreatedByThisRun)", owner_check + 1)
    preserve = helper_text.find("Preserving exact V25 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.", non_owned + 1)
    post_sha_check = helper_text.find("Remote tag $ReleaseTag changed during rollback; refusing tag deletion.", preserve + 1)
    tag_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $tagRefUri", post_sha_check + 1)
    tag_reconcile = helper_text.find("Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri", tag_delete + 1)
    helper_order = [release_delete, release_reconcile, owner_check, non_owned, preserve, post_sha_check, tag_delete, tag_reconcile]
    if min(helper_order) < 0 or helper_order != sorted(helper_order):
        errors.append("helper order must remain draft delete -> acknowledgement reconciliation -> exhaustive release-owner scan -> non-owned tag preservation -> owned exact-SHA recheck -> tag delete -> acknowledgement reconciliation")

    required_workflow = [
        '$tagRef = "refs/tags/$env:RELEASE_TAG"',
        "function Test-GitHubNotFound",
        "function Get-ExactReusableReleaseTag",
        "snapshot.ref, $tagRef",
        "snapshot.object.type, 'commit'",
        "snapshot.object.sha, $env:GITHUB_SHA",
        "$existingTag = Get-ExactReusableReleaseTag",
        "Reusing exact V25 lightweight tag $env:RELEASE_TAG at workflow SHA without claiming deletion ownership.",
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
        "$verifiedAssetIds[$name] = $uploadedAssetId",
        "$publishPatchAttempted = $false",
        "$publishPatchAttempted = $true",
        "function Assert-PublishedReleaseMatchesVerifiedTransaction",
        "$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        "if ($reconciledRelease.draft -eq $false)",
        "if (-not $publishPatchAttempted)",
        "Assert-PublishedReleaseMatchesVerifiedTransaction",
        "authoritative release state confirms the exact qualified release is already published; treating publication as committed.",
        "publication acknowledgement reconciliation failed",
        "Manual cleanup is required before retry.",
        "& .\\scripts\\rollback-v25-draft-release.ps1",
        "-TagCreatedByThisRun $tagCreatedByThisRun",
        "Automatic V25 draft rollback failed",
        "publication failed after exact release-tag admission",
        "Automatic rollback completed; retry with the same tag is safe.",
    ]
    require_all(workflow_text, required_workflow, "workflow", errors)

    draft_create_start = workflow_text.find("function Resolve-AmbiguousDraftCreate")
    publish_assert_start = workflow_text.find("function Assert-PublishedReleaseMatchesVerifiedTransaction", draft_create_start + 1)
    if draft_create_start < 0 or publish_assert_start <= draft_create_start:
        errors.append("workflow draft-create acknowledgement missing bounded Resolve-AmbiguousDraftCreate function scope")
        draft_create_scope = ""
    else:
        draft_create_scope = workflow_text[draft_create_start:publish_assert_start]

    draft_create_contract = [
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
        "matching V25 draft-create transaction marker",
    ]
    require_all(draft_create_scope, draft_create_contract, "workflow Resolve-AmbiguousDraftCreate", errors)

    publish_assert_end = workflow_text.find("$notes =", publish_assert_start + 1)
    if publish_assert_start < 0 or publish_assert_end <= publish_assert_start:
        errors.append("workflow publish acknowledgement missing Assert-PublishedReleaseMatchesVerifiedTransaction function scope")
        publish_assert_scope = ""
    else:
        publish_assert_scope = workflow_text[publish_assert_start:publish_assert_end]

    publish_identity_contract = [
        "function Assert-PublishedReleaseMatchesVerifiedTransaction",
        "[long]$ReleaseSnapshot.id -ne $ReleaseId",
        "ReleaseSnapshot.url, $ReleaseUri",
        "ReleaseSnapshot.draft -ne $false",
        "ReleaseSnapshot.tag_name, $env:RELEASE_TAG",
        "ReleaseSnapshot.target_commitish, $env:GITHUB_SHA",
        "ReleaseSnapshot.name, $ExpectedReleaseName",
        "ReleaseSnapshot.prerelease -ne $IsPrerelease",
        "ReleaseSnapshot.body",
        "$TransactionMarker",
        "Assert-RemoteReleaseTagTargetsWorkflowSha",
        "@($ReleaseSnapshot.assets).Count -ne $ExpectedAssets.Count",
        "$VerifiedAssetIds.Count -ne $ExpectedAssets.Count",
        "$VerifiedAssetIds.ContainsKey($expectedAsset)",
        "$LocalAssets.ContainsKey($expectedAsset)",
        "[long]$publishedAsset.id -ne $expectedAssetId",
        "[int64]$publishedAsset.size -ne $localLength",
    ]
    require_all(publish_assert_scope, publish_identity_contract, "workflow published-release acknowledgement", errors)

    for stale in ["$existing = @(git ls-remote --tags origin $tagRef", "& gh @createArgs", "if (git tag --list $env:RELEASE_TAG) { throw"]:
        if stale in workflow_text:
            errors.append(f"workflow retains stale reject/implicit-tag creation contract: {stale}")

    reusable_fn = workflow_text.find("function Get-ExactReusableReleaseTag")
    existing_lookup = workflow_text.find("$existingTag = Get-ExactReusableReleaseTag", reusable_fn + 1)
    tag_create = workflow_text.find("$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri", existing_lookup + 1)
    tag_owned = workflow_text.find("$tagCreatedByThisRun = $true", tag_create + 1)
    tag_create_error = workflow_text.find("$tagCreateError = $_", tag_owned + 1)
    reconcile_tag = workflow_text.find("$reconciledTag = Get-ExactReusableReleaseTag", tag_create_error + 1)
    tag_order = [reusable_fn, existing_lookup, tag_create, tag_owned, tag_create_error, reconcile_tag]
    if min(tag_order) < 0 or tag_order != sorted(tag_order):
        errors.append("V25 tag admission order must remain reusable lookup -> exact create -> positive ownership -> ambiguous create reconciliation")

    marker = workflow_text.find("$draftTransactionMarker =")
    request = workflow_text.find("$releaseRequest = @{", marker + 1)
    release_create = workflow_text.find('$release = Invoke-RestMethod -Method Post', request + 1)
    draft_error = workflow_text.find("$draftCreateError = $_", release_create + 1)
    draft_reconcile = workflow_text.find("$reconciledDraft = Resolve-AmbiguousDraftCreate", draft_error + 1)
    recovered = workflow_text.find("$release = $reconciledDraft", draft_reconcile + 1)
    release_id = workflow_text.find("$releaseId = [long]$release.id", recovered + 1)
    publish_attempt = workflow_text.find("$publishPatchAttempted = $true", release_id + 1)
    publish_patch = workflow_text.find("Invoke-RestMethod -Method Patch -Uri $releaseUri", publish_attempt + 1)
    catch_block = workflow_text.find("$publicationError = $_", publish_patch + 1)
    publish_get = workflow_text.find("$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", catch_block + 1)
    publish_match = workflow_text.find("Assert-PublishedReleaseMatchesVerifiedTransaction", publish_get + 1)
    rollback_call = workflow_text.find("rollback-v25-draft-release.ps1", publish_match + 1)
    transaction_order = [marker, request, release_create, draft_error, draft_reconcile, recovered, release_id, publish_attempt, publish_patch, catch_block, publish_get, publish_match, rollback_call]
    if min(transaction_order) < 0 or transaction_order != sorted(transaction_order) or release_create <= reconcile_tag:
        errors.append("V25 release transaction order must remain marker/request -> exact-tag admission -> one draft POST -> create-error reconciliation -> positive releaseId -> publish-attempt proof -> PATCH -> authoritative GET/match -> bounded rollback")

    return errors


canonical_errors = validate(helper, workflow)
if canonical_errors:
    raise SystemExit("V25 draft rollback contract failed: " + "; ".join(canonical_errors))

mutations = {
    "reusable tag lookup": (helper, workflow.replace("$existingTag = Get-ExactReusableReleaseTag", "$existingTag = $null", 1)),
    "tag-ready proof": (helper, workflow.replace("$tagReadyForRelease = $true", "$tagReadyForRelease = $false")),
    "positive tag ownership": (helper, workflow.replace("$tagCreatedByThisRun = $true", "$tagCreatedByThisRun = $false", 1)),
    "created-ref identity": (helper, workflow.replace("createdTag.ref, $tagRef", "createdTag.ref, 'refs/tags/other'", 1)),
    "created-ref type": (helper, workflow.replace("createdTag.object.type, 'commit'", "createdTag.object.type, 'tag'", 1)),
    "created-ref SHA": (helper, workflow.replace("createdTag.object.sha, $env:GITHUB_SHA", "createdTag.object.sha, ('0' * 40)", 1)),
    "ambiguous tag-create reconciliation": (helper, workflow.replace("$reconciledTag = Get-ExactReusableReleaseTag", "$reconciledTag = $null", 1)),
    "draft transaction marker": (helper, workflow.replace("QS3D-DRAFT-CREATE-V25:", "QS3D-DRAFT-CREATE-REMOVED:", 1)),
    "draft reconciliation": (helper, workflow.replace("$reconciledDraft = Resolve-AmbiguousDraftCreate", "$reconciledDraft = $null", 1)),
    "draft exact tag": (helper, workflow.replace("ReleaseSnapshot.tag_name, $env:RELEASE_TAG", "ReleaseSnapshot.tag_name, 'other'", 1)),
    "draft exact SHA": (helper, workflow.replace("ReleaseSnapshot.target_commitish, $env:GITHUB_SHA", "ReleaseSnapshot.target_commitish, ('0' * 40)", 1)),
    "draft exact name": (helper, workflow.replace("ReleaseSnapshot.name, $ExpectedReleaseName", "ReleaseSnapshot.name, 'other'", 1)),
    "draft prerelease": (helper, workflow.replace("ReleaseSnapshot.prerelease -ne $IsPrerelease", "$false", 1)),
    "draft marker identity": (helper, workflow.replace("$body.IndexOf($TransactionMarker, [StringComparison]::Ordinal)", "$false", 1)),
    "bounded enumeration": (helper, workflow.replace("$maxPages = 20", "$maxPages = [int]::MaxValue", 1)),
    "draft-only delete": (helper.replace("if ($release.draft -ne $true)", "if ($false)", 1), workflow),
    "draft delete acknowledgement reconciliation": (helper.replace("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", "# removed", 1), workflow),
    "release-owner scan": (helper.replace("\nAssert-NoReleaseOwnsTag\n", "\n# removed\n", 1), workflow),
    "non-owned tag preservation": (helper.replace("if (-not $TagCreatedByThisRun)", "if ($false)", 1), workflow),
    "tag delete acknowledgement reconciliation": (helper.replace("Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri", "# removed", 1), workflow),
    "verified asset identity": (helper, workflow.replace("$verifiedAssetIds[$name] = $uploadedAssetId", "# removed", 1)),
    "publish attempt proof": (helper, workflow.replace("$publishPatchAttempted = $true", "$publishPatchAttempted = $false", 1)),
    "published reconciliation GET": (helper, workflow.replace("$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", "$reconciledRelease = $null", 1)),
    "published release id": (helper, workflow.replace("[long]$ReleaseSnapshot.id -ne $ReleaseId", "$false", 1)),
    "published repository identity": (helper, workflow.replace("ReleaseSnapshot.url, $ReleaseUri", "ReleaseSnapshot.url, 'other'", 1)),
    "published exact tag": (helper, workflow.replace("ReleaseSnapshot.tag_name, $env:RELEASE_TAG", "ReleaseSnapshot.tag_name, 'other'", 2)),
    "published exact SHA": (helper, workflow.replace("ReleaseSnapshot.target_commitish, $env:GITHUB_SHA", "ReleaseSnapshot.target_commitish, ('0' * 40)", 2)),
    "published exact name": (helper, workflow.replace("ReleaseSnapshot.name, $ExpectedReleaseName", "ReleaseSnapshot.name, 'other'", 2)),
    "published prerelease": (helper, workflow.replace("ReleaseSnapshot.prerelease -ne $IsPrerelease", "$false", 2)),
    "published remote tag proof": (helper, workflow.replace("Assert-RemoteReleaseTagTargetsWorkflowSha", "# removed", 1)),
    "published asset count": (helper, workflow.replace("@($ReleaseSnapshot.assets).Count -ne $ExpectedAssets.Count", "$false", 1)),
    "published asset id": (helper, workflow.replace("[long]$publishedAsset.id -ne $expectedAssetId", "$false", 1)),
    "published asset bytes": (helper, workflow.replace("[int64]$publishedAsset.size -ne $localLength", "$false", 1)),
    "pre-PATCH publication veto": (helper, workflow.replace("if (-not $publishPatchAttempted)", "if ($false)", 1)),
    "rollback wiring": (helper, workflow.replace("& .\\scripts\\rollback-v25-draft-release.ps1", "# removed", 1)),
}
for label, (mutated_helper, mutated_workflow) in mutations.items():
    if not validate(mutated_helper, mutated_workflow):
        raise SystemExit(f"V25 draft rollback mutation probe did not fail closed: {label}")

print("PASS V25 restart-safe tag admission, draft-create acknowledgement recovery, publish acknowledgement reconciliation, rollback, non-owned tag preservation, and destructive acknowledgement contract")