#!/usr/bin/env python3
"""Require the finalized V25 ZIP publication to remain bound to the verified staged file generation."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def main() -> int:
    source = FINALIZER.read_text(encoding="utf-8")
    failures: list[str] = []

    verifier_start = source.find("function Assert-ZipManifestIntegrity")
    verifier_end = source.find("\nfunction ", verifier_start + 1) if verifier_start >= 0 else -1
    if verifier_start >= 0 and verifier_end < 0:
        verifier_end = len(source)
    verifier = source[verifier_start:verifier_end] if verifier_start >= 0 else ""

    held_helper_start = source.find("function Publish-HeldVerifiedZipGeneration")
    held_helper_end = source.find("\nfunction ", held_helper_start + 1) if held_helper_start >= 0 else -1
    if held_helper_start >= 0 and held_helper_end < 0:
        held_helper_end = len(source)
    held_helper = source[held_helper_start:held_helper_end] if held_helper_start >= 0 else ""

    manifest_verify = source.find("$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip")
    held_open = source.find("$heldZip = Open-HeldVerifiedZipGeneration -Path $tempZip -ExpectedSha256 $stagedZipHash")
    publish_call = source.find("Publish-HeldVerifiedZipGeneration -HeldZip $heldZip -TargetPath $zip -ReplaceIfExists $zipExistedBeforePublish")
    installed_hash = source.find("$installedZipHash = [string]$heldZip.Sha256")
    mismatch_check = source.find("[string]::Equals($installedZipHash, $stagedZipHash, [StringComparison]::Ordinal)")
    committed = source.find("$transactionCommitted = $true", max(publish_call, 0))

    required = (
        "$zipPublished = $false",
        "$zipExistedBeforePublish = $false",
        "$zipRollbackDiscard",
        "$stagedZipHash = Assert-ZipManifestIntegrity -ZipPath $tempZip",
        "$heldZip = Open-HeldVerifiedZipGeneration -Path $tempZip -ExpectedSha256 $stagedZipHash",
        "function Assert-HeldVerifiedZipStable",
        "function Publish-HeldVerifiedZipGeneration",
        "[QS3DV25HeldZipPublication]::SetFileInformationByHandleFileRenameInfo(",
        "$installedZipHash = [string]$heldZip.Sha256",
        "[string]::Equals($installedZipHash, $stagedZipHash, [StringComparison]::Ordinal)",
        "Finalized ZIP generation mismatch",
        "$heldZip.Stream.Dispose()",
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

    held_required = (
        "$expectedHash = [string]$HeldZip.Sha256",
        "Assert-HeldVerifiedZipStable -HeldZip $HeldZip",
        "[QS3DV25HeldZipPublication]::SetFileInformationByHandleFileRenameInfo(",
        "$HeldZip.Stream.SafeFileHandle",
        "Assert-HeldVerifiedZipStable -HeldZip $HeldZip",
        "Held staged ZIP admitted identity changed across publication",
    )
    for token in held_required:
        if token not in held_helper:
            failures.append(f"held publication helper is incomplete; missing: {token}")

    for forbidden in (
        "Get-FileHash -LiteralPath $tempZip",
        "[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)",
        "[IO.File]::Move($tempZip, $zip)",
        "$installedZipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()",
    ):
        if forbidden in source:
            failures.append("generation-bound publication must not reopen/publish by pathname; forbidden: " + forbidden)

    if min(manifest_verify, held_open, publish_call, installed_hash, mismatch_check, committed) < 0:
        failures.append("could not bound same-generation verify/hold/publish/commit sequence")
    elif not (manifest_verify < held_open < publish_call < installed_hash < mismatch_check < committed):
        failures.append("finalization must obtain same-handle verified digest, hold that generation, publish its handle, compare admitted identity, then commit")

    catch_start = source.find("catch {", source.find("try {", source.find("$transactionCommitted = $false")))
    finally_start = source.find("\nfinally {", catch_start)
    catch_body = source[catch_start:finally_start] if catch_start >= 0 and finally_start > catch_start else ""
    rollback_required = (
        "if ($zipPublished)",
        "if ($zipExistedBeforePublish",
        "$zip = Assert-SafeOptionalFileTarget -Path $zip -Label 'PackageZip rollback target'",
        "$zipBackup = Assert-SafeFile -Path $zipBackup -Label 'original PackageZip rollback backup'",
        "[IO.File]::Replace($zipBackup, $zip, $zipRollbackDiscard, $true)",
        "Remove-Item -LiteralPath $zip -Force -ErrorAction Stop",
    )
    for token in rollback_required:
        if token not in catch_body:
            failures.append(f"publication failure cannot restore/remove the published ZIP safely; missing: {token}")

    if "continue-on-error" in source.lower():
        failures.append("finalizer must not hide generation-binding failures with continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: finalized V25 ZIP remains bound to one held verified generation through native handle publication and commit")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
