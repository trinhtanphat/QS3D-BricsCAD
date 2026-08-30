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
        "function Assert-NoReleaseOwnsTag",
        "releases?per_page=100&page=$page",
        "A release still owns tag $ReleaseTag; refusing tag deletion.",
        "Release enumeration exceeded $maxPages pages",
        "Assert-NoReleaseOwnsTag\n\n$resolvedAfter = Resolve-ExactRemoteTagSha",
        "Remote tag $ReleaseTag changed during rollback; refusing tag deletion.",
        "Invoke-RestMethod -Method Delete -Uri $tagRefUri",
    ]
    for needle in required_helper:
        if needle not in helper_text:
            errors.append(f"helper missing fail-closed contract: {needle}")

    for forbidden in ["TagWasAbsentBeforeCreate", "releases/tags/", "git push --delete", "-Force"]:
        if forbidden in helper_text:
            errors.append(f"helper uses stale/broad destructive contract: {forbidden}")

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
        "rollback-v25-draft-release.ps1",
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
    "release-owner scan": (helper.replace("Assert-NoReleaseOwnsTag\n\n$resolvedAfter", "$resolvedAfter", 1), workflow),
    "post-owner SHA recheck": (helper.replace("if (-not [string]::Equals($resolvedAfter, $WorkflowSha, [StringComparison]::OrdinalIgnoreCase))", "if ($false)", 1), workflow),
    "rollback wiring": (helper, workflow.replace("& .\\scripts\\rollback-v25-draft-release.ps1", "# rollback removed", 1)),
}
for label, (mutated_helper, mutated_workflow) in mutations.items():
    if not validate(mutated_helper, mutated_workflow):
        raise SystemExit(f"V25 draft rollback mutation probe did not fail closed: {label}")

print("PASS V25 draft release rollback contract")
