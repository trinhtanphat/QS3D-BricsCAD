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
    "Test-PinnedMsiGeneration -Path $staging",
    "Assert-NoExistingReparseComponent -Path $msi -Label 'MsiPath before atomic publication'",
    "[IO.File]::Move($staging, $msi)",
    "Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'",
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
    staged_admission = text.find("Test-PinnedMsiGeneration -Path $staging")
    publish = text.find("[IO.File]::Move($staging, $msi)")
    published_admission = text.find("Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'")
    if not (0 <= download < staged_admission < publish < published_admission):
        failures.append("remote MSI must be staged, held-verified, atomically published, then held-verified again")

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
        "Test-PinnedMsiGeneration -Path $staging",
        "[IO.File]::Move($staging, $msi)",
        "Test-PinnedMsiGeneration -Path $msi -Label 'Published BricsCAD V25 MSI'",
        "after Authenticode verification",
        "after Windows Installer metadata verification",
        "after administrative extraction",
        "Extracted V25 tree must not contain filesystem reparse points",
    )
    for token in mutation_tokens:
        mutated = text.replace(token, "MUTATED-STABLE-ACQUISITION-MARKER")
        if not validate(mutated):
            failures.append(f"guard mutation escaped detection: {token}")

    direct_download = text.replace(
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $staging",
        "Invoke-WebRequest -Uri $candidate.Url -OutFile $msi",
    )
    if not validate(direct_download):
        failures.append("guard mutation escaped direct-to-canonical MSI download")

    pathname_hash = text.replace(
        "$state = Open-PinnedMsiReadLock -Path $Path -ExpectedSha256 $expected",
        "$actual = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash",
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
    print(" - remote bytes are staged before atomic canonical publication")
    print(" - Authenticode, MSI metadata, and msiexec stay inside the final generation lock")
    print(" - extracted-tree discovery rejects reparse-backed traversal")
    return 0


if __name__ == "__main__":
    sys.exit(main())
