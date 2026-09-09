#!/usr/bin/env python3
"""Guard V26 package SHA-256 binding to the exact creator-held ZIP generation."""

from __future__ import annotations

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "scripts" / "package-v26.ps1"

REQUIRED = (
    "$destinationStream.Flush($true)",
    "$destinationStream.Position = 0",
    "[Security.Cryptography.SHA256]::Create()",
    "$packageHash = $sha256.ComputeHash($destinationStream)",
    "Set-PackageOutputDeleteDisposition -Stream $destinationStream -Delete $false",
    "return ([Convert]::ToHexString($packageHash))",
    "$zipHash = New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
)

FORBIDDEN_FINAL_REOPEN = "$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()"


def validate(source: str) -> None:
    missing = [token for token in REQUIRED if token not in source]
    if missing:
        raise AssertionError("missing held-generation package hash contract: " + ", ".join(missing))
    if FORBIDDEN_FINAL_REOPEN in source:
        raise AssertionError("final V26 package digest must not be established by reopening the ZIP pathname")

    flush = source.index("$destinationStream.Flush($true)")
    rewind = source.index("$destinationStream.Position = 0", flush)
    create_hash = source.index("[Security.Cryptography.SHA256]::Create()", rewind)
    compute = source.index("$packageHash = $sha256.ComputeHash($destinationStream)", create_hash)
    clear_rollback = source.index("Set-PackageOutputDeleteDisposition -Stream $destinationStream -Delete $false", compute)
    returned = source.index("return ([Convert]::ToHexString($packageHash))", clear_rollback)
    dispose = source.index("$destinationStream.Dispose()", returned)
    if not (flush < rewind < create_hash < compute < clear_rollback < returned < dispose):
        raise AssertionError("held ZIP hash must occur after durable flush and before rollback clear/creator close")

    call = source.index("$zipHash = New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc")
    host = source.index('Write-Host "SHA256: $zipHash"', call)
    if call >= host:
        raise AssertionError("exact-generation digest must flow directly from package creation to reported SHA256")


def main() -> int:
    source = TARGET.read_text(encoding="utf-8")
    validate(source)

    for token in REQUIRED:
        mutated = source.replace(token, "__REMOVED_HELD_HASH_PRIMITIVE__", 1)
        if mutated == source:
            raise AssertionError(f"mutation setup failed for token: {token}")
        try:
            validate(mutated)
        except AssertionError:
            pass
        else:
            raise AssertionError(f"guard accepted mutation removing: {token}")

    unsafe = source.replace(
        "$zipHash = New-DeterministicPackageZip -PackageRoot $dist -DestinationPath $zip -SourceTimestamp $sourceTimestampUtc",
        "$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()",
        1,
    )
    try:
        validate(unsafe)
    except AssertionError:
        pass
    else:
        raise AssertionError("guard accepted pathname-reopened final package hashing")

    print("PASS: V26 package SHA-256 is bound to the exact creator-held ZIP generation")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
