#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "scripts/new-v25-update-manifest.ps1"
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


source = read(SOURCE, "V25 update-manifest template")
wrapper = read(V26_WRAPPER, "V26 generated-manifest wrapper")

for token in (
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
    "[IO.File]::WriteAllText($stagePath",
    "[IO.File]::Replace($stage.FullName, $outputFull, $backupPath, $true)",
    "[IO.File]::Move($stage.FullName, $outputFull)",
    "Published update manifest",
):
    require(source, token, "V25 update-manifest template")

for token in (
    "Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json",
    "Remove-Item -LiteralPath $temp -Recurse",
    "$manifest | ConvertTo-Json | Set-Content -LiteralPath $outputFull",
    "New-Item -ItemType Directory -Path $temp -Force",
    "$zipHash = (Get-FileHash",
):
    forbid(source, token, "V25 update-manifest template")

# Ordering matters: no parser/trust/parity publication may precede fail-closed
# ordinary-file admission and stable generation binding.
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
    "[IO.File]::WriteAllText($stagePath",
)
positions = [source.find(token) for token in ordered]
if any(pos < 0 for pos in positions) or positions != sorted(positions):
    errors.append("V25 update-manifest safety ordering must be path admission -> stable capture -> bounded metadata/trust/parity -> stable ZIP recheck/hash -> atomic publication")

# V26 must keep routing through the shared generated V25 template, not grow an
# independent unguarded implementation.
for token in (
    "new-v26-script-from-v25.ps1",
    "-SourceScript 'new-v25-update-manifest.ps1'",
    "Generated V26 update-manifest script",
):
    require(wrapper, token, "V26 update-manifest wrapper")

# Mutation probes ensure each major protection is independently observable.
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
    "[IO.File]::WriteAllText($stagePath",
    "[IO.File]::Replace($stage.FullName, $outputFull, $backupPath, $true)",
)
mutations = {
    "remove metadata bound": source.replace("$stream.Length -gt $script:MaxMetadataBytes", "$false", 1),
    "weaken strict UTF8": source.replace("[Text.UTF8Encoding]::new($false, $true).GetString($bytes)", "[Text.Encoding]::UTF8.GetString($bytes)", 1),
    "bypass metadata ordinary file": source.replace("$metadataFile = Resolve-OrdinaryNonReparseFile", "$metadataFile = Get-Item", 1),
    "remove metadata stable capture": source.replace("$metadataState = Get-StableFileState", "$metadataState = $null", 1),
    "remove metadata stable recheck": source.replace("Assert-StableFileState -Expected $metadataState", "# removed metadata stable recheck", 1),
    "bypass zip ordinary file": source.replace("$zip = Resolve-OrdinaryNonReparseFile", "$zip = Get-Item", 1),
    "remove ZIP stable capture": source.replace("$zipState = Get-StableFileState", "$zipState = $null", 1),
    "remove ZIP stable recheck": source.replace("Assert-StableFileState -Expected $zipState", "# removed ZIP stable recheck", 1),
    "restore recursive cleanup": source.replace("Remove-Item -LiteralPath $workspace.FullName -Force", "Remove-Item -LiteralPath $workspace.FullName -Recurse -Force", 1),
    "direct final write": source.replace("[IO.File]::WriteAllText($stagePath", "[IO.File]::WriteAllText($outputFull", 1),
    "remove replace publication": source.replace("[IO.File]::Replace($stage.FullName, $outputFull, $backupPath, $true)", "[IO.File]::Move($stage.FullName, $outputFull)", 1),
}
for name, mutated in mutations.items():
    if mutated == source:
        errors.append(f"mutation probe did not alter update-manifest source: {name}")
        continue
    if all(marker in mutated for marker in required_markers):
        errors.append(f"mutation escaped update-manifest I/O safety contract: {name}")

print("QS3D update-manifest I/O safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: shared V25/V26 update-manifest generation is bounded, reparse-aware, generation-stable, cleanup-bounded, and atomically published.")
