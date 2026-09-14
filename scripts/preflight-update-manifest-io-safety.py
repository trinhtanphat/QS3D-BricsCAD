#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
VALIDATION = ROOT / "scripts/new-v25-update-manifest-validation-core.ps1"
WRAPPER = ROOT / "scripts/new-v25-update-manifest.ps1"
NATIVE = ROOT / "scripts/Qs3dV25UpdateManifestPublicationNative.cs"
V26_WRAPPER = ROOT / "scripts/new-v26-update-manifest.ps1"
errors = []


def read(path: Path, label: str) -> str:
    if not path.is_file():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")
        return ""
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        errors.append(f"{label} missing required safety token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        errors.append(f"{label} contains forbidden unsafe token: {token}")


validation = read(VALIDATION, "V25 update-manifest validation core")
wrapper = read(WRAPPER, "V25 update-manifest publication wrapper")
native = read(NATIVE, "V25 held-generation native helper")
v26_wrapper = read(V26_WRAPPER, "V26 generated-manifest wrapper")

validation_tokens = (
    "$script:MaxMetadataBytes = 65536",
    "Assert-NoReparseDirectoryChain",
    "Resolve-OrdinaryNonReparseDirectory",
    "Resolve-OrdinaryNonReparseFile",
    "function Get-StreamingSha256",
    "function Get-StableFileState",
    "function Assert-StableFileState",
    "Read-BoundedStrictUtf8File",
    "$stream.Length -gt $script:MaxMetadataBytes",
    "[Text.UTF8Encoding]::new($false, $true)",
    "[Text.DecoderFallbackException]",
    "$metadataFile = Resolve-OrdinaryNonReparseFile",
    "$metadataState = Get-StableFileState",
    "$metadataText = Read-BoundedStrictUtf8File",
    "Assert-StableFileState -Expected $metadataState",
    "$payloadFiles[$name] = Resolve-OrdinaryNonReparseFile",
    "$zip = Resolve-OrdinaryNonReparseFile",
    "$zipState = Get-StableFileState",
    "Assert-StableFileState -Expected $zipState",
    "$zipHash = [string]$zipState.Sha256",
    "$package = Resolve-OrdinaryNonReparseDirectory",
    "Manifest verification temp parent",
    "Manifest verification workspace cleanup",
    "Unexpected directory in manifest verification workspace",
    "Remove-Item -LiteralPath $workspace.FullName -Force",
    "Update manifest output parent",
    "Existing update manifest",
    "Update manifest staging file",
)
for token in validation_tokens:
    require(validation, token, "V25 validation core")

for token in (
    "Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json",
    "Remove-Item -LiteralPath $temp -Recurse",
    "$manifest | ConvertTo-Json | Set-Content -LiteralPath $outputFull",
    "New-Item -ItemType Directory -Path $temp -Force",
    "$zipHash = (Get-FileHash",
):
    forbid(validation, token, "V25 validation core")

# The validation core owns only admission. Public publication must dot-source it
# under WhatIf and retain exact generation + parent authority around real mutation.
for token in (
    "$validationCorePath = Join-Path $PSScriptRoot 'new-v25-update-manifest-validation-core.ps1'",
    "$nativeHelperPath = Join-Path $PSScriptRoot 'Qs3dV25UpdateManifestPublicationNative.cs'",
    ". $validationCorePath",
    "-WhatIf 6>$null",
    "OpenOwnedDirectory($preOutputParentPath)",
    "OpenOwnedExisting($preOutputFull)",
    "$wrapperCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')",
    "OpenOwnedStaging($stagePath)",
    "WriteOwnedGeneration($stageOwned, $expectedManifestBytes)",
    "PublishOwnedGenerationInDirectory",
    "GetOwnedGenerationIdentity($stageOwned)",
    "ReadOwnedGenerationBytes($stageOwned",
    "RollbackOwnedGenerationInDirectory",
    "DeleteOwnedGeneration($priorOwned)",
):
    require(wrapper, token, "V25 publication wrapper")
for token in (
    "[IO.File]::WriteAllText($stagePath",
    "[IO.File]::Replace($stage.FullName, $outputFull",
    "[IO.File]::Move($stage.FullName, $outputFull",
    "& $validationCorePath",
):
    forbid(wrapper, token, "V25 publication wrapper")

# Native helper must keep handle-owned parent-relative publication and the exact
# one-byte FILE_DISPOSITION_INFO BOOLEAN ABI. MarshalAs(UnmanagedType.Bool) is
# valid for BOOL-returning P/Invokes; inspect only DeleteOwnedGeneration's payload.
for token in (
    "NtSetInformationFile",
    "FileRenameInformation",
    "RtlNtStatusToDosError",
    "PublishOwnedGenerationInDirectory",
    "RollbackOwnedGenerationInDirectory",
    "AssertOwnedDirectoryPath",
    "GetOwnedGenerationIdentity",
    "GetOwnedDirectoryIdentity",
):
    require(native, token, "V25 held-generation native helper")
for token in (
    "Marshal.SizeOf(typeof(FILE_DISPOSITION_INFO))",
    "private struct FILE_DISPOSITION_INFO",
):
    forbid(native, token, "V25 held-generation native helper disposition ABI")

delete_start = native.find("public static void DeleteOwnedGeneration")
delete_end = native.find("private static void RenameOwnedInDirectory", delete_start)
if delete_start < 0 or delete_end < 0:
    errors.append("V25 held-generation native helper must define DeleteOwnedGeneration before rename helpers")
else:
    disposition = native[delete_start:delete_end]
    for token in (
        "Marshal.AllocHGlobal(1)",
        "Marshal.WriteByte(buffer, 0, 1)",
        "generation.Stream.SafeFileHandle",
        "FileDispositionInfo",
        "buffer,",
        "1))",
    ):
        require(disposition, token, "V25 DeleteOwnedGeneration one-byte disposition ABI")

ordered = (
    "$package = Resolve-OrdinaryNonReparseDirectory",
    "$zip = Resolve-OrdinaryNonReparseFile",
    "$metadataFile = Resolve-OrdinaryNonReparseFile",
    "$metadataState = Get-StableFileState",
    "$zipState = Get-StableFileState",
    "$metadataText = Read-BoundedStrictUtf8File",
    "Assert-StableFileState -Expected $metadataState",
    "ConvertFrom-Json -ErrorAction Stop",
    "$expectedSigner = Normalize-Thumbprint",
    "Assert-ZipPayloadMatchesSignedStaging -ZipFile $zip -PackageRoot $package",
    "$zip = Assert-StableFileState -Expected $zipState",
    "$zipHash = [string]$zipState.Sha256",
    "$PSCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')",
)
positions = [validation.find(token) for token in ordered]
if any(pos < 0 for pos in positions) or positions != sorted(positions):
    errors.append("V25 validation safety ordering must be path admission -> stable capture -> bounded metadata/trust/parity -> stable ZIP recheck/hash -> non-publishing WhatIf boundary")

wrapper_ordered = (
    "OpenOwnedDirectory($preOutputParentPath)",
    "OpenOwnedExisting($preOutputFull)",
    ". $validationCorePath",
    "$wrapperCmdlet.ShouldProcess($outputFull, 'Write QS3D update manifest')",
    "OpenOwnedStaging($stagePath)",
    "WriteOwnedGeneration($stageOwned, $expectedManifestBytes)",
    "PublishOwnedGenerationInDirectory",
    "ReadOwnedGenerationBytes($stageOwned",
)
positions = [wrapper.find(token) for token in wrapper_ordered]
if any(pos < 0 for pos in positions) or positions != sorted(positions):
    errors.append("V25 publication ordering must hold parent/prior generations before validation, then stage/publish/verify the same held generation")

# Mutation probes prove admission protections remain independently observable.
required_markers = (
    "$stream.Length -gt $script:MaxMetadataBytes",
    "[Text.UTF8Encoding]::new($false, $true).GetString($bytes)",
    "$metadataFile = Resolve-OrdinaryNonReparseFile",
    "$metadataState = Get-StableFileState",
    "Assert-StableFileState -Expected $metadataState",
    "$zip = Resolve-OrdinaryNonReparseFile",
    "$zipState = Get-StableFileState",
    "Assert-StableFileState -Expected $zipState",
    "$zipHash = [string]$zipState.Sha256",
    "$package = Resolve-OrdinaryNonReparseDirectory",
    "Remove-Item -LiteralPath $workspace.FullName -Force",
)
mutations = {
    "remove metadata bound": validation.replace("$stream.Length -gt $script:MaxMetadataBytes", "$false", 1),
    "weaken strict UTF8": validation.replace("[Text.UTF8Encoding]::new($false, $true).GetString($bytes)", "[Text.Encoding]::UTF8.GetString($bytes)", 1),
    "bypass metadata ordinary file": validation.replace("$metadataFile = Resolve-OrdinaryNonReparseFile", "$metadataFile = Get-Item", 1),
    "remove metadata stable capture": validation.replace("$metadataState = Get-StableFileState", "$metadataState = $null", 1),
    "remove metadata stable recheck": validation.replace("Assert-StableFileState -Expected $metadataState", "# removed metadata stable recheck", 1),
    "bypass zip ordinary file": validation.replace("$zip = Resolve-OrdinaryNonReparseFile", "$zip = Get-Item", 1),
    "remove ZIP stable capture": validation.replace("$zipState = Get-StableFileState", "$zipState = $null", 1),
    "remove ZIP stable recheck": validation.replace("Assert-StableFileState -Expected $zipState", "# removed ZIP stable recheck", 1),
    "restore recursive cleanup": validation.replace("Remove-Item -LiteralPath $workspace.FullName -Force", "Remove-Item -LiteralPath $workspace.FullName -Recurse -Force", 1),
}
for name, mutated in mutations.items():
    if mutated == validation:
        errors.append(f"mutation probe did not alter validation core: {name}")
        continue
    if all(marker in mutated for marker in required_markers):
        errors.append(f"mutation escaped update-manifest validation I/O safety contract: {name}")

# V26 must still route through generated V25 templates. The dedicated V26 package
# guard additionally verifies all split dependencies are generated and held.
for token in (
    "new-v26-script-from-v25.ps1",
    "new-v25-update-manifest.ps1",
):
    require(v26_wrapper, token, "V26 update-manifest wrapper")

print("QS3D update-manifest I/O safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: split V25 admission is bounded/reparse-aware/generation-stable and public publication is held-parent/held-generation owned; V26 remains template-derived.")
