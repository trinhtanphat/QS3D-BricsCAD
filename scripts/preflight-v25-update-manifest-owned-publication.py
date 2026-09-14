#!/usr/bin/env python3
from __future__ import annotations

import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "new-v25-update-manifest.ps1"
CORE = ROOT / "scripts" / "new-v25-update-manifest-validation-core.ps1"
HELPER = ROOT / "scripts" / "Qs3dV25UpdateManifestPublicationNative.cs"
TRANSFORMER = ROOT / "scripts" / "new-v26-script-from-v25.ps1"
V26_MANIFEST = ROOT / "scripts" / "new-v26-update-manifest.ps1"
source = SCRIPT.read_text(encoding="utf-8")
core = CORE.read_text(encoding="utf-8")
helper = HELPER.read_text(encoding="utf-8")
transformer = TRANSFORMER.read_text(encoding="utf-8")
v26_manifest = V26_MANIFEST.read_text(encoding="utf-8")


def require(text: str, token: str, message: str) -> None:
    if token not in text:
        raise SystemExit(f"ERROR: {message}: missing {token!r}")


# The internal core deliberately preserves the previously-reviewed admission logic,
# including its legacy publication block. The canonical wrapper MUST invoke it only
# with -WhatIf, so the legacy pathname publisher can never become write authority.
require(source, "new-v25-update-manifest-validation-core.ps1", "canonical wrapper does not use the validation core")
require(source, "-WhatIf 6>$null", "validation core is not forced into non-publishing validation mode")
require(source, "$wrapperCmdlet.ShouldProcess", "canonical wrapper does not own final ShouldProcess authority")

# Audit executable PowerShell references, not test/source-guard documentation. The
# previous repository-wide grep treated every Python preflight that named the split
# validation core as an executable caller and therefore false-failed Clean20. V26's
# transformer and orchestrator may name the V25 template strictly as transform input;
# they must not directly invoke it.
completed = subprocess.run(
    [
        "git",
        "grep",
        "-l",
        "new-v25-update-manifest-validation-core.ps1",
        "--",
        "scripts/*.ps1",
        ":(exclude)scripts/new-v25-update-manifest-validation-core.ps1",
    ],
    cwd=ROOT,
    capture_output=True,
    text=True,
    encoding="utf-8",
    errors="strict",
    check=False,
)
if completed.returncode not in (0, 1):
    raise SystemExit(f"ERROR: could not audit validation-core PowerShell references: {(completed.stderr or completed.stdout).strip()}")
allowed_references = {
    "scripts/new-v25-update-manifest.ps1",
    "scripts/new-v26-script-from-v25.ps1",
    "scripts/new-v26-update-manifest.ps1",
}
references = {line.strip().replace("\\", "/") for line in completed.stdout.splitlines() if line.strip()}
unexpected = sorted(references - allowed_references)
if unexpected:
    raise SystemExit(f"ERROR: internal V25 validation core has unauthorized PowerShell reference(s): {unexpected}")
if "new-v25-update-manifest-validation-core.ps1" not in transformer:
    raise SystemExit("ERROR: V26 transformer no longer declares the split validation core as a transform input")
require(
    v26_manifest,
    "Source = 'new-v25-update-manifest-validation-core.ps1'",
    "V26 manifest orchestrator no longer declares the V25 validation core as a transform input",
)
require(
    v26_manifest,
    "Generated = 'new-v26-update-manifest-validation-core.ps1'",
    "V26 manifest orchestrator no longer binds the transformed validation-core output",
)
require(v26_manifest, "& $transformer -TemplateScript", "V26 manifest orchestrator does not route generation through the transformer")
for label, text in (
    ("V26 transformer", transformer),
    ("V26 manifest orchestrator", v26_manifest),
):
    for unsafe_transformer_call in (
        ". 'new-v25-update-manifest-validation-core.ps1'",
        '& "new-v25-update-manifest-validation-core.ps1"',
        "& 'new-v25-update-manifest-validation-core.ps1'",
        '. "new-v25-update-manifest-validation-core.ps1"',
    ):
        if unsafe_transformer_call in text:
            raise SystemExit(
                f"ERROR: {label} must transform the validation core, not execute it directly: "
                + unsafe_transformer_call
            )


