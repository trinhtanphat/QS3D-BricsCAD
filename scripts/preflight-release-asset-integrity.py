#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
WORKFLOW = ROOT / ".github" / "workflows" / "release-v25.yml"


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
    text = WORKFLOW.read_text(encoding="utf-8")

    required_tokens = (
        "$assetNames = @('QS3D-BricsCAD-V25.zip','QS3D-BricsCAD-V25.zip.sha256','QS3D-BricsCAD-V25.update.json','QS3D-BricsCAD-V25.provenance.json')",
        "$tagRef = \"refs/tags/$env:RELEASE_TAG\"",
        "git ls-remote --tags origin $tagRef ($tagRef + '^{}')",
        "if ($existing.Count -gt 0) { throw \"Remote release tag already exists: $env:RELEASE_TAG\" }",
        "'--target', $env:GITHUB_SHA",
        "'--draft'",
        "Malformed remote tag response; release remains a draft.",
        "Release tag does not target exact qualified workflow SHA; release remains a draft.",
        "gh release download $env:RELEASE_TAG",
        "$downloadedNames = @(Get-ChildItem -LiteralPath $downloadRoot -File | ForEach-Object Name | Sort-Object)",
        "Draft release asset set mismatch.",
        "$localHash = (Get-FileHash -LiteralPath (Join-Path $dist $name) -Algorithm SHA256).Hash",
        "$remoteHash = (Get-FileHash -LiteralPath (Join-Path $downloadRoot $name) -Algorithm SHA256).Hash",
        "Draft release asset SHA-256 mismatch for $name; release remains a draft.",
        "Downloaded draft checksum is malformed.",
        "Downloaded draft ZIP fails its SHA-256 checksum.",
        "verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT",
        "gh release edit $env:RELEASE_TAG --repo $env:GITHUB_REPOSITORY --draft=false",
        "GitHub release remained a draft after publication request.",
    )
    for token in required_tokens:
        require(token in text, "V25 release publication integrity guard missing token: " + token)

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

    create_pos = text.find("& gh @createArgs")
    tag_pos = text.find("Release tag does not target exact qualified workflow SHA; release remains a draft.", create_pos)
    download_pos = text.find("gh release download $env:RELEASE_TAG", tag_pos)
    set_pos = text.find("Draft release asset set mismatch.", download_pos)
    hash_pos = text.find("Draft release asset SHA-256 mismatch for $name; release remains a draft.", set_pos)
    checksum_pos = text.find("Downloaded draft ZIP fails its SHA-256 checksum.", hash_pos)
    signature_pos = text.find("verify-v25-signatures.ps1 -Path $payload -ExpectedThumbprint $env:QS3D_SIGNING_CERT_THUMBPRINT", checksum_pos)
    publish_pos = text.find("gh release edit $env:RELEASE_TAG --repo $env:GITHUB_REPOSITORY --draft=false", signature_pos)
    positions = (create_pos, tag_pos, download_pos, set_pos, hash_pos, checksum_pos, signature_pos, publish_pos)
    require(min(positions) >= 0 and list(positions) == sorted(positions), "V25 release must create draft -> bind exact tag -> download exact asset set -> hash/checksum/signature verify -> publish")

    print("PASS: V25 commercial publication remains draft-first, binds the remote tag to the exact workflow SHA, re-downloads the exact asset set, verifies SHA-256/checksum/Authenticode, and only then publishes.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print("FAIL:", exc)
        raise SystemExit(1)
