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
        "if ($ReleaseId -lt 0)",
        "if ($ReleaseId -gt 0)",
        "Resolve-ExactRemoteTagSha",
        "git ls-remote --tags origin $tagRef $peeledRef",
        "if ($release.draft -ne $true)",
        "release.url, $releaseUri",
        "release.tag_name, $ReleaseTag",
        "function Test-GitHubNotFound",
        "[int]$response.StatusCode -eq 404",
        "function Assert-DraftDeleteCommittedAfterError",
        "$remainingRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers",
        "Invoke-RestMethod -Method Delete -Uri $releaseUri",
        "Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri",
        "function Assert-NoReleaseOwnsTag",
        "releases?per_page=100&page=$page",
        "A release still owns tag $ReleaseTag; refusing rollback completion.",
        "$resolvedPreserved = Resolve-ExactRemoteTagSha",
        "V25 release tag $ReleaseTag changed during draft rollback",
        "Preserving exact V25 tag $ReleaseTag",
        "TagDeleted = $false",
    ]
    require_all(helper_text, required_helper, "helper", errors)

    forbidden_helper = [
        "TagWasAbsentBeforeCreate",
        "releases/tags/",
        "git push --delete",
        "git push origin :refs/tags/",
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
        "function Assert-TagDeleteCommittedAfterError",
        "$remainingTag = Invoke-RestMethod -Method Get -Uri $TagGetUri -Headers $headers",
        "TagDeleted = $true",
        "/git/refs/tags/",
        "/git/ref/tags/",
    ]
    for token in forbidden_helper:
        if token in helper_text:
            errors.append(f"helper retains destructive/stale tag contract: {token}")

    release_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $releaseUri")
    release_reconcile = helper_text.find(
        "Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri",
        release_delete + 1,
    )
    owner_check = helper_text.find("Assert-NoReleaseOwnsTag", release_reconcile + 1)
    preserved_resolution = helper_text.find("$resolvedPreserved = Resolve-ExactRemoteTagSha", owner_check + 1)
    preserved_sha_gate = helper_text.find(
        "V25 release tag $ReleaseTag changed during draft rollback", preserved_resolution + 1
    )
    preserve = helper_text.find("Preserving exact V25 tag $ReleaseTag", preserved_sha_gate + 1)
    result = helper_text.find("TagDeleted = $false", preserve + 1)
    helper_order = [
        release_delete,
        release_reconcile,
        owner_check,
        preserved_resolution,
        preserved_sha_gate,
        preserve,
        result,
    ]
    if min(helper_order) < 0 or helper_order != sorted(helper_order):
        errors.append(
            "helper order must remain draft delete -> acknowledgement reconciliation -> "
            "exhaustive release-owner scan -> exact-tag re-resolution -> exact-SHA gate -> "
            "tag preservation -> non-destructive result"
        )

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
        "QS3D-DRAFT-CREATE-V25:",
        "function Resolve-AmbiguousDraftCreate",
        "releases?per_page=100&page=$page",
        "$maxPages = 20",
        "ReleaseSnapshot.draft -ne $true",
        "ReleaseSnapshot.tag_name, $env:RELEASE_TAG",
        "ReleaseSnapshot.target_commitish, $env:GITHUB_SHA",
        "ReleaseSnapshot.name, $ExpectedReleaseName",
        "ReleaseSnapshot.prerelease -ne $IsPrerelease",
        "$body.IndexOf($TransactionMarker, [StringComparison]::Ordinal)",
        "$releaseId = [long]0",
        "$releaseId = [long]$release.id",
        "$verifiedAssetIds = @{}",
        "$verifiedAssetIds[$name] = $uploadedAssetId",
        "$publishPatchAttempted = $false",
        "$publishPatchAttempted = $true",
        "function Assert-PublishedReleaseMatchesVerifiedTransaction",
        "[long]$ReleaseSnapshot.id -ne $ReleaseId",
        "ReleaseSnapshot.url, $ReleaseUri",
        "ReleaseSnapshot.draft -ne $false",
        "ReleaseSnapshot.tag_name, $env:RELEASE_TAG",
        "ReleaseSnapshot.target_commitish, $env:GITHUB_SHA",
        "@($ReleaseSnapshot.assets).Count -ne $ExpectedAssets.Count",
        "$VerifiedAssetIds.Count -ne $ExpectedAssets.Count",
        "[long]$publishedAsset.id -ne $expectedAssetId",
        "[int64]$publishedAsset.size -ne $localLength",
        "$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        "if ($reconciledRelease.draft -eq $false)",
        "if (-not $publishPatchAttempted)",
        "authoritative release state confirms the exact qualified release is already published; treating publication as committed.",
        "publication acknowledgement reconciliation failed",
        "Manual cleanup is required before retry.",
        "& .\\scripts\\rollback-v25-draft-release.ps1",
        "-TagCreatedByThisRun $tagCreatedByThisRun",
        "Automatic V25 draft rollback failed",
        "Automatic rollback completed; retry with the same tag is safe.",
    ]
    require_all(workflow_text, required_workflow, "workflow", errors)

    for stale in [
        "$existing = @(git ls-remote --tags origin $tagRef",
        "& gh @createArgs",
        "if (git tag --list $env:RELEASE_TAG) { throw",
        "TagDeleted",
    ]:
        if stale in workflow_text:
            errors.append(f"workflow retains stale release contract: {stale}")

    reusable_fn = workflow_text.find("function Get-ExactReusableReleaseTag")
    existing_lookup = workflow_text.find("$existingTag = Get-ExactReusableReleaseTag", reusable_fn + 1)
    tag_create = workflow_text.find(
        "$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri", existing_lookup + 1
    )
    tag_owned = workflow_text.find("$tagCreatedByThisRun = $true", tag_create + 1)
    tag_create_error = workflow_text.find("$tagCreateError = $_", tag_owned + 1)
    reconcile_tag = workflow_text.find("$reconciledTag = Get-ExactReusableReleaseTag", tag_create_error + 1)
    tag_order = [reusable_fn, existing_lookup, tag_create, tag_owned, tag_create_error, reconcile_tag]
    if min(tag_order) < 0 or tag_order != sorted(tag_order):
        errors.append(
            "V25 tag admission order must remain reusable lookup -> exact create -> "
            "positive ownership -> ambiguous create reconciliation"
        )

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
    publish_get = workflow_text.find(
        "$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        catch_block + 1,
    )
    publish_match = workflow_text.find("Assert-PublishedReleaseMatchesVerifiedTransaction", publish_get + 1)
    rollback_call = workflow_text.find("rollback-v25-draft-release.ps1", publish_match + 1)
    transaction_order = [
        marker,
        request,
        release_create,
        draft_error,
        draft_reconcile,
        recovered,
        release_id,
        publish_attempt,
        publish_patch,
        catch_block,
        publish_get,
        publish_match,
        rollback_call,
    ]
    if min(transaction_order) < 0 or transaction_order != sorted(transaction_order) or release_create <= reconcile_tag:
        errors.append(
            "V25 release transaction order must remain marker/request -> exact-tag admission -> "
            "one draft POST -> create-error reconciliation -> positive releaseId -> publish-attempt "
            "proof -> PATCH -> authoritative GET/match -> bounded rollback"
        )

    return errors