for token, message in (
    ("CreateFileW", "native generation acquisition is missing"),
    ("SetFileInformationByHandle", "handle-driven rename/delete is missing"),
    ("GetFileInformationByHandle", "stable generation identity is missing"),
    ("GetFinalPathNameByHandleW", "held-handle path verification is missing"),
    ("FileFlagOpenReparsePoint", "reparse-target following is not disabled"),
    ("FileFlagBackupSemantics", "held destination-directory acquisition is missing"),
    ("Flush(true)", "durable held-generation flush is missing"),
    ("OpenOwnedDirectory", "owned destination-parent acquisition is missing"),
    ("OpenOwnedStaging", "owned staging acquisition is missing"),
    ("OpenOwnedExisting", "prior-generation ownership is missing"),
    ("PublishOwnedGenerationInDirectory", "held-parent publication is missing"),
    ("GetOwnedGenerationIdentity", "held generation identity API is missing"),
    ("GetOwnedDirectoryIdentity", "held destination-parent identity API is missing"),
    ("ReadOwnedGenerationBytes", "held exact-byte verification API is missing"),
    ("RollbackOwnedGenerationInDirectory", "held-parent rollback API is missing"),
    ("DeleteOwnedGeneration", "generation-bound deletion API is missing"),
    ("FileDispositionInfo", "handle-bound delete information class is missing"),
    ("Marshal.AllocHGlobal(1)", "FILE_DISPOSITION_INFO must allocate its one-byte BOOLEAN payload"),
    ("Marshal.WriteByte(buffer, 0, 1)", "FILE_DISPOSITION_INFO.DeleteFile is not marshalled as BOOLEAN"),
    ("RenameOwnedInDirectory", "rename must be rooted in held destination-parent authority"),
):
    require(helper, token, message)

for unsafe in (
    "Marshal.AllocHGlobal(sizeof(int))",
    "Marshal.WriteInt32(buffer, 1)",
    "System.IO.File.Delete(",
    "public static void PublishOwnedGeneration(",
    "public static void RollbackOwnedGeneration(",
    "private static void RenameOwned(Qs3dOwnedGeneration",
):
    if unsafe in helper:
        raise SystemExit(f"ERROR: owned-generation publication regressed to unsafe ABI/path authority: {unsafe}")


