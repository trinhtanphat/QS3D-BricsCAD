#!/usr/bin/env python3
"""Guard finalized V25 ZIPs against manifest/archive byte drift."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def main() -> int:
    source = FINALIZER.read_text(encoding="utf-8")
    failures: list[str] = []

    function_start = source.find("function Assert-ZipManifestIntegrity")
    call_site = source.find("Assert-ZipManifestIntegrity -ZipPath $tempZip")
    zip_shape_check = source.find("Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package")
    zip_hash = source.find("$stagedZipHash = (Get-FileHash -LiteralPath $tempZip -Algorithm SHA256)")

    required = (
        "function Assert-ZipManifestIntegrity",
        "[IO.Compression.ZipFile]::OpenRead($ZipPath)",
        "SHA256SUMS.txt",
        "$entry.Open()",
        "[Security.Cryptography.SHA256]::Create()",
        "^([0-9A-F]{64})  (.+)$",
        "case-insensitive duplicate",
        "checksum manifest coverage mismatch",
        "checksum mismatch",
        "Assert-ZipManifestIntegrity -ZipPath $tempZip",
    )
    for token in required:
        if token not in source:
            failures.append(f"finalized ZIP byte-integrity contract is incomplete; missing: {token}")

    if min(function_start, call_site, zip_shape_check, zip_hash) < 0:
        failures.append("could not bound finalized ZIP shape/manifest/hash validation")
    elif not (zip_shape_check < call_site < zip_hash):
        failures.append("completed ZIP must pass shape then manifest-entry byte validation before its outer digest is admitted")

    if function_start >= 0:
        function_end = source.find("\nfunction ", function_start + 1)
        if function_end < 0:
            function_end = len(source)
        verifier = source[function_start:function_end]
        verifier_required = (
            "$manifestEntries.Count -ne 1",
            "$seenManifestPaths",
            "$archivePayloadPaths",
            "$manifestPayloadPaths",
            "$hash.ComputeHash($stream)",
            "$manifestEntry.Length -gt 4MB",
            "must not hash itself",
        )
        for token in verifier_required:
            if token not in verifier:
                failures.append(f"ZIP manifest verifier is not fail-closed enough; missing: {token}")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: finalized V25 ZIP validates actual entry bytes against exact embedded SHA256SUMS coverage")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
