#!/usr/bin/env python3
"""Require finalized V25 ZIP generation and transaction pathname binding."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def require_before(source: str, left: str, right: str, label: str, failures: list[str]) -> None:
    left_pos = source.find(left)
    right_pos = source.find(right)
    if left_pos < 0:
        failures.append(f"{label}; missing admission: {left}")
    elif right_pos < 0:
        failures.append(f"{label}; missing mutation: {right}")
    elif left_pos >= right_pos:
        failures.append(f"{label}; pathname admission must occur immediately before the mutation sequence")


def main() -> int:
    source = FINALIZER.read_text(encoding="utf-8")
    failures: list[str] = []

    verifier_start = source.find("function Assert-ZipManifestIntegrity")
    verifier_end = source.find("\nfunction ", verifier_start + 1) if verifier_start >= 0 else -1
    if verifier_start >= 0 and verifier_end < 0:
        verifier_end = len(source)
    verifier = source[verifier_start:verifier_end] if verifier_start >= 0 else ""

    manifest_verify = source.find("$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip")
    replace_call = source.find("[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)")
    move_call = source.find("[IO.File]::Move($tempZip, $zip)")
    installed_hash = source.find("$installedZipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()")
    mismatch_check = source.find("[string]::Equals($installedZipHash, $stagedZipHash, [StringComparison]::Ordinal)")
    committed = source.find("$transactionCommitted = $true", max(replace_call, move_call, 0))

    required = (
        "$zipPublished = $false",
        "$zipExistedBeforePublish = $false",
        "$zipRollbackDiscard",
        "$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip",
        "$installedZipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()",
        "[string]::Equals($installedZipHash, $stagedZipHash, [StringComparison]::Ordinal)",
        "Finalized ZIP generation mismatch",
    )
    for token in required:
        if token not in source:
            failures.append(f"final ZIP generation-binding contract is incomplete; missing: {token}")

    verifier_required = (
        "$fileStream = [IO.FileStream]::new(",
        "[IO.FileMode]::Open",
        "[IO.FileAccess]::Read",
        "[IO.FileShare]::Read",
        "[IO.Compression.ZipArchive]::new($fileStream",
        "$fileStream.Position = 0",
        "$outerHash = [Security.Cryptography.SHA256]::Create()",
        "$outerDigest = $outerHash.ComputeHash($fileStream)",
        "return (-join ($outerDigest | ForEach-Object { $_.ToString('X2') }))",
    )
    for token in verifier_required:
        if token not in verifier:
            failures.append(f"manifest validation must return the outer digest from the same locked file handle; missing: {token}")

    if "Get-FileHash -LiteralPath $tempZip" in source:
        failures.append("staged ZIP must not be reopened by pathname for its admitted digest after manifest verification")

    if min(manifest_verify, replace_call, move_call, installed_hash, mismatch_check, committed) < 0:
        failures.append("could not bound same-handle verify/install/reverify/commit sequence")
    else:
        if not (manifest_verify < replace_call < installed_hash < mismatch_check < committed):
            failures.append("existing-target finalization must obtain same-handle verified digest, replace, rehash destination, compare, then commit")
        if not (manifest_verify < move_call < installed_hash < mismatch_check < committed):
            failures.append("new-target finalization must obtain same-handle verified digest, move, rehash destination, compare, then commit")

    # Every mutable filesystem pathname that is handed to Move/Replace must be
    # rebound to an ordinary non-reparse file/target immediately in its phase.
    prepublish_start = source.find("try {", source.find("$transactionCommitted = $false"))
    catch_start = source.find("catch {", prepublish_start)
    publish_body = source[prepublish_start:catch_start] if prepublish_start >= 0 and catch_start > prepublish_start else ""
    publish_required = (
        "$metadataBackup = Assert-SafeOptionalFileTarget -Path $metadataBackup -Label 'PACKAGE-METADATA rollback backup target'",
        "$manifestBackup = Assert-SafeOptionalFileTarget -Path $manifestBackup -Label 'checksum manifest rollback backup target'",
        "$zipBackup = Assert-SafeOptionalFileTarget -Path $zipBackup -Label 'PackageZip rollback backup target'",
    )
    for token in publish_required:
        if token not in publish_body:
            failures.append(f"publish transaction pathname is not rebound immediately before mutation; missing: {token}")

    require_before(
        publish_body,
        "$metadataBackup = Assert-SafeOptionalFileTarget -Path $metadataBackup -Label 'PACKAGE-METADATA rollback backup target'",
        "[IO.File]::Replace($metadataStage, $metadataPath, $metadataBackup, $true)",
        "metadata backup admission",
        failures,
    )
    require_before(
        publish_body,
        "$manifestBackup = Assert-SafeOptionalFileTarget -Path $manifestBackup -Label 'checksum manifest rollback backup target'",
        "[IO.File]::Move($hashManifest, $manifestBackup)",
        "manifest backup admission",
        failures,
    )
    require_before(
        publish_body,
        "$zipBackup = Assert-SafeOptionalFileTarget -Path $zipBackup -Label 'PackageZip rollback backup target'",
        "[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)",
        "ZIP backup admission",
        failures,
    )

    finally_start = source.find("\nfinally {", catch_start)
    catch_body = source[catch_start:finally_start] if catch_start >= 0 and finally_start > catch_start else ""
    rollback_required = (
        "if ($zipPublished)",
        "if ($zipExistedBeforePublish",
        "$zip = Assert-SafeOptionalFileTarget -Path $zip -Label 'PackageZip rollback target'",
        "$zipBackup = Assert-SafeFile -Path $zipBackup -Label 'original PackageZip rollback backup'",
        "$zipRollbackDiscard = Assert-SafeOptionalFileTarget -Path $zipRollbackDiscard -Label 'PackageZip rollback discard target'",
        "[IO.File]::Replace($zipBackup, $zip, $zipRollbackDiscard, $true)",
        "$hashManifest = Assert-SafeOptionalFileTarget -Path $hashManifest -Label 'checksum manifest rollback target'",
        "$manifestBackup = Assert-SafeFile -Path $manifestBackup -Label 'original checksum manifest rollback backup'",
        "[IO.File]::Move($manifestBackup, $hashManifest)",
        "$metadataBackup = Assert-SafeFile -Path $metadataBackup -Label 'original PACKAGE-METADATA rollback backup'",
        "$metadataPath = Assert-SafeFile -Path $metadataPath -Label 'PACKAGE-METADATA rollback target'",
        "$metadataRollbackDiscard = Assert-SafeOptionalFileTarget -Path $metadataRollbackDiscard -Label 'PACKAGE-METADATA rollback discard target'",
        "[IO.File]::Replace($metadataBackup, $metadataPath, $metadataRollbackDiscard, $true)",
        "Remove-Item -LiteralPath $zip -Force -ErrorAction Stop",
    )
    for token in rollback_required:
        if token not in catch_body:
            failures.append(f"rollback transaction cannot restore/remove artifacts safely; missing: {token}")

    if "continue-on-error" in source.lower():
        failures.append("finalizer must not hide generation-binding failures with continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: finalized V25 ZIP binds one verified generation and revalidates every publish/rollback mutation operand")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
