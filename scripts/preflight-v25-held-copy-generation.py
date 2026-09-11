#!/usr/bin/env python3
"""Fail closed unless V25 held-file Copy pins and verifies the exact destination generation."""

from __future__ import annotations
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "scripts" / "verify-v25-held-file.ps1"


def contract_errors(text: str | None) -> list[str]:
    errors: list[str] = []
    if text is None:
        return ["missing scripts/verify-v25-held-file.ps1"]

    required = (
        ("FILE_FLAG_OPEN_REPARSE_POINT", "native no-follow directory open"),
        ("FILE_FLAG_BACKUP_SEMANTICS", "native directory handle open"),
        ("FILE_SHARE_READ | FILE_SHARE_WRITE", "directory holds that permit I/O while denying delete/rename sharing"),
        ("GetFileInformationByHandle", "handle-bound directory attributes"),
        ("GetFinalPathNameByHandleW", "handle-bound final path identity"),
        ("OpenDirectoryNoFollow", "no-follow directory open helper"),
        ("Open-HeldDestinationDirectoryChain", "destination ancestor generation holds"),
        ("[IO.FileMode]::CreateNew", "no-clobber destination creation"),
        ("[IO.FileAccess]::ReadWrite", "read/write exact destination stream"),
        ("$sourceDigest = Get-HeldStreamSha256 -Stream $held.Stream", "source exact-stream digest before copy"),
        ("$held.Stream.Position = 0", "source rewind before copy"),
        ("$held.Stream.CopyTo($output)", "copy from admitted source generation"),
        ("$output.Flush($true)", "durable destination write before verification"),
        ("$destinationDigest = Get-HeldStreamSha256 -Stream $output", "destination exact-stream digest"),
        ("[string]::Equals($sourceDigest, $destinationDigest, [StringComparison]::OrdinalIgnoreCase)", "source/destination exact-stream digest comparison"),
        ("Publish-CommercialZipDigest -CanonicalPath $held.CanonicalPath -Digest $sourceDigest", "publish only the digest proven equal to destination bytes"),
        ("$destinationHolds[$i].Dispose()", "destination hold disposal after verification"),
    )
    for token, label in required:
        if token not in text:
            errors.append(f"held-copy generation contract missing {label}: {token}")

    forbidden = (
        ("FILE_SHARE_DELETE", "destination directory holds must deny delete/rename sharing"),
        ("Get-FileHash", "held-copy verification must not reopen either pathname for hashing"),
        ("Remove-Item -LiteralPath $destinationFull", "failure cleanup must not mutate a potentially substituted destination pathname"),
    )
    for token, label in forbidden:
        if token in text:
            errors.append(f"held-copy generation contract violation: {label}: {token}")

    hold = text.find("$destinationHolds = Open-HeldDestinationDirectoryChain")
    source_hash = text.find("$sourceDigest = Get-HeldStreamSha256 -Stream $held.Stream", hold)
    create = text.find("[IO.File]::Open($destinationFull, [IO.FileMode]::CreateNew", source_hash)
    copy = text.find("$held.Stream.CopyTo($output)", create)
    flush = text.find("$output.Flush($true)", copy)
    destination_hash = text.find("$destinationDigest = Get-HeldStreamSha256 -Stream $output", flush)
    compare = text.find("[string]::Equals($sourceDigest, $destinationDigest, [StringComparison]::OrdinalIgnoreCase)", destination_hash)
    publish = text.find("Publish-CommercialZipDigest -CanonicalPath $held.CanonicalPath -Digest $sourceDigest", compare)
    # Scope disposal ordering to the Copy critical section. The helper legitimately disposes
    # partially-acquired handles in Open-HeldDestinationDirectoryChain's error path before the
    # Copy operation appears in the file; that cleanup must not be mistaken for an early release.
    dispose = text.find("$destinationHolds[$i].Dispose()", publish)
    if not (0 <= hold < source_hash < create < copy < flush < destination_hash < compare < publish < dispose):
        errors.append("destination ancestors must stay pinned from before CreateNew through exact destination-stream digest equality and ZIP-digest publication")

    return errors


def self_test() -> list[str]:
    safe = r'''FILE_FLAG_OPEN_REPARSE_POINT FILE_FLAG_BACKUP_SEMANTICS
FILE_SHARE_READ | FILE_SHARE_WRITE
GetFileInformationByHandle GetFinalPathNameByHandleW OpenDirectoryNoFollow
function Open-HeldDestinationDirectoryChain {
    # Legitimate cleanup of partially-acquired handles is outside the Copy critical section.
    $destinationHolds[$i].Dispose()
}
$destinationHolds = Open-HeldDestinationDirectoryChain
$sourceDigest = Get-HeldStreamSha256 -Stream $held.Stream
$held.Stream.Position = 0
$output = [IO.File]::Open($destinationFull, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
$held.Stream.CopyTo($output)
$output.Flush($true)
$destinationDigest = Get-HeldStreamSha256 -Stream $output
if (-not [string]::Equals($sourceDigest, $destinationDigest, [StringComparison]::OrdinalIgnoreCase)) { throw 'mismatch' }
Publish-CommercialZipDigest -CanonicalPath $held.CanonicalPath -Digest $sourceDigest
$destinationHolds[$i].Dispose()
'''
    errors: list[str] = []
    if contract_errors(safe):
        errors.append("guard rejected intended held-copy generation contract")

    mutants = {
        "missing destination holds": safe.replace("$destinationHolds = Open-HeldDestinationDirectoryChain", "$destinationHolds = @()", 1),
        "delete sharing": safe + "\nFILE_SHARE_DELETE",
        "clobber": safe.replace("[IO.FileMode]::CreateNew", "[IO.FileMode]::Create", 1),
        "write-only destination": safe.replace("[IO.FileAccess]::ReadWrite", "[IO.FileAccess]::Write", 1),
        "missing destination digest": safe.replace("$destinationDigest = Get-HeldStreamSha256 -Stream $output", "$destinationDigest = $sourceDigest", 1),
        "missing digest equality": safe.replace("[string]::Equals($sourceDigest, $destinationDigest, [StringComparison]::OrdinalIgnoreCase)", "$true", 1),
        "pathname hash": safe + "\nGet-FileHash -LiteralPath $destinationFull",
        "unsafe cleanup": safe + "\nRemove-Item -LiteralPath $destinationFull -Force",
        "early hold release": safe.replace("$output = [IO.File]::Open", "$destinationHolds[$i].Dispose()\n$output = [IO.File]::Open", 1),
    }
    for label, mutant in mutants.items():
        if not contract_errors(mutant):
            errors.append(f"guard failed to reject mutant: {label}")
    return errors


def main() -> int:
    errors = self_test()
    try:
        helper = HELPER.read_text(encoding="utf-8")
    except OSError:
        helper = None
    errors.extend(contract_errors(helper))
    if errors:
        for error in errors:
            print("FAIL:", error)
        return 1
    print("PASS: V25 held-file Copy pins every destination ancestor and verifies the exact created destination stream before admission is published.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
