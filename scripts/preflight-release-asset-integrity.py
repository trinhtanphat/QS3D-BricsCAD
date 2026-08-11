#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github/workflows/release-v25.yml"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def asset_verified(*, match_count: int, local_size: int, remote_size: int, local_hash: str, remote_hash: str) -> bool:
    return (
        match_count == 1
        and local_size == remote_size
        and bool(local_hash)
        and local_hash.lower() == (remote_hash or "").lower()
    )


def tag_targets_sha(*, exact_refs, peeled_refs, expected_sha: str) -> bool:
    if len(exact_refs) != 1 or len(peeled_refs) > 1:
        return False
    resolved = peeled_refs[0] if len(peeled_refs) == 1 else exact_refs[0]
    return bool(resolved) and resolved.lower() == expected_sha.lower()


def main() -> int:
    if not WORKFLOW.is_file():
        raise AssertionError("missing .github/workflows/release-v25.yml")
    text = WORKFLOW.read_text(encoding="utf-8")

    required_tokens = (
        "function Assert-RemoteReleaseTagTargetsWorkflowSha",
        "$tagRef = \"refs/tags/$env:RELEASE_TAG\"",
        "$peeledRef = $tagRef + '^{}'",
        "$lines = @(git ls-remote --tags origin $tagRef $peeledRef)",
        "if ($LASTEXITCODE -ne 0)",
        "Malformed git ls-remote output while resolving release tag",
        "$exact.Count -ne 1 -or $peeled.Count -gt 1",
        "$resolvedSha = if ($peeled.Count -eq 1) { $peeled[0] } else { $exact[0] }",
        "instead of qualified workflow SHA $env:GITHUB_SHA",
        "$localAssets = @{}",
        "Duplicate local release asset name",
        "$draftRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers",
        "$matches = @($draftRelease.assets | Where-Object { [string]$_.name -ceq $expectedAsset })",
        "$matches.Count -ne 1",
        "$remoteLength = [int64]$uploadedAsset.size",
        "Uploaded release asset size mismatch",
        "$assetDownloadHeaders['Accept'] = 'application/octet-stream'",
        "-Uri ([string]$uploadedAsset.url)",
        "-OutFile $downloadedAsset",
        "$localHash = (Get-FileHash -LiteralPath $localAsset -Algorithm SHA256).Hash",
        "$remoteHash = (Get-FileHash -LiteralPath $downloadedAsset -Algorithm SHA256).Hash",
        "Uploaded release asset SHA-256 mismatch",
        "finally {",
        "Remove-Item -LiteralPath $downloadedAsset -Force -ErrorAction SilentlyContinue",
        "$publishBody = @{ draft = $false } | ConvertTo-Json",
    )
    for token in required_tokens:
        require(token in text, "V25 release publication integrity guard missing token: " + token)

    asset_cases = (
        (1, 1024, 1024, "AA" * 32, "aa" * 32, True, "exact uploaded bytes"),
        (0, 1024, 1024, "AA" * 32, "AA" * 32, False, "missing asset"),
        (2, 1024, 1024, "AA" * 32, "AA" * 32, False, "duplicate asset name"),
        (1, 1024, 1000, "AA" * 32, "AA" * 32, False, "truncated upload"),
        (1, 1024, 1024, "AA" * 32, "BB" * 32, False, "same-size hash mismatch"),
        (1, 1024, 1024, "", "", False, "missing digest evidence"),
    )
    for match_count, local_size, remote_size, local_hash, remote_hash, expected, label in asset_cases:
        actual = asset_verified(
            match_count=match_count,
            local_size=local_size,
            remote_size=remote_size,
            local_hash=local_hash,
            remote_hash=remote_hash,
        )
        require(actual is expected, f"release asset integrity model mismatch for {label}: expected {expected}, got {actual}")

    workflow_sha = "a" * 40
    tag_cases = (
        ([workflow_sha], [], True, "lightweight exact tag"),
        (["b" * 40], [workflow_sha], True, "annotated tag peeled to exact commit"),
        ([], [], False, "missing tag"),
        (["b" * 40], [], False, "lightweight wrong target"),
        (["b" * 40], ["c" * 40], False, "annotated wrong peeled target"),
        ([workflow_sha, workflow_sha], [], False, "ambiguous exact ref records"),
        (["b" * 40], [workflow_sha, workflow_sha], False, "ambiguous peeled ref records"),
    )
    for exact_refs, peeled_refs, expected, label in tag_cases:
        actual = tag_targets_sha(exact_refs=exact_refs, peeled_refs=peeled_refs, expected_sha=workflow_sha)
        require(actual is expected, f"release tag target model mismatch for {label}: expected {expected}, got {actual}")

    draft_create_pos = text.find("$release = Invoke-RestMethod `")
    first_tag_call = text.find("\n          Assert-RemoteReleaseTagTargetsWorkflowSha", draft_create_pos)
    upload_pos = text.find("Invoke-RestMethod `\n              -Method Post `\n              -Uri ($uploadBase + '?name=' + $encodedName)")
    draft_read_pos = text.find("$draftRelease = Invoke-RestMethod -Method Get -Uri $releaseUri -Headers $headers")
    unique_pos = text.find("$matches.Count -ne 1")
    size_pos = text.find("$remoteLength = [int64]$uploadedAsset.size")
    download_pos = text.find("-Uri ([string]$uploadedAsset.url)")
    local_hash_pos = text.find("$localHash = (Get-FileHash -LiteralPath $localAsset -Algorithm SHA256).Hash")
    remote_hash_pos = text.find("$remoteHash = (Get-FileHash -LiteralPath $downloadedAsset -Algorithm SHA256).Hash")
    hash_compare_pos = text.find("Uploaded release asset SHA-256 mismatch")
    second_tag_call = text.find("\n          Assert-RemoteReleaseTagTargetsWorkflowSha", first_tag_call + 1)
    publish_pos = text.find("$publishBody = @{ draft = $false } | ConvertTo-Json")
    positions = (
        draft_create_pos,
        first_tag_call,
        upload_pos,
        draft_read_pos,
        unique_pos,
        size_pos,
        download_pos,
        local_hash_pos,
        remote_hash_pos,
        hash_compare_pos,
        second_tag_call,
        publish_pos,
    )
    require(min(positions) >= 0, "V25 release verification/publication ordering token is missing")
    require(
        draft_create_pos < first_tag_call < upload_pos < draft_read_pos < unique_pos < size_pos < download_pos < local_hash_pos < remote_hash_pos < hash_compare_pos < second_tag_call < publish_pos,
        "V25 release must bind remote tag after draft creation, verify uploaded bytes, re-bind the tag, then publish",
    )
    require(text.count("\n          Assert-RemoteReleaseTagTargetsWorkflowSha") == 2, "V25 release must assert remote tag target exactly twice in the publication path")

    require("draft = $true" in text, "V25 release must remain draft-first")
    require("if ($publishedRelease.draft -ne $false)" in text, "V25 release must verify publish transition")

    print(
        "PASS: V25 GitHub Release publication is draft-first, binds lightweight/annotated remote tag identity to the exact qualified workflow SHA before upload and again before publish, and requires each uploaded asset to match local size and re-downloaded SHA-256."
    )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
