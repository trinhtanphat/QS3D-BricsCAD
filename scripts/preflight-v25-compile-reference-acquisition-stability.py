#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
ACQUIRE = ROOT / "scripts/acquire-v25-compile-references.ps1"

REQUIRED = (
    "function Open-PinnedMsiReadLock",
    "[IO.FileShare]::Read",
    "[Security.Cryptography.SHA256]::Create()",
    "$sha.ComputeHash($stream)",
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
    "$actualHash = (Get-FileHash -LiteralPath $msi -Algorithm SHA256).Hash.ToUpperInvariant()",
    "Get-ChildItem -LiteralPath $extract -Recurse -File -Filter 'BrxMgd.dll'",
)


def validate(text: str) -> list[str]:
    failures: list[str] = []
    for token in REQUIRED:
        if token not in text:
            failures.append(f"missing stable-acquisition contract marker: {token}")
    for token in FORBIDDEN:
        if token in text:
            failures.append(f"unsafe post-admission path/traversal marker remains: {token}")
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

    for token in (
        "[IO.FileShare]::Read",
        "$sha.ComputeHash($stream)",
        "after Authenticode verification",
        "after Windows Installer metadata verification",
        "after administrative extraction",
        "Extracted V25 tree must not contain filesystem reparse points",
    ):
        mutated = text.replace(token, f"MUTATED-{token}", 1)
        if not validate(mutated):
            failures.append(f"guard mutation escaped detection: {token}")

    if failures:
        print("V25 compile-reference acquisition stability preflight FAILED")
        for failure in failures:
            print(" -", failure)
        return 1

    print("PASS: V25 compile-reference MSI trust is bound to one stable generation.")
    print(" - pinned digest is recomputed through the held read lock")
    print(" - Authenticode, MSI metadata, and msiexec stay inside the same generation lock")
    print(" - extracted-tree discovery rejects reparse-backed traversal")
    return 0


if __name__ == "__main__":
    sys.exit(main())
