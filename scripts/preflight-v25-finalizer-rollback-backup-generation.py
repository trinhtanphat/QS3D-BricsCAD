#!/usr/bin/env python3
"""Require V25 rollback sources/destinations to be re-admitted at mutation time."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def ordered(section: str, *tokens: str) -> bool:
    cursor = -1
    for token in tokens:
        cursor = section.find(token, cursor + 1)
        if cursor < 0:
            return False
    return True


def main() -> int:
    source = SCRIPT.read_text(encoding="utf-8")
    failures: list[str] = []

    catch_start = source.find("catch {\n    $originalError = $_")
    finally_start = source.find("\nfinally {", catch_start if catch_start >= 0 else 0)
    if catch_start < 0 or finally_start < 0:
        failures.append("could not isolate V25 transaction rollback block")
        rollback = ""
    else:
        rollback = source[catch_start:finally_start]

    zip_contract = (
        "$zipBackup = Assert-SafeFile -Path $zipBackup -Label 'original PackageZip rollback backup'",
        "[IO.File]::Replace($zipBackup, $zip, $zipRollbackDiscard, $true)",
    )
    if rollback and not ordered(rollback, *zip_contract):
        failures.append("existing finalized-ZIP rollback generation admission regressed")

    manifest_contract = (
        "if ($manifestDetached -and (Test-Path -LiteralPath $manifestBackup))",
        "$manifestBackup = Assert-SafeFile -Path $manifestBackup -Label 'original SHA256SUMS.txt rollback backup'",
        "$hashManifest = Assert-SafeOptionalFileTarget -Path $hashManifest -Label 'SHA256SUMS.txt rollback target'",
        "[IO.File]::Move($manifestBackup, $hashManifest)",
    )
    if rollback and not ordered(rollback, *manifest_contract):
        failures.append(
            "manifest rollback must re-admit the exact backup source and destination immediately before Move"
        )

    metadata_contract = (
        "if ($metadataPublished -and (Test-Path -LiteralPath $metadataBackup))",
        "$metadataBackup = Assert-SafeFile -Path $metadataBackup -Label 'original PACKAGE-METADATA.json rollback backup'",
        "$metadataPath = Assert-SafeFile -Path $metadataPath -Label 'PACKAGE-METADATA.json rollback target'",
        "[IO.File]::Replace($metadataBackup, $metadataPath, $metadataRollbackDiscard, $true)",
    )
    if rollback and not ordered(rollback, *metadata_contract):
        failures.append(
            "metadata rollback must re-admit the exact backup source and destination immediately before Replace"
        )

    forbidden_unguarded = (
        "try { [IO.File]::Move($manifestBackup, $hashManifest) }",
        "try {\n            [IO.File]::Replace($metadataBackup, $metadataPath, $metadataRollbackDiscard, $true)",
    )
    for token in forbidden_unguarded:
        if token in rollback:
            failures.append(f"pathname-only rollback restore remains present: {token.splitlines()[-1]}")

    if "throw $originalError" not in rollback:
        failures.append("successful rollback must preserve and rethrow the original finalization failure")
    if "$rollbackErrors.Add(" not in rollback:
        failures.append("rollback failures must remain aggregated rather than replacing prior rollback diagnostics")
    if "continue-on-error" in source.lower():
        failures.append("V25 finalizer rollback safety must not hide errors")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: V25 metadata/manifest rollback restores re-admit source and destination generations at the destructive boundary")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
