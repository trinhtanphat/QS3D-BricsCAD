#!/usr/bin/env python3
from __future__ import annotations

import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "new-v25-update-manifest.ps1"
HELPER = ROOT / "scripts" / "Qs3dV25UpdateManifestPublicationNative.cs"
source = SCRIPT.read_text(encoding="utf-8")
helper = HELPER.read_text(encoding="utf-8")


def require(text: str, token: str, message: str) -> None:
    if token not in text:
        raise SystemExit(f"ERROR: {message}: missing {token!r}")


# The destination directory and prior output generation must become authority before
# the long package/signature validation call. Byte equality after validation is not
# generation identity: an attacker could delete/recreate the same bytes otherwise.
for token, message in (
    ("Qs3dOwnedDirectory", "native held-directory authority is missing"),
    ("OpenOwnedDirectory", "destination-parent authority is not acquired"),
    ("GetOwnedDirectoryIdentity", "destination-parent stable identity API is missing"),
    ("AssertOwnedDirectoryPath", "destination-parent path continuity API is missing"),
    ("FileFlagBackupSemantics", "directory handle acquisition does not use backup semantics"),
):
    require(helper, token, message)

validation_call = source.find(". $validationCorePath")
if validation_call < 0:
    raise SystemExit("ERROR: canonical wrapper validation-core call is missing")
for token in ("OpenOwnedDirectory", "OpenOwnedExisting"):
    position = source.find(token)
    if position < 0 or position > validation_call:
        raise SystemExit(
            f"ERROR: {token} must acquire authority before long validation, not after it"
        )

for token, message in (
    ("$destinationParentOwned", "wrapper does not retain held destination-parent authority"),
    ("$priorOwned", "wrapper does not retain prior-generation authority"),
    ("AssertOwnedDirectoryPath", "wrapper does not revalidate held parent path"),
    ("GetOwnedGenerationIdentity", "wrapper does not retain exact prior generation identity"),
):
    require(source, token, message)

# Publication/rollback must be rooted in the already-held destination directory.
# Passing an absolute destination pathname back into the native rename helper would
# reintroduce pathname authority after admission.
require(helper, "SafeFileHandle RootHandle", "owned directory does not expose its stable root handle internally")
require(helper, "RenameOwnedInDirectory", "rename is not rooted in the held destination directory")


if os.name == "nt":
    smoke = r'''
$ErrorActionPreference = 'Stop'
$helperPath = [Environment]::GetEnvironmentVariable('QS3D_V25_CONTINUITY_HELPER')
if ([string]::IsNullOrWhiteSpace($helperPath)) { throw 'continuity helper path is missing' }
Add-Type -Path $helperPath
$root = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-v25-continuity-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$outputPath = Join-Path $root 'manifest.json'
$stagePath = Join-Path $root 'manifest.tmp'
$backupName = 'manifest.bak'
$parent = $null
$prior = $null
$stage = $null
try {
    $priorBytes = [Text.Encoding]::UTF8.GetBytes('same-bytes')
    [IO.File]::WriteAllBytes($outputPath, $priorBytes)
    $parent = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedDirectory($root)
    $parentIdentity = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($parent)
    $prior = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedExisting($outputPath)
    $priorIdentity = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($prior)

    # Exact prior-generation ownership must block delete/recreate, even when the
    # replacement would contain byte-identical content.
    $replacementBlocked = $false
    try {
        [IO.File]::Delete($outputPath)
        [IO.File]::WriteAllBytes($outputPath, $priorBytes)
    }
    catch { $replacementBlocked = $true }
    if (-not $replacementBlocked) { throw 'byte-identical destination recreation was not blocked by held prior generation' }
    if ($priorIdentity -ne [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($prior)) {
        throw 'prior generation identity changed while authority was held'
    }

    # Parent authority must reject a path swap while admission is in progress.
    $moved = $root + '.moved'
    $parentSwapBlocked = $false
    try { [IO.Directory]::Move($root, $moved) } catch { $parentSwapBlocked = $true }
    if (-not $parentSwapBlocked) { throw 'destination-parent rename was not blocked by held directory authority' }
    if ($parentIdentity -ne [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($parent)) {
        throw 'destination-parent identity changed while authority was held'
    }
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($parent, $root)

    $stage = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedStaging($stagePath)
    $payload = [Text.Encoding]::UTF8.GetBytes('{"schemaVersion":2}')
    [Qs3dV25UpdateManifestPublicationNative]::WriteOwnedGeneration($stage, $payload)
    [Qs3dV25UpdateManifestPublicationNative]::PublishOwnedGenerationInDirectory(
        $stage, $parent, 'manifest.json', $prior, $backupName)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($parent, $root)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($stage, $outputPath)

    [Qs3dV25UpdateManifestPublicationNative]::RollbackOwnedGenerationInDirectory(
        $stage, $parent, $prior, 'manifest.json', $stagePath)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($prior, $outputPath)
}
finally {
    if ($stage) { $stage.Dispose() }
    if ($prior) { $prior.Dispose() }
    if ($parent) { $parent.Dispose() }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
    if (Test-Path -LiteralPath ($root + '.moved')) { Remove-Item -LiteralPath ($root + '.moved') -Recurse -Force }
}
'''
    env = os.environ.copy()
    env["QS3D_V25_CONTINUITY_HELPER"] = str(HELPER)
    completed = subprocess.run(
        ["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", smoke],
        cwd=ROOT,
        env=env,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="strict",
        check=False,
    )
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise SystemExit(f"ERROR: V25 generation-continuity behavioral smoke failed: {detail}")

print("PASS: V25 update-manifest authority is held before admission and remains generation/parent-bound")
