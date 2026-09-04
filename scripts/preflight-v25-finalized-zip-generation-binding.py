#!/usr/bin/env python3
"""Require the finalized V25 ZIP installed at PackageZip to match the verified staged generation."""

from __future__ import annotations

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FINALIZER = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def main() -> int:
    source = FINALIZER.read_text(encoding="utf-8")
    failures: list[str] = []

    manifest_verify = source.find("Assert-ZipManifestIntegrity -ZipPath $tempZip")
    staged_hash = source.find("$stagedZipHash = (Get-FileHash -LiteralPath $tempZip -Algorithm SHA256).Hash.ToUpperInvariant()")
    replace_call = source.find("[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)")
    move_call = source.find("[IO.File]::Move($tempZip, $zip)")
    installed_hash = source.find("$installedZipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()")
    mismatch_check = source.find("[string]::Equals($installedZipHash, $stagedZipHash, [StringComparison]::Ordinal)")
    committed = source.find("$transactionCommitted = $true", max(replace_call, move_call, 0))

    required = (
        "$zipPublished = $false",
        "$zipExistedBeforePublish = $false",
        "$zipRollbackDiscard",
        "$installedZipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()",
        "[string]::Equals($installedZipHash, $stagedZipHash, [StringComparison]::Ordinal)",
        "Finalized ZIP generation mismatch",
    )
    for token in required:
        if token not in source:
            failures.append(f"final ZIP generation-binding contract is incomplete; missing: {token}")

    if min(manifest_verify, staged_hash, replace_call, move_call, installed_hash, mismatch_check, committed) < 0:
        failures.append("could not bound verify/hash/install/reverify/commit sequence")
    else:
        if not (manifest_verify < staged_hash < replace_call < installed_hash < mismatch_check < committed):
            failures.append("existing-target finalization must verify staged bytes, hash them, replace, rehash destination, compare, then commit")
        if not (manifest_verify < staged_hash < move_call < installed_hash < mismatch_check < committed):
            failures.append("new-target finalization must verify staged bytes, hash them, move, rehash destination, compare, then commit")

    catch_start = source.find("catch {", source.find("try {", source.find("$transactionCommitted = $false")))
    finally_start = source.find("\nfinally {", catch_start)
    catch_body = source[catch_start:finally_start] if catch_start >= 0 and finally_start > catch_start else ""
    rollback_required = (
        "if ($zipPublished)",
        "if ($zipExistedBeforePublish",
        "[IO.File]::Replace($zipBackup, $zip, $zipRollbackDiscard, $true)",
        "Remove-Item -LiteralPath $zip -Force -ErrorAction Stop",
    )
    for token in rollback_required:
        if token not in catch_body:
            failures.append(f"post-install verification failure cannot restore/remove the published ZIP safely; missing: {token}")

    if "continue-on-error" in source.lower():
        failures.append("finalizer must not hide generation-binding failures with continue-on-error")

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}", file=sys.stderr)
        return 1

    print("PASS: finalized V25 ZIP destination is rebound to the verified staged digest before transaction commit with rollback on mismatch")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
