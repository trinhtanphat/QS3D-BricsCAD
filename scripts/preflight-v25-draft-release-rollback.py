#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
helper_path = root / "scripts" / "rollback-v25-draft-release.ps1"
workflow_path = root / ".github" / "workflows" / "release-v25.yml"

helper = helper_path.read_text(encoding="utf-8")
workflow = workflow_path.read_text(encoding="utf-8")


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
        "V25 draft DELETE acknowledgement was ambiguous, but the exact release is authoritatively absent; treating draft deletion as committed.",
        "Exact owned V25 draft $ReleaseId still exists after DELETE error; refusing to assume deletion.",
        "Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri",
        "Preserving exact V25 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.",
        "TagCreatedByThisRun = $false",
        "TagDeleted = $false",
        "function Assert-NoReleaseOwnsTag",
        "releases?per_page=100&page=$page",
        "A release still owns tag $ReleaseTag; refusing tag deletion.",
        "Release enumeration exceeded $maxPages pages",
        "Assert-NoReleaseOwnsTag\n\n$resolvedAfter = Resolve-ExactRemoteTagSha",
        "if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))",
        "Remote tag $ReleaseTag changed during rollback; refusing tag deletion.",
        "$escapedReleaseTag = [Uri]::EscapeDataString($ReleaseTag)",
        "git/refs/tags/",
        "git/ref/tags/",
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
        "function Assert-TagDeleteCommittedAfterError",
        "$remainingTag = Invoke-RestMethod -Method Get -Uri $TagGetUri -Headers $headers",
        "V25 tag DELETE acknowledgement was ambiguous, but the exact tag is authoritatively absent; treating tag deletion as committed.",
        "remainingTag.object.sha, $WorkflowSha",
        "Exact owned V25 tag $ReleaseTag still exists after DELETE error; refusing to assume deletion.",
        "Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri",
    ]
    for needle in required_helper:
        if needle not in helper_text:
            errors.append(f"helper missing fail-closed contract: {needle}")

    for forbidden in ["TagWasAbsentBeforeCreate", "releases/tags/", "git push --delete", "git push origin :refs/tags/", "-Force"]:
        if forbidden in helper_text:
            errors.append(f"helper uses stale/broad destructive contract: {forbidden}")

    draft_get = helper_text.find("$remainingRelease = Invoke-RestMethod -Method Get -Uri $ReleaseUri -Headers $headers")
    draft_not_found = helper_text.find("if (Test-GitHubNotFound -ErrorRecord $_)", draft_get + 1)
    draft_absent = helper_text.find("V25 draft DELETE acknowledgement was ambiguous, but the exact release is authoritatively absent; treating draft deletion as committed.", draft_not_found + 1)
    draft_still_exists = helper_text.find("Exact owned V25 draft $ReleaseId still exists after DELETE error; refusing to assume deletion.", draft_absent + 1)
    if min(draft_get, draft_not_found, draft_absent, draft_still_exists) < 0 or not (
        draft_get < draft_not_found < draft_absent < draft_still_exists
    ):
        errors.append("draft DELETE reconciliation must classify authoritative 404 before accepting absence and refuse a surviving exact draft")

    non_owned = helper_text.find("if (-not $TagCreatedByThisRun)")
    preserve = helper_text.find("Preserving exact V25 tag $ReleaseTag because this run lacks positive tag-creation ownership proof.", non_owned + 1)
    owner_check = helper_text.find("Assert-NoReleaseOwnsTag", preserve + 1)
    if min(non_owned, preserve, owner_check) < 0 or not (non_owned < preserve < owner_check):
        errors.append("non-owned tag path must preserve the exact tag and return before any tag-owner/delete path")

    tag_get = helper_text.find("$remainingTag = Invoke-RestMethod -Method Get -Uri $TagGetUri -Headers $headers")
    tag_not_found = helper_text.find("if (Test-GitHubNotFound -ErrorRecord $_)", tag_get + 1)
    tag_absent = helper_text.find("V25 tag DELETE acknowledgement was ambiguous, but the exact tag is authoritatively absent; treating tag deletion as committed.", tag_not_found + 1)
    tag_sha = helper_text.find("remainingTag.object.sha, $WorkflowSha", tag_absent + 1)
    tag_still_exists = helper_text.find("Exact owned V25 tag $ReleaseTag still exists after DELETE error; refusing to assume deletion.", tag_sha + 1)
    if min(tag_get, tag_not_found, tag_absent, tag_sha, tag_still_exists) < 0 or not (
        tag_get < tag_not_found < tag_absent < tag_sha < tag_still_exists
    ):
        errors.append("tag DELETE reconciliation must classify authoritative 404 before accepting absence and validate exact SHA before refusing a surviving tag")

    release_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $releaseUri")
    release_reconcile = helper_text.find("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", release_delete + 1)
    post_sha_check = helper_text.find("Remote tag $ReleaseTag changed during rollback; refusing tag deletion.")
    tag_delete = helper_text.find("Invoke-RestMethod -Method Delete -Uri $tagRefUri")
    tag_reconcile = helper_text.find("Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri", tag_delete + 1)
    if min(release_delete, release_reconcile, owner_check, post_sha_check, tag_delete, tag_reconcile) < 0 or not (
        release_delete < release_reconcile < non_owned < preserve < owner_check < post_sha_check < tag_delete < tag_reconcile
    ):
        errors.append("helper deletion order must be draft delete -> reconciliation -> non-owned preserve boundary -> owned release scan -> exact-SHA recheck -> tag delete -> reconciliation")

    required_workflow = [
        '$tagRef = "refs/tags/$env:RELEASE_TAG"',
        '$tagRefUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/git/refs"',
        "$tagCreateRequest = @{ ref = $tagRef; sha = $env:GITHUB_SHA } | ConvertTo-Json",
        "$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri",
        'createdTag.ref, "refs/tags/$env:RELEASE_TAG"',
        "createdTag.object.sha, $env:GITHUB_SHA",
        "$tagCreatedByThisRun = $false",
        "$tagCreatedByThisRun = $true",
        "$releaseId = [long]0",
        "$releaseId = [long]$release.id",
        "& .\\scripts\\rollback-v25-draft-release.ps1",
        "-TagCreatedByThisRun $tagCreatedByThisRun",
        "Automatic V25 draft rollback failed",
        "Automatic rollback completed; retry with the same tag is safe.",
    ]
    for needle in required_workflow:
        if needle not in workflow_text:
            errors.append(f"workflow missing positive-ownership transaction contract: {needle}")

    for stale in ["$existing = @(git ls-remote --tags origin $tagRef", "& gh @createArgs"]:
        if stale in workflow_text:
            errors.append(f"workflow retains absence/implicit-tag creation contract: {stale}")

    return errors


