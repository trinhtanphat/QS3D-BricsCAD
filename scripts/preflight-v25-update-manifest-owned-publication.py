#!/usr/bin/env python3
from __future__ import annotations

import os
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "new-v25-update-manifest.ps1"
CORE = ROOT / "scripts" / "new-v25-update-manifest-validation-core.ps1"
HELPER = ROOT / "scripts" / "Qs3dV25UpdateManifestPublicationNative.cs"
source = SCRIPT.read_text(encoding="utf-8")
core = CORE.read_text(encoding="utf-8")
helper = HELPER.read_text(encoding="utf-8")


def require(text: str, token: str, message: str) -> None:
    if token not in text:
        raise SystemExit(f"ERROR: {message}: missing {token!r}")


# The internal core deliberately preserves the previously-reviewed admission logic,
# including its legacy publication block. The canonical wrapper MUST invoke it only
# with -WhatIf, so the legacy pathname publisher can never become write authority.
require(source, "new-v25-update-manifest-validation-core.ps1", "canonical wrapper does not use the validation core")
require(source, "-WhatIf 6>$null", "validation core is not forced into non-publishing validation mode")
require(source, "$wrapperCmdlet.ShouldProcess", "canonical wrapper does not own final ShouldProcess authority")

# Fail if any tracked source other than the canonical wrapper/preflight names the
# internal core. This prevents a workflow or release script from bypassing the safe
# publisher by invoking the copied legacy core directly.
completed = subprocess.run(
    ["git", "grep", "-l", "new-v25-update-manifest-validation-core.ps1", "--", ":(exclude)scripts/new-v25-update-manifest-validation-core.ps1"],
    cwd=ROOT,
    capture_output=True,
    text=True,
    encoding="utf-8",
    errors="strict",
    check=False,
)
if completed.returncode not in (0, 1):
    raise SystemExit(f"ERROR: could not audit validation-core callers: {(completed.stderr or completed.stdout).strip()}")
allowed_callers = {
    "scripts/new-v25-update-manifest.ps1",
    "scripts/preflight-v25-update-manifest-owned-publication.py",
}
callers = {line.strip().replace("\\", "/") for line in completed.stdout.splitlines() if line.strip()}
unexpected = sorted(callers - allowed_callers)
if unexpected:
    raise SystemExit(f"ERROR: internal V25 validation core has unauthorized caller/reference(s): {unexpected}")


# The helper must implement an actual held-handle boundary, not merely expose names
# that make the release script's lexical guard pass.
for token, message in (
    ("CreateFileW", "native generation acquisition is missing"),
    ("SetFileInformationByHandle", "handle-driven rename/delete is missing"),
    ("GetFileInformationByHandle", "stable generation identity is missing"),
    ("GetFinalPathNameByHandleW", "held-handle path verification is missing"),
    ("FileFlagOpenReparsePoint", "reparse-target following is not disabled"),
    ("Flush(true)", "durable held-generation flush is missing"),
    ("OpenOwnedStaging", "owned staging acquisition is missing"),
    ("OpenOwnedExisting", "prior-generation ownership is missing"),
    ("PublishOwnedGeneration", "handle-driven publication is missing"),
    ("GetOwnedGenerationIdentity", "held generation identity API is missing"),
    ("ReadOwnedGenerationBytes", "held exact-byte verification API is missing"),
    ("RollbackOwnedGeneration", "generation-bound rollback API is missing"),
    ("DeleteOwnedGeneration", "generation-bound deletion API is missing"),
):
    require(helper, token, message)


# Exercise actual Win32 layout/rename/rollback semantics on hosted Windows. This
# catches FILE_RENAME_INFO alignment or sharing mistakes that lexical source guards
# cannot detect.
if os.name == "nt":
    smoke = r'''
$ErrorActionPreference = 'Stop'
Add-Type -Path $args[0]
$root = Join-Path ([IO.Path]::GetTempPath()) ('qs3d-owned-publish-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($root) | Out-Null
$stagePath = Join-Path $root 'manifest.tmp'
$outputPath = Join-Path $root 'manifest.json'
$backupPath = Join-Path $root 'manifest.bak'
$stage = $null
$prior = $null
try {
    [IO.File]::WriteAllBytes($outputPath, [Text.Encoding]::UTF8.GetBytes('prior'))
    $stage = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedStaging($stagePath)
    $prior = [Qs3dV25UpdateManifestPublicationNative]::OpenOwnedExisting($outputPath)
    $payload = [Text.Encoding]::UTF8.GetBytes('{"schemaVersion":2}')
    [Qs3dV25UpdateManifestPublicationNative]::WriteOwnedGeneration($stage, $payload)
    $before = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stage)
    [Qs3dV25UpdateManifestPublicationNative]::PublishOwnedGeneration($stage, $outputPath, $prior, $backupPath)
    $after = [Qs3dV25UpdateManifestPublicationNative]::GetOwnedGenerationIdentity($stage)
    if ($before -ne $after) { throw 'staging identity changed across held-handle publication' }
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($stage, $outputPath)
    $actual = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($stage, 4096)
    if ($payload.Length -ne $actual.Length) { throw 'published held byte length differs from generated bytes' }
    for ($i = 0; $i -lt $payload.Length; $i++) {
        if ($payload[$i] -ne $actual[$i]) { throw 'published held bytes differ from generated bytes' }
    }

    [Qs3dV25UpdateManifestPublicationNative]::RollbackOwnedGeneration($stage, $prior, $outputPath, $stagePath)
    [Qs3dV25UpdateManifestPublicationNative]::AssertOwnedPath($prior, $outputPath)
    $restored = [Qs3dV25UpdateManifestPublicationNative]::ReadOwnedGenerationBytes($prior, 4096)
    $expectedPrior = [Text.Encoding]::UTF8.GetBytes('prior')
    if ($expectedPrior.Length -ne $restored.Length) { throw 'rollback prior length mismatch' }
    for ($i = 0; $i -lt $expectedPrior.Length; $i++) {
        if ($expectedPrior[$i] -ne $restored[$i]) { throw 'rollback did not restore exact prior generation bytes' }
    }
}
finally {
    if ($stage) { $stage.Dispose() }
    if ($prior) { $prior.Dispose() }
    if (Test-Path -LiteralPath $root) { Remove-Item -LiteralPath $root -Recurse -Force }
}
'''
    completed = subprocess.run(
        ["pwsh", "-NoLogo", "-NoProfile", "-NonInteractive", "-Command", smoke, str(HELPER)],
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


# Canonical publication must use only the held-generation API after validation.
for token, message in (
    ("Qs3dV25UpdateManifestPublicationNative", "release script does not load/use owned publication helper"),
    ("OpenOwnedStaging", "staging generation is not held open"),
    ("OpenOwnedExisting", "existing destination generation is not acquired before replacement"),
    ("PublishOwnedGeneration", "publication is not driven by the owned staging handle"),
    ("GetOwnedGenerationIdentity", "published generation identity is not verified from the held handle"),
    ("ReadOwnedGenerationBytes", "published bytes are not verified through the owned generation"),
    ("RollbackOwnedGeneration", "rollback is not bound to owned generation identity"),
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
):
    if unsafe in source:
        raise SystemExit(f"ERROR: canonical V25 update-manifest publisher still reauthorizes by pathname: {unsafe}")

print("PASS: V25 update-manifest validation is non-publishing and final publication is generation-owned")