canonical_errors = validate(helper, workflow)
if canonical_errors:
    raise SystemExit("V25 draft rollback contract failed: " + "; ".join(canonical_errors))

helper_mutations = {
    "draft-only delete": "if ($release.draft -ne $true)",
    "draft repository identity": "release.url, $releaseUri",
    "draft tag identity": "release.tag_name, $ReleaseTag",
    "draft delete acknowledgement": "Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri",
    "release-owner scan": "Assert-NoReleaseOwnsTag",
    "post-cleanup tag resolution": "$resolvedPreserved = Resolve-ExactRemoteTagSha",
    "post-cleanup SHA gate": "V25 release tag $ReleaseTag changed during draft rollback",
    "tag preservation": "Preserving exact V25 tag $ReleaseTag",
    "non-destructive result": "TagDeleted = $false",
}
for label, token in helper_mutations.items():
    mutated = helper.replace(token, "__REMOVED__", 1)
    if not validate(mutated, workflow):
        raise SystemExit(f"V25 draft rollback mutation probe did not fail closed: {label}")

workflow_mutations = {
    "reusable tag lookup": "$existingTag = Get-ExactReusableReleaseTag",
    "positive tag ownership": "$tagCreatedByThisRun = $true",
    "ambiguous tag-create reconciliation": "$reconciledTag = Get-ExactReusableReleaseTag",
    "draft transaction marker": "QS3D-DRAFT-CREATE-V25:",
    "draft reconciliation": "$reconciledDraft = Resolve-AmbiguousDraftCreate",
    "verified asset identity": "$verifiedAssetIds[$name] = $uploadedAssetId",
    "publish attempt proof": "$publishPatchAttempted = $true",
    "published reconciliation GET": "$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
    "rollback wiring": "& .\\scripts\\rollback-v25-draft-release.ps1",
}
for label, token in workflow_mutations.items():
    mutated = workflow.replace(token, "__REMOVED__", 1)
    if not validate(helper, mutated):
        raise SystemExit(f"V25 release workflow mutation probe did not fail closed: {label}")

for label, token in {
    "tag delete request": "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
    "tag delete reconciliation": "function Assert-TagDeleteCommittedAfterError",
    "destructive result": "TagDeleted = $true",
}.items():
    mutated = helper + "\n# injected destructive mutation\n" + token + "\n"
    if not validate(mutated, workflow):
        raise SystemExit(f"V25 tag-preservation mutation probe did not fail closed: {label}")

print(
    "PASS V25 restart-safe tag admission, draft/publication acknowledgement recovery, "
    "bounded draft rollback, exact-tag preservation, and non-destructive retry contract"
)