canonical_errors = validate(helper, workflow)
if canonical_errors:
    raise SystemExit("V25 draft rollback contract failed: " + "; ".join(canonical_errors))

mutations = {
    "tag ref binding": (helper, workflow.replace("$tagCreateRequest = @{ ref = $tagRef; sha = $env:GITHUB_SHA } | ConvertTo-Json", "$tagCreateRequest = @{ ref = 'refs/tags/not-owned'; sha = $env:GITHUB_SHA } | ConvertTo-Json", 1)),
    "positive ownership": (helper, workflow.replace("$tagCreatedByThisRun = $true", "$tagCreatedByThisRun = $false", 1)),
    "draft-only delete": (helper.replace("if ($release.draft -ne $true)", "if ($false)", 1), workflow),
    "draft delete acknowledgement reconciliation": (helper.replace("Assert-DraftDeleteCommittedAfterError -DeleteError $_ -ReleaseUri $releaseUri", "# draft delete reconciliation removed", 1), workflow),
    "draft authoritative absence": (helper.replace("if (Test-GitHubNotFound -ErrorRecord $_)", "if ($false)", 1), workflow),
    "non-owned tag preservation": (helper.replace("if (-not $TagCreatedByThisRun)", "if ($false)", 1), workflow),
    "non-owned tag return": (helper.replace("TagDeleted = $false", "TagDeleted = $true", 1), workflow),
    "release-owner scan": (helper.replace("Assert-NoReleaseOwnsTag\n\n$resolvedAfter", "$resolvedAfter", 1), workflow),
    "post-owner SHA recheck": (helper.replace("if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))", "if ($false)", 1), workflow),
    "tag delete acknowledgement reconciliation": (helper.replace("Assert-TagDeleteCommittedAfterError -DeleteError $_ -TagGetUri $tagGetUri", "# tag delete reconciliation removed", 1), workflow),
    "tag authoritative absence": (helper[: helper.find("$remainingTag = Invoke-RestMethod -Method Get -Uri $TagGetUri -Headers $headers")] + helper[helper.find("$remainingTag = Invoke-RestMethod -Method Get -Uri $TagGetUri -Headers $headers"):].replace("if (Test-GitHubNotFound -ErrorRecord $_)", "if ($false)", 1), workflow),
    "tag exact identity": (helper.replace("remainingTag.object.sha, $WorkflowSha", "remainingTag.object.sha, ('0' * 40)", 1), workflow),
    "rollback wiring": (helper, workflow.replace("& .\\scripts\\rollback-v25-draft-release.ps1", "# rollback removed", 1)),
}
for label, (mutated_helper, mutated_workflow) in mutations.items():
    if not validate(mutated_helper, mutated_workflow):
        raise SystemExit(f"V25 draft rollback mutation probe did not fail closed: {label}")

print("PASS V25 draft release rollback, non-owned tag preservation, and destructive acknowledgement contract")