if os.name == "nt":
    smoke = r'''
$ErrorActionPreference = 'Stop'
$helperPath = [Environment]::GetEnvironmentVariable('QS3D_V25_PUBLICATION_HELPER')
if ([string]::IsNullOrWhiteSpace($helperPath)) { throw 'owned-publication helper path environment variable is missing' }
Add-Type -Path $helperPath
$root = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-owned-publish-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$stagePath = Join-Path $root 'manifest.tmp'
$outputPath = Join-Path $root 'manifest.json'
$stage = $null
$prior = $null
$parent = $null
try {
    [IO.File]::WriteAllBytes($outputPath, [Text.Encoding]::UTF8.GetBytes('prior'))
    $parent = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedDirectory($root)
    $stage = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedStaging($stagePath)
    $prior = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedExisting($outputPath)
    $payload = [Text.Encoding]::UTF8.GetBytes('{"schemaVersion":2}')
    [Qs3dV25UpdateManifestPublicationNative]::WriteOwnedGeneration($stage, $payload)
    $before = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stage)
    $parentBefore = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($parent)
    [Qs3dV25UpdateManifestPublicationNative]::PublishOwnedGenerationInDirectory($stage, $parent, 'manifest.json', $prior, 'manifest.bak')
    $after = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stage)
    if ($before -ne $after) { throw 'staging identity changed across held-handle publication' }
    if ($parentBefore -ne [Qs3dV25UpdateManifestPublicationNative]::GetOwnedDirectoryIdentity($parent)) { throw 'parent identity changed across publication' }
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedDirectoryPath($parent, $root)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($stage, $outputPath)
    $actual = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($stage, 4096)
    if ($payload.Length -ne $actual.Length) { throw 'published held byte length differs from generated bytes' }
    for ($i = 0; $i -lt $payload.Length; $i++) {
        if ($payload[$i] -ne $actual[$i]) { throw 'published held bytes differ from generated bytes' }
    }

    [Qs3dV25UpdateManifestPublicationNative]::RollbackOwnedGenerationInDirectory($stage, $parent, $prior, 'manifest.json', $stagePath)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($prior, $outputPath)
    $restored = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($prior, 4096)
    $expectedPrior = [Text.Encoding]::UTF8.GetBytes('prior')
    if ($expectedPrior.Length -ne $restored.Length) { throw 'rollback prior length mismatch' }
    for ($i = 0; $i -lt $expectedPrior.Length; $i++) {
        if ($expectedPrior[$i] -ne $restored[$i]) { throw 'rollback did not restore exact prior generation bytes' }
    }

    $quarantinePath = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedCurrentPath($stage)
    $stage.Dispose()
    $stage = $null
    if (Test-Path -LiteralPath $quarantinePath) { throw 'generation-bound rollback deletion did not remove the quarantined staging generation' }
}
finally {
    if ($stage) { $stage.Dispose() }
    if ($prior) { $prior.Dispose() }
    if ($parent) { $parent.Dispose() }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
'''
    smoke_env = os.environ.copy()
    smoke_env["QS3D_V25_PUBLICATION_HELPER"] = str(HELPER)
    completed = subprocess.run(
        ["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", smoke],
        env=smoke_env,
        cwd=ROOT,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="strict",
        check=False,
    )
    if completed.returncode != 0:
        detail = (completed.stderr or completed.stdout).strip()
        raise SystemExit(f"ERROR: V25 owned-publication native smoke failed: {detail}")


for token, message in (
    ("Qs3dV25UpdateManifestPublicationNative", "release script does not load/use owned publication helper"),
    ("OpenOwnedDirectory", "destination parent is not held open"),
    ("OpenOwnedStaging", "staging generation is not held open"),
    ("OpenOwnedExisting", "existing destination generation is not acquired before replacement"),
    ("PublishOwnedGenerationInDirectory", "publication is not rooted in held destination parent"),
    ("GetOwnedGenerationIdentity", "published generation identity is not verified from the held handle"),
    ("GetOwnedDirectoryIdentity", "destination parent identity is not verified from held handle"),
    ("ReadOwnedGenerationBytes", "published bytes are not verified through the owned generation"),
    ("RollbackOwnedGenerationInDirectory", "rollback is not rooted in held destination parent"),
    ("DeleteOwnedGeneration", "superseded prior generation is not deleted by owned handle"),
    ("$expectedManifestBytes", "expected manifest bytes are not retained for exact verification"),
):
    require(source, token, message)

for unsafe in (
    "[IO.File]::WriteAllText($stagePath",
    "[IO.File]::Move($stage.FullName, $outputFull)",
    "[IO.File]::Replace($stage.FullName, $outputFull",
    "Resolve-OrdinaryNonReparseFile -Path $outputFull -Label 'Published update manifest'",
    "[IO.File]::Delete($failedOutput.FullName)",
    "::PublishOwnedGeneration($stageOwned",
    "::RollbackOwnedGeneration($stageOwned",
):
    if unsafe in source:
        raise SystemExit(f"ERROR: canonical V25 update-manifest publisher still reauthorizes by pathname: {unsafe}")

print("PASS: V25 update-manifest validation is non-publishing and final publication is generation/parent-owned")
