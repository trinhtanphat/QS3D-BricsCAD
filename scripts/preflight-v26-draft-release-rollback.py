#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper = (root / "scripts" / "rollback-v26-draft-release.ps1").read_text(encoding="utf-8")
publisher = (root / "scripts" / "publish-v26-release.ps1").read_text(encoding="utf-8")


def require_all(text: str, tokens: list[str], label: str, errors: list[str]) -> None:
    for token in tokens:
        if token not in text:
            errors.append(f"{label} missing fail-closed contract: {token}")


def validate(helper_text: str, publisher_text: str) -> list[str]:
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
        "A release still owns tag $ReleaseTag; refusing rollback completion.",
        "if (-not $TagCreatedByThisRun)",
        "Non-owned V26 release tag $ReleaseTag changed during draft rollback; refusing to claim restart safety.",
        "Preserving exact V26 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.",
        "Owned V26 release tag $ReleaseTag changed during draft rollback; refusing to claim restart safety.",
        "Preserving exact V26 tag $ReleaseTag at $resolvedPreserved for safe retry; rollback intentionally avoids destructive tag deletion.",
        "TagDeleted = $false",
    ]
    require_all(helper_text, helper_contract, "helper", errors)

    destructive_tag_tokens = [
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
        "function Assert-TagDeleteCommittedAfterError",
        "Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri",
        "$tagRefUri =",
        "$tagGetUri =",
        "refusing tag deletion.",
        "changed during rollback; refusing tag deletion.",
    ]
    for forbidden in [
        "TagWasAbsentBeforeCreate",
        "releases/tags/",
        "git push --delete",
        "git push origin :refs/tags/",
        "-Force",
        *destructive_tag_tokens,
    ]:
        if forbidden in helper_text:
            errors.append(f"helper uses stale/broad destructive contract: {forbidden}")

    release_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $releaseUri")
    release_reconcile = helper_text.find("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", release_delete + 1)
    owner_check = helper_text.find("Assert-NoReleaseOwnsTag", release_reconcile + 1)
    non_owned_branch = helper_text.find("if (-not $TagCreatedByThisRun)", owner_check + 1)
    non_owned_recheck = helper_text.find("Non-owned V26 release tag $ReleaseTag changed during draft rollback; refusing to claim restart safety.", non_owned_branch + 1)
    non_owned_preserve = helper_text.find("Preserving exact V26 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.", non_owned_recheck + 1)
    owned_recheck = helper_text.find("Owned V26 release tag $ReleaseTag changed during draft rollback; refusing to claim restart safety.", non_owned_preserve + 1)
    owned_preserve = helper_text.find("Preserving exact V26 tag $ReleaseTag at $resolvedPreserved for safe retry; rollback intentionally avoids destructive tag deletion.", owned_recheck + 1)
    ordered_helper = [release_delete, release_reconcile, owner_check, non_owned_branch, non_owned_recheck, non_owned_preserve, owned_recheck, owned_preserve]
    if min(ordered_helper) < 0 or ordered_helper != sorted(ordered_helper):
        errors.append("rollback order must remain draft-delete/reconcile -> release-owner scan -> non-owned exact-tag preservation -> owned exact-tag preservation")

    if helper_text.count("$resolvedPreserved = Resolve-ExactRemoteTagSha") < 2:
        errors.append("rollback must re-resolve the exact remote tag independently in both ownership branches before claiming restart safety")
    if helper_text.count("TagDeleted = $false") < 2:
        errors.append("rollback result must report non-destructive TagDeleted=false in both ownership branches")
    if "TagDeleted = $true" in helper_text:
        errors.append("rollback must never report destructive tag deletion")

    publisher_contract = [
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
    require_all(publisher_text, publisher_contract, "publisher", errors)

    draft_create_publisher_contract = [
        "$draftTransactionMarker =",
        "QS3D-DRAFT-CREATE-V26:",
        "$expectedReleaseName = \"QS3D for BricsCAD V26 $env:RELEASE_TAG\"",
        "$draftCreateError = $_",
        "$reconciledDraft = Resolve-AmbiguousDraftCreate",
        "draft-create acknowledgement was ambiguous, but exactly one transaction-owned draft was recovered",
        "$release = $reconciledDraft",
    ]
    require_all(publisher_text, draft_create_publisher_contract, "publisher draft-create acknowledgement", errors)

    draft_create_start = publisher_text.find("function Resolve-AmbiguousDraftCreate")
    draft_create_end = publisher_text.find("function Assert-PublishedReleaseMatchesVerifiedTransaction", draft_create_start + 1)
    if draft_create_start < 0 or draft_create_end <= draft_create_start:
        errors.append("publisher draft-create acknowledgement missing bounded Resolve-AmbiguousDraftCreate function scope")
        draft_create_scope = ""
    else:
        draft_create_scope = publisher_text[draft_create_start:draft_create_end]
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
    require_all(draft_create_scope, draft_create_function_contract, "publisher Resolve-AmbiguousDraftCreate", errors)

    for stale in ["$preCreateTagLines", "$tagWasAbsentBeforeCreate", "$releaseCreatedByThisRun", "-TagWasAbsentBeforeCreate", "if (git tag --list $env:RELEASE_TAG) { throw"]:
        if stale in publisher_text:
            errors.append(f"publisher retains stale reject/absence-based ownership contract: {stale}")

    if publisher_text.count("$tagReadyForRelease = $true") < 2:
        errors.append("publisher must admit both acknowledged/reusable exact-tag paths before publication")

    marker = publisher_text.find("$draftTransactionMarker =")
    request = publisher_text.find("$request = @{", marker + 1)
    create = publisher_text.find("Invoke-RestMethod -Method Post -Uri \"https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases\"", request + 1)
    create_error = publisher_text.find("$draftCreateError = $_", create + 1)
    reconcile = publisher_text.find("$reconciledDraft = Resolve-AmbiguousDraftCreate", create_error + 1)
    recovered = publisher_text.find("$release = $reconciledDraft", reconcile + 1)
    release_id = publisher_text.find("$releaseId = [long]$release.id", recovered + 1)
    draft_order = [marker, request, create, create_error, reconcile, recovered, release_id]
    if min(draft_order) < 0 or draft_order != sorted(draft_order):
        errors.append("V26 draft-create order must be marker -> request -> one POST -> catch -> bounded reconciliation -> recovered identity -> releaseId assignment")

    return errors


canonical_errors = validate(helper, publisher)
if canonical_errors:
    raise SystemExit("V26 draft rollback contract failed: " + "; ".join(canonical_errors))

mutations = {
    "reusable lookup": (helper, publisher.replace("$existingTag = Get-ExactReusableReleaseTag", "$existingTag = $null", 1)),
    "tag-ready proof": (helper, publisher.replace("$tagReadyForRelease = $true", "$tagReadyForRelease = $false")),
    "positive ownership": (helper, publisher.replace("$tagCreatedByThisRun = $true", "$tagCreatedByThisRun = $false", 1)),
    "created ref": (helper, publisher.replace("createdTag.ref, $tagRef", "createdTag.ref, 'refs/tags/other'", 1)),
    "created type": (helper, publisher.replace("createdTag.object.type, 'commit'", "createdTag.object.type, 'tag'", 1)),
    "created SHA": (helper, publisher.replace("createdTag.object.sha, $env:GITHUB_SHA", "createdTag.object.sha, ('0' * 40)", 1)),
    "ambiguous tag-create reconciliation": (helper, publisher.replace("$reconciledTag = Get-ExactReusableReleaseTag", "$reconciledTag = $null", 1)),
    "draft transaction marker": (helper, publisher.replace("QS3D-DRAFT-CREATE-V26:", "QS3D-DRAFT-CREATE-REMOVED:", 1)),
    "draft reconciliation": (helper, publisher.replace("$reconciledDraft = Resolve-AmbiguousDraftCreate", "$reconciledDraft = $null", 1)),
    "draft exact tag": (helper, publisher.replace("ReleaseSnapshot.tag_name, $env:RELEASE_TAG", "ReleaseSnapshot.tag_name, 'other'", 1)),
    "draft exact SHA": (helper, publisher.replace("ReleaseSnapshot.target_commitish, $env:GITHUB_SHA", "ReleaseSnapshot.target_commitish, ('0' * 40)", 1)),
    "draft exact name": (helper, publisher.replace("ReleaseSnapshot.name, $ExpectedReleaseName", "ReleaseSnapshot.name, 'other'", 1)),
    "draft prerelease": (helper, publisher.replace("ReleaseSnapshot.prerelease -ne $IsPrerelease", "$false", 1)),
    "draft marker identity": (helper, publisher.replace("$body.IndexOf($TransactionMarker, [StringComparison]::Ordinal)", "$false", 1)),
    "bounded enumeration": (helper, publisher.replace("$maxPages = 20", "$maxPages = [int]::MaxValue", 1)),
    "draft delete reconciliation": (helper.replace("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", "# removed", 1), publisher),
    "release-owner scan": (helper.replace("\nAssert-NoReleaseOwnsTag\n", "\n# removed\n", 1), publisher),
    "non-owned preservation": (helper.replace("Preserving exact V26 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.", "removed non-owned preservation", 1), publisher),
    "non-owned exact tag": (helper.replace("Non-owned V26 release tag $ReleaseTag changed during draft rollback; refusing to claim restart safety.", "removed non-owned exact tag", 1), publisher),
    "owned preservation": (helper.replace("Preserving exact V26 tag $ReleaseTag at $resolvedPreserved for safe retry; rollback intentionally avoids destructive tag deletion.", "removed owned preservation", 1), publisher),
    "owned exact tag": (helper.replace("Owned V26 release tag $ReleaseTag changed during draft rollback; refusing to claim restart safety.", "removed owned exact tag", 1), publisher),
    "non-destructive result": (helper.replace("TagDeleted = $false", "TagDeleted = $true", 1), publisher),
    "asset identity": (helper, publisher.replace("$verifiedAssetIds[$expectedAsset] = $uploadedAssetId", "# removed", 1)),
    "publish attempt proof": (helper, publisher.replace("$publishPatchAttempted = $true", "$publishPatchAttempted = $false", 1)),
    "published reconciliation": (helper, publisher.replace("$reconciledRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers", "$reconciledRelease = $null", 1)),
    "rollback wiring": (helper, publisher.replace("& .\\scripts\\rollback-v26-draft-release.ps1", "# removed", 1)),
}
for label, (mutated_helper, mutated_publisher) in mutations.items():
    if not validate(mutated_helper, mutated_publisher):
        raise SystemExit(f"V26 draft rollback mutation probe did not fail closed: {label}")

print("PASS V26 restart-safe tag admission, draft-create acknowledgement recovery, publish reconciliation, exact draft rollback, and tag preservation contract")