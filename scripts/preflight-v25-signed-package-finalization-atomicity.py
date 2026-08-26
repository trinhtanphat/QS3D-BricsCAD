#!/usr/bin/env python3
"""Regression guard for bounded/failure-atomic V25 signed-package finalization."""

from __future__ import annotations

import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "finalize-v25-signed-package.ps1"


def require(source: str, token: str) -> int:
    index = source.find(token)
    if index < 0:
        raise AssertionError(f"missing finalization safety contract: {token}")
    return index


def assert_source_contract() -> None:
    source = SCRIPT.read_text(encoding="utf-8")

    if "Get-Content -LiteralPath $metadataPath -Raw" in source:
        raise AssertionError("PACKAGE-METADATA.json must not use unbounded Get-Content -Raw")
    if "if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }" in source:
        raise AssertionError("existing published ZIP must not be deleted before staged ZIP verification")

    max_bytes = require(source, "$MaxMetadataBytes = 1MB")
    bounded_read = require(source, "function Read-BoundedUtf8Text")
    file_stream = require(source, "[IO.FileStream]::new(")
    share_read = require(source, "[IO.FileShare]::Read")
    strict_utf8 = require(source, "[Text.UTF8Encoding]::new($false, $true)")
    read_call = require(
        source,
        "$metadataText = Read-BoundedUtf8Text -Path $metadataPath -MaxBytes $MaxMetadataBytes",
    )
    json_parse = require(source, "$metadataText | ConvertFrom-Json -ErrorAction Stop")
    if not (max_bytes < read_call < json_parse and bounded_read < file_stream < share_read):
        raise AssertionError("bounded metadata read must precede JSON materialization")
    if strict_utf8 > json_parse:
        raise AssertionError("strict UTF-8 decoding must be part of the bounded metadata reader")

    metadata_stage = require(source, "$metadataStage = New-SiblingTempPath")
    metadata_backup = require(source, "$metadataBackup = New-SiblingTempPath")
    temp_zip = require(source, "$tempZip = New-SiblingTempPath")
    metadata_replace = require(
        source, "[IO.File]::Replace($metadataStage, $metadataPath, $metadataBackup, $true)"
    )
    manifest_backup = require(source, "[IO.File]::Move($hashManifest, $manifestBackup)")
    manifest_stage = require(source, "[IO.File]::Move($manifestStage, $hashManifest)")
    compress = require(source, "Compress-Archive -Path (Join-Path $package '*') -DestinationPath $tempZip")
    verify_zip = require(source, "Assert-ZipMatchesPackage -ZipPath $tempZip -PackageRoot $package")
    publish_existing_zip = require(source, "[IO.File]::Replace($tempZip, $zip, $zipBackup, $true)")
    publish_new_zip = require(source, "[IO.File]::Move($tempZip, $zip)")
    committed = require(source, "$transactionCommitted = $true")

    if not (metadata_stage < metadata_backup < temp_zip < metadata_replace):
        raise AssertionError("all sibling stage/backup paths must be allocated before package mutation")
    if not (
        metadata_replace
        < manifest_backup
        < manifest_stage
        < compress
        < verify_zip
        < publish_existing_zip
        < committed
    ):
        raise AssertionError("finalizer must stage/verify all package state before commit")
    if not (verify_zip < publish_new_zip < committed):
        raise AssertionError("new ZIP publication must happen only after staged ZIP verification")

    rollback_manifest = require(source, "restore original manifest")
    rollback_metadata = require(source, "restore original metadata")
    rollback_failure = require(source, "Rollback also failed")
    if not (committed < rollback_manifest < rollback_metadata < rollback_failure):
        raise AssertionError("failed finalization must restore package metadata/manifest and fail closed")


def assert_failure_atomic_reference_model() -> None:
    """Pin the intended transaction property independently of PowerShell/runtime signing APIs."""
    with tempfile.TemporaryDirectory(prefix="qs3d-finalize-atomic-") as temp_dir:
        root = Path(temp_dir)
        metadata = root / "PACKAGE-METADATA.json"
        manifest = root / "SHA256SUMS.txt"
        archive = root / "QS3D-BricsCAD-V25.zip"
        metadata.write_bytes(b"old-metadata\n")
        manifest.write_bytes(b"old-manifest\n")
        archive.write_bytes(b"old-zip\n")
        before = {p: p.read_bytes() for p in (metadata, manifest, archive)}

        metadata_backup = root / ".metadata.backup"
        manifest_backup = root / ".manifest.backup"
        staged_zip = root / ".package.stage.zip"
        metadata.replace(metadata_backup)
        manifest.replace(manifest_backup)
        metadata.write_bytes(b"new-metadata\n")
        manifest.write_bytes(b"new-manifest\n")
        staged_zip.write_bytes(b"corrupt-stage\n")

        # Model a verification failure before ZIP publication and the mandatory rollback.
        metadata.unlink()
        manifest.unlink()
        metadata_backup.replace(metadata)
        manifest_backup.replace(manifest)
        staged_zip.unlink()

        after = {p: p.read_bytes() for p in (metadata, manifest, archive)}
        if before != after:
            raise AssertionError("failed staged finalization must preserve every published artifact byte-for-byte")


def main() -> int:
    assert_source_contract()
    assert_failure_atomic_reference_model()
    print("PASS: V25 signed-package finalization is bounded and failure-atomic by source contract")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
