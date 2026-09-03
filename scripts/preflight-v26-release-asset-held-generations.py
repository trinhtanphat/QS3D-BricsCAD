#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PUBLISHER = ROOT / "scripts/publish-v26-release.ps1"
HELPER = ROOT / "scripts/verify-v26-held-file.ps1"
errors = []


def check(publisher: str, helper: str):
    found = []
    helper_tokens = (
        "[ValidateSet('Hash', 'Copy')]",
        "Assert-NoReparseAncestor",
        "[IO.FileAttributes]::ReparsePoint",
        "$admittedLength = [int64]$admitted.Length",
        "$admittedWriteTicks = [int64]$admitted.LastWriteTimeUtc.Ticks",
        "[IO.FileShare]::Read",
        "$rebound = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop",
        "[int64]$stream.Length -ne $admittedLength",
        "[Security.Cryptography.SHA256]::Create()",
        "$sha.ComputeHash($held.Stream)",
        "[IO.FileMode]::CreateNew",
        "$held.Stream.CopyTo($output)",
        "$output.Flush($true)",
    )
    for token in helper_tokens:
        if token not in helper:
            found.append("V26 held release verifier missing generation-binding token: " + token)

    required_publisher = (
        "verify-v26-held-file.ps1 -Operation Hash -Path $localAsset",
        "verify-v26-held-file.ps1 -Operation Hash -Path $downloadedAsset",
        "Uploaded V26 release asset size mismatch",
        "Uploaded V26 release asset SHA-256 mismatch",
        "Assert-RemoteReleaseTagTargetsWorkflowSha",
        "$published = Invoke-RestMethod -Method Patch",
    )
    for token in required_publisher:
        if token not in publisher:
            found.append("V26 release publisher missing held-asset contract: " + token)

    for forbidden in (
        "Get-FileHash -LiteralPath $localAsset",
        "Get-FileHash -LiteralPath $downloadedAsset",
    ):
        if forbidden in publisher:
            found.append("V26 release publisher must not reopen release assets by pathname for hashing: " + forbidden)

    size = publisher.find("Uploaded V26 release asset size mismatch")
    download = publisher.find("-OutFile $downloadedAsset", size + 1)
    local_hash = publisher.find("verify-v26-held-file.ps1 -Operation Hash -Path $localAsset", download + 1)
    remote_hash = publisher.find("verify-v26-held-file.ps1 -Operation Hash -Path $downloadedAsset", local_hash + 1)
    hash_compare = publisher.find("Uploaded V26 release asset SHA-256 mismatch", remote_hash + 1)
    second_tag = publisher.find("Assert-RemoteReleaseTagTargetsWorkflowSha", hash_compare + 1)
    publish = publisher.find("$published = Invoke-RestMethod -Method Patch", second_tag + 1)
    if min(size, download, local_hash, remote_hash, hash_compare, second_tag, publish) < 0 or not (
        size < download < local_hash < remote_hash < hash_compare < second_tag < publish
    ):
        found.append("V26 release order must be size -> download -> held local hash -> held remote hash -> hash compare -> tag/SHA recheck -> publish")
    return found

if not PUBLISHER.is_file():
    errors.append("missing V26 release publisher")
if not HELPER.is_file():
    errors.append("missing V26 held release verifier")
if not errors:
    publisher_text = PUBLISHER.read_text(encoding="utf-8")
    helper_text = HELPER.read_text(encoding="utf-8")
    errors.extend(check(publisher_text, helper_text))

    # Deterministic negative probes: pathname hashing and weakened held-open semantics must fail closed.
    if not check(
        publisher_text.replace(
            "verify-v26-held-file.ps1 -Operation Hash -Path $localAsset",
            "Get-FileHash -LiteralPath $localAsset -Algorithm SHA256",
            1,
        ),
        helper_text,
    ):
        errors.append("mutation probe accepted pathname reopening for local release asset hash")
    if not check(publisher_text, helper_text.replace("[IO.FileShare]::Read", "[IO.FileShare]::Write", 1)):
        errors.append("mutation probe accepted writable sharing for held release generation")
    if not check(publisher_text, helper_text.replace("Assert-NoReparseAncestor", "Assert-Ancestor")):
        errors.append("mutation probe accepted loss of reparse-ancestor rejection")

print("QS3D V26 release asset held-generation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    raise SystemExit(1)
print("PASS: V26 draft-release publisher hashes one admitted ordinary held generation and preserves publish-last tag/SHA ordering; mutation probes reject pathname reopening and weakened held-file admission.")
