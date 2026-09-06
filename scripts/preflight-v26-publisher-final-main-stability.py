#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts" / "publish-v26-release.ps1"

FUNCTION = "function Assert-ProtectedMainStableForPublisherMutation"
MAIN_GET = '$publisherMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main" -Headers $headers'
MAIN_RAW = "$publisherMain = [string]$publisherMainResponse.sha"
MAIN_CANONICAL = "if ($publisherMain -notmatch '^[0-9a-f]{40}$')"
MAIN_REF = "$publisherMainRef = 'refs/remotes/origin/qs3d-v26-publisher-admitted-main'"
FETCH = '& git fetch --no-tags --force origin "+refs/heads/main:$publisherMainRef"'
FETCHED = "$fetchedPublisherMain = ([string](& git rev-parse --verify $publisherMainRef)).Trim().ToLowerInvariant()"
IDENTITY = "[string]::Equals($fetchedPublisherMain, $publisherMain, [StringComparison]::Ordinal)"
ANCESTRY = '& git merge-base --is-ancestor $env:GITHUB_SHA $publisherMain'
DIFF = '& git diff --quiet --no-ext-diff "$env:GITHUB_SHA..$publisherMain" -- @publisherReleaseRelevantPaths'
SECOND_GET = '$confirmedPublisherMainResponse = Invoke-RestMethod -Method Get -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/commits/main" -Headers $headers'
SECOND_RAW = "$confirmedPublisherMain = [string]$confirmedPublisherMainResponse.sha"
SECOND_CANONICAL = "if ($confirmedPublisherMain -notmatch '^[0-9a-f]{40}$')"
STABILITY = "[string]::Equals($confirmedPublisherMain, $publisherMain, [StringComparison]::Ordinal)"
CALL = "Assert-ProtectedMainStableForPublisherMutation"
TAG_POST = "$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri"
RELEASE_POST = '$release = Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases"'
UPLOAD = "& .\\scripts\\invoke-v26-held-release-upload.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise SystemExit(message)


def function_block(publisher: str) -> str:
    start = publisher.find(FUNCTION)
    require(start >= 0, "V26 publisher must define the protected-main mutation-boundary admission function.")
    next_function = publisher.find("\nfunction ", start + len(FUNCTION))
    return publisher[start: next_function if next_function >= 0 else len(publisher)]


def validate(publisher: str) -> None:
    block = function_block(publisher)
    for token, label in (
        (MAIN_GET, "authenticated protected-main API read"),
        (MAIN_RAW, "raw protected-main SHA binding"),
        (MAIN_CANONICAL, "canonical protected-main SHA validation"),
        (MAIN_REF, "dedicated fetched-main ref"),
        (FETCH, "protected-main fetch"),
        (FETCHED, "fetched-main resolution"),
        (IDENTITY, "API/fetch identity binding"),
        (ANCESTRY, "workflow-source ancestry proof"),
        (DIFF, "release-relevant drift classification"),
        (SECOND_GET, "second protected-main API read"),
        (SECOND_RAW, "second raw protected-main SHA binding"),
        (SECOND_CANONICAL, "second protected-main canonical validation"),
        (STABILITY, "protected-main stability comparison"),
    ):
        require(token in block, f"V26 publisher mutation admission missing {label}.")

    require("$LASTEXITCODE -eq 1" in block and "$LASTEXITCODE -ne 0" in block,
            "V26 publisher mutation admission must distinguish release drift from git classification failure and fail closed.")
    require("src/QS3D.BricsCAD.V26/" in block and ".github/workflows/" in block and "scripts/" in block,
            "V26 publisher mutation admission must classify the release-relevant path set.")
    require("throw" in block.lower(), "V26 publisher mutation admission must fail closed.")

    tag_post = publisher.find(TAG_POST)
    release_post = publisher.find(RELEASE_POST)
    upload = publisher.find(UPLOAD)
    require(tag_post >= 0 and release_post >= 0 and upload >= 0,
            "Expected V26 publisher mutation markers were not found.")

    calls = []
    offset = 0
    while True:
        pos = publisher.find(CALL, offset)
        if pos < 0:
            break
        if not publisher[max(0, pos - 9):pos].endswith("function "):
            calls.append(pos)
        offset = pos + len(CALL)
    require(any(pos < tag_post for pos in calls),
            "V26 publisher must admit protected main before release-tag mutation.")
    require(any(tag_post < pos < release_post for pos in calls),
            "V26 publisher must re-admit protected main after tag mutation and before draft-release creation.")
    require(any(release_post < pos < upload for pos in calls),
            "V26 publisher must re-admit protected main after draft creation and before held asset upload.")


def expect_failure(publisher: str, label: str) -> None:
    try:
        validate(publisher)
    except SystemExit:
        return
    raise SystemExit(f"Mutation probe unexpectedly passed: {label}")


publisher = PUBLISHER.read_text(encoding="utf-8")
validate(publisher)

for token, label in (
    (MAIN_GET, "main API read removal"),
    (FETCH, "main fetch removal"),
    (ANCESTRY, "ancestry removal"),
    (DIFF, "release drift classifier removal"),
    (SECOND_GET, "second main read removal"),
    (STABILITY, "stability comparison removal"),
):
    expect_failure(publisher.replace(token, "# removed by mutation probe", 1), label)

expect_failure(publisher.replace("$LASTEXITCODE -ne 0", "$LASTEXITCODE -eq 0", 1),
               "fail-open git classification")

# Remove one admission call from each mutation boundary; every phase must remain independently fenced.
for marker, label in ((TAG_POST, "tag boundary"), (RELEASE_POST, "draft boundary"), (UPLOAD, "asset boundary")):
    marker_pos = publisher.find(marker)
    prior_call = publisher.rfind(CALL, 0, marker_pos)
    require(prior_call >= 0, f"Expected admission call before {label} was not found for mutation probe.")
    line_start = publisher.rfind("\n", 0, prior_call) + 1
    line_end = publisher.find("\n", prior_call)
    if line_end < 0:
        line_end = len(publisher)
    mutated = publisher[:line_start] + publisher[line_end + (1 if line_end < len(publisher) else 0):]
    expect_failure(mutated, f"missing {label} admission")

print("PASS V26 publisher protected-main mutation-boundary stability guard")
