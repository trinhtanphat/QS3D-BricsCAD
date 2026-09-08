#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ACQUIRE = ROOT / "scripts/acquire-v25-compile-references.ps1"

REQUIRED = (
    "function Open-PinnedMsiReadLock",
    "function Test-PinnedMsiGeneration",
    "[IO.FileShare]::Read",
    "[Security.Cryptography.SHA256]::Create()",
    "$sha.ComputeHash($stream)",
    "'.qs3d-v25-msi-' + [Guid]::NewGuid().ToString('N') + '.tmp'",
    "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
    "$stagingAdmission = Open-PinnedMsiReadLock -Path $staging -ExpectedSha256 $expected",
    "Assert-NoExistingReparseComponent -Path $msi -Label 'MsiPath before held-generation publication'",
    "[IO.FileMode]::CreateNew",
    "[IO.FileOptions]::DeleteOnClose",
    "$stagingAdmission.Stream.CopyTo($publishedStream)",
    "$publishedStream.Flush($true)",
    "$publishedStream.Position = 0",
    "$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)",
    "[string]::Equals($publishedHash, [string]$stagingAdmission.Sha256",
    "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false",
    "$publishedByThisAttempt = $false",
    "$publishedStream.Dispose()",
    "$publishedAdmission = Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected",
    "Assert-PinnedMsiStable -State $publishedAdmission -Label 'immediately after held-generation publication commit'",
    "$stagingAdmission.Stream.Dispose()",
    "Remove-Item -LiteralPath $staging -Force -ErrorAction SilentlyContinue",
    "function Assert-PinnedMsiStable",
    "before Authenticode verification",
    "after Authenticode verification",
    "after Windows Installer metadata verification",
    "immediately before administrative extraction",
    "after administrative extraction",
    "$msiState.Stream.Dispose()",
    "function Get-OrdinaryFilesByNameUnderRoot",
    "Extracted V25 tree must not contain filesystem reparse points",
    "Get-OrdinaryFilesByNameUnderRoot -Root $extract -Name 'BrxMgd.dll'",
)

FORBIDDEN = (
    "Get-FileHash",
    "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi",
    "[IO.File]::Move($staging, $msi)",
    "Remove-Item -LiteralPath $msi -Force",
    "[IO.File]::Delete($msi)",
    "Get-ChildItem -LiteralPath $extract -Recurse -File -Filter 'BrxMgd.dll'",
)


def validate(text: str) -> list[str]:
    failures: list[str] = []
    for token in REQUIRED:
        if token not in text:
            failures.append(f"missing stable-acquisition contract marker: {token}")
    for token in FORBIDDEN:
        if token in text:
            failures.append(f"unsafe acquisition path/traversal marker remains: {token}")

    download = text.find("Invoke-WebRequest -Uri $candidate.Url -OutFile $staging")
    staged_admission = text.find("$stagingAdmission = Open-PinnedMsiReadLock -Path $staging -ExpectedSha256 $expected")
    fresh_publish = text.find("[IO.FileMode]::CreateNew", staged_admission)
    delete_on_close = text.find("[IO.FileOptions]::DeleteOnClose", fresh_publish)
    copy_from_held = text.find("$stagingAdmission.Stream.CopyTo($publishedStream)", delete_on_close)
    durable_flush = text.find("$publishedStream.Flush($true)", copy_from_held)
    rewind = text.find("$publishedStream.Position = 0", durable_flush)
    same_handle_hash = text.find("$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)", rewind)
    digest_match = text.find("[string]::Equals($publishedHash, [string]$stagingAdmission.Sha256", same_handle_hash)
    commit = text.find("Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false", digest_match)
    ownership_clear = text.find("$publishedByThisAttempt = $false", commit)
    creator_dispose = text.find("$publishedStream.Dispose()", ownership_clear)
    published_admission = text.find("$publishedAdmission = Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected", creator_dispose)
    staged_dispose = text.find("$stagingAdmission.Stream.Dispose()", published_admission)
    if not (
        0 <= download < staged_admission < fresh_publish < delete_on_close < copy_from_held < durable_flush
        < rewind < same_handle_hash < digest_match < commit < ownership_clear < creator_dispose
        < published_admission < staged_dispose
    ):
        failures.append(
            "remote MSI must stay held through delete-on-close publication, same-handle digest verification, explicit handle commit, and canonical re-admission"
        )

    lock = text.find("$msiState = Open-PinnedMsiReadLock")
    signature = text.find("Get-AuthenticodeSignature -FilePath $msiState.Path")
    metadata = text.find("$database = $installer.OpenDatabase($msiState.Path, 0)")
    extraction = text.find("Start-Process -FilePath msiexec.exe")
    dispose = text.find("$msiState.Stream.Dispose()")
    if not (0 <= lock < signature < metadata < extraction < dispose):
        failures.append("MSI read lock must span signature, metadata, and msiexec consumption")
    return failures


def main() -> int:
    text = ACQUIRE.read_text(encoding="utf-8")
    failures = validate(text)

    mutation_tokens = (
        "[IO.FileShare]::Read",
        "$sha.ComputeHash($stream)",
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
        "$stagingAdmission = Open-PinnedMsiReadLock -Path $staging -ExpectedSha256 $expected",
        "[IO.FileMode]::CreateNew",
        "[IO.FileOptions]::DeleteOnClose",
        "$stagingAdmission.Stream.CopyTo($publishedStream)",
        "$publishedStream.Flush($true)",
        "$publishedStream.Position = 0",
        "$publishedHashBytes = $publishedSha.ComputeHash($publishedStream)",
        "[string]::Equals($publishedHash, [string]$stagingAdmission.Sha256",
        "Set-OwnedMsiDeleteDisposition -Stream $publishedStream -Delete $false",
        "$publishedByThisAttempt = $false\n            $publishedStream.Dispose()",
        "$publishedAdmission = Open-PinnedMsiReadLock -Path $msi -ExpectedSha256 $expected",
        "Assert-PinnedMsiStable -State $publishedAdmission -Label 'immediately after held-generation publication commit'",
        "after Authenticode verification",
        "after Windows Installer metadata verification",
        "after administrative extraction",
        "Extracted V25 tree must not contain filesystem reparse points",
    )
    for token in mutation_tokens:
        mutated = text.replace(token, "MUTATED-STABLE-ACQUISITION-MARKER", 1)
        if not validate(mutated):
            failures.append(f"guard mutation escaped detection: {token}")

    direct_download = text.replace(
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi",
        1,
    )
    if not validate(direct_download):
        failures.append("guard mutation escaped direct-to-canonical MSI download")

    pathname_publish = text.replace(
        "$stagingAdmission.Stream.CopyTo($publishedStream)",
        "[IO.File]::Move($staging, $msi)",
        1,
    )
    if not validate(pathname_publish):
        failures.append("guard mutation escaped pathname File.Move publication")

    pathname_hash = text.replace(
        "$state = Open-PinnedMsiReadLock -Path $Path -ExpectedSha256 $expected",
        "$actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash",
        1,
    )
    if not validate(pathname_hash):
        failures.append("guard mutation escaped pathname-based MSI hashing")

    if failures:
        print("V25 compile-reference acquisition stability preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V25 compile-reference MSI admission and trust stay generation-bound.")
    print(" - cache and staged downloads are hashed through held read generations")
    print(" - canonical publication stays delete-on-close and handle-owned until same-handle digest verification commits it")
    print(" - Authenticode, MSI metadata, and msiexec stay inside the final generation lock")
    print(" - extracted-tree discovery rejects reparse-backed traversal")
    return 0


if __name__ == "__main__":
    sys.exit(main())
