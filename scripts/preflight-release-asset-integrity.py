#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"
HELPER = ROOT / "scripts" / "verify-v25-held-file.ps1"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def asset_verified(*, local_hash: str, remote_hash: str) -> bool:
    return bool(local_hash) and local_hash.lower() == (remote_hash or "").lower()


def tag_targets_sha(*, exact_refs, peeled_refs, expected_sha: str) -> bool:
    if len(exact_refs) != 1 or len(peeled_refs) > 1:
        return False
    resolved = peeled_refs[0] if len(peeled_refs) == 1 else exact_refs[0]
    return bool(resolved) and resolved.lower() == expected_sha.lower()


def main() -> int:
    if not WORKFLOW.is_file():
        raise AssertionError("missing .github/workflows/release-v25.yml")
    if not HELPER.is_file():
        raise AssertionError("missing scripts/verify-v25-held-file.ps1")
    text = WORKFLOW.read_text(encoding="utf-8")
    helper = HELPER.read_text(encoding="utf-8")

    required_tokens = (
        "$assetNames = @('QS3D-BricsCAD-V25.zip','QS3D-BricsCAD-V25.zip.sha256','QS3D-BricsCAD-V25.update.json','QS3D-BricsCAD-V25.provenance.json')",
        "$tagRef = \"refs/tags/$env:RELEASE_TAG\"",
        '$tagRefUri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/git/refs"',
        "$tagCreateRequest = @{ ref = $tagRef; sha = $env:GITHUB_SHA } | ConvertTo-Json",
        "$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri",
        "createdTag.object.type, 'commit'",
        "createdTag.object.sha, $env:GITHUB_SHA",
        "$tagCreatedByThisRun = $true",
        'Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases"',
        "$releaseId = [long]$release.id",
        "gh release upload $env:RELEASE_TAG $asset --repo $env:GITHUB_REPOSITORY",
        "function Assert-RemoteReleaseTagTargetsWorkflowSha",
        "git ls-remote --tags origin $tagRef $peeledRef",
        "Assert-RemoteReleaseTagTargetsWorkflowSha",
        "gh release download $env:RELEASE_TAG",
        "$downloadedNames = @(Get-ChildItem -LiteralPath $downloadRoot -File | ForEach-Object Name | Sort-Object)",
        "Draft release asset set mismatch.",
        "scripts\\verify-v25-held-file.ps1",
        "-Operation Hash",
        "-Operation Copy",
        "Draft release asset SHA-256 mismatch for $name; release remains a draft.",
        "Downloaded draft checksum is malformed.",
        "Downloaded draft ZIP fails its SHA-256 checksum.",
        "verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "$published = Invoke-RestMethod -Method Patch -Uri $releaseUri",
        "GitHub release remained a draft after publication request.",
    )
    for token in required_tokens:
        require(token in text, "V25 release publication integrity guard missing token: " + token)

    helper_tokens = (
        "[IO.FileShare]::Read",
        "$rebound = Get-Item -LiteralPath $canonical",
        "[int64]$stream.Length -ne $admittedLength",
        "$sha.ComputeHash($held.Stream)",
        "$held.Stream.CopyTo($output)",
    )
    for token in helper_tokens:
        require(token in helper, "V25 release held-generation helper missing token: " + token)

    forbidden = (
        "$existing = @(git ls-remote --tags origin $tagRef",
        "& gh @createArgs",
        "gh release edit $env:RELEASE_TAG --repo $env:GITHUB_REPOSITORY --draft=false",
        "$localHash = (Get-FileHash -LiteralPath (Join-Path $dist $name) -Algorithm SHA256).Hash",
        "$remoteHash = (Get-FileHash -LiteralPath (Join-Path $downloadRoot $name) -Algorithm SHA256).Hash",
        "if ((Get-FileHash -LiteralPath $remoteZip -Algorithm SHA256).Hash.ToLowerInvariant() -ne $Matches[1])",
    )
    for token in forbidden:
        require(token not in text, "V25 release publication regressed to stale/unsafe integrity semantics: " + token)

    for local_hash, remote_hash, expected, label in (
        ("AA" * 32, "aa" * 32, True, "exact downloaded bytes"),
        ("AA" * 32, "BB" * 32, False, "hash mismatch"),
        ("", "", False, "missing digest evidence"),
    ):
        require(asset_verified(local_hash=local_hash, remote_hash=remote_hash) is expected, "release asset integrity model mismatch for " + label)

    workflow_sha = "a" * 40
    for exact_refs, peeled_refs, expected, label in (
        ([workflow_sha], [], True, "lightweight exact tag"),
        (["b" * 40], [workflow_sha], True, "annotated tag peeled to exact commit"),
        ([], [], False, "missing tag"),
        (["b" * 40], [], False, "lightweight wrong target"),
        (["b" * 40], ["c" * 40], False, "annotated wrong peeled target"),
    ):
        require(tag_targets_sha(exact_refs=exact_refs, peeled_refs=peeled_refs, expected_sha=workflow_sha) is expected, "release tag target model mismatch for " + label)

    tag_create_pos = text.find("$createdTag = Invoke-RestMethod -Method Post -Uri $tagRefUri")
    ownership_pos = text.find("$tagCreatedByThisRun = $true", tag_create_pos)
    release_create_pos = text.find('Invoke-RestMethod -Method Post -Uri "https://api.github.com/repos/$env:GITHUB_REPOSITORY/releases"', ownership_pos)
    release_id_pos = text.find("$releaseId = [long]$release.id", release_create_pos)
    upload_pos = text.find("gh release upload $env:RELEASE_TAG $asset --repo $env:GITHUB_REPOSITORY", release_id_pos)
    tag_pos = text.find("Assert-RemoteReleaseTagTargetsWorkflowSha", upload_pos)
    download_pos = text.find("gh release download $env:RELEASE_TAG", tag_pos)
    set_pos = text.find("Draft release asset set mismatch.", download_pos)
    hash_pos = text.find("Draft release asset SHA-256 mismatch for $name; release remains a draft.", set_pos)
    copy_pos = text.find("-Operation Copy -Path $remoteZip -Destination $heldRemoteZip", hash_pos)
    checksum_pos = text.find("Downloaded draft ZIP fails its SHA-256 checksum.", copy_pos)
    signature_pos = text.find("verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT", checksum_pos)
    publish_pos = text.find("$published = Invoke-RestMethod -Method Patch -Uri $releaseUri", signature_pos)
    positions = (tag_create_pos, ownership_pos, release_create_pos, release_id_pos, upload_pos, tag_pos, download_pos, set_pos, hash_pos, copy_pos, checksum_pos, signature_pos, publish_pos)
    require(min(positions) >= 0 and list(positions) == sorted(positions), "V25 release must create exact owned tag -> create exact draft -> upload -> assert exact tag SHA -> download exact asset set -> held hash -> stable ZIP copy/checksum -> signature verify -> publish exact release")

    print("PASS: V25 commercial publication uses positive exact-tag ownership, exact draft identity, exact asset set, reusable exact-tag assertion, held-generation hashes, stable ZIP checksum, Authenticode verification, and only then exact-release publication.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)