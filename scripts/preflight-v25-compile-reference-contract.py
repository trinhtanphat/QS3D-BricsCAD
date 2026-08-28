#!/usr/bin/env python3
from pathlib import Path, PureWindowsPath
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
HELPER = ROOT / "scripts/acquire-v25-compile-references.ps1"
SNAPSHOT_HELPER = ROOT / "scripts/snapshot-v25-compile-references.ps1"
STATE_ASSERT = ROOT / "scripts/assert-v25-compile-reference-state.ps1"
BUILD_TARGETS = ROOT / "Directory.Build.targets"
WORKFLOWS = {
    "V25 integration": ROOT / ".github/workflows/bricscad-v25.yml",
    "manual V25 release": ROOT / ".github/workflows/release-v25.yml",
    "cloud V25 release": ROOT / ".github/workflows/release-v25-cloud.yml",
}
BASELINE_REFERENCES = {"BrxMgd.dll", "TD_Mgd.dll", "TD_MgdBrep.dll"}
errors = []


def read_required(path: Path, label: str) -> str:
    if not path.is_file():
        errors.append(f"missing {label}: {path.relative_to(ROOT)}")
        return ""
    try:
        return path.read_text(encoding="utf-8")
    except (OSError, UnicodeError) as exc:
        errors.append(f"cannot read {label}: {exc}")
        return ""


if not PROJECT.is_file():
    errors.append(f"missing V25 project: {PROJECT.relative_to(ROOT)}")
    required_files = set()
else:
    try:
        project_root = ET.fromstring(PROJECT.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, ET.ParseError) as exc:
        errors.append(f"cannot parse V25 project: {exc}")
        required_files = set()
    else:
        required_files = set()
        prefix = "$(BRICSCAD_V25_DIR)\\"
        for reference in project_root.findall(".//Reference"):
            hint = reference.findtext("HintPath")
            if not hint or not hint.startswith(prefix):
                continue
            filename = PureWindowsPath(hint).name
            if filename:
                required_files.add(filename)

        missing_baseline = sorted(BASELINE_REFERENCES - required_files)
        if missing_baseline:
            errors.append(
                "V25 project no longer exposes required managed references: "
                + ", ".join(missing_baseline)
            )

workflow_text = {}
for label, path in WORKFLOWS.items():
    text = read_required(path, f"{label} workflow")
    if not text:
        continue
    workflow_text[label] = text
    for filename in sorted(required_files):
        if filename not in text:
            errors.append(
                f"{label} does not mention project-required compile reference {filename}"
            )

integration = workflow_text.get("V25 integration", "")
if 'BRICSCAD_V25_DIR\\TD_MgdBrep.dll' not in integration:
    errors.append("V25 integration workflow does not fail fast on missing TD_MgdBrep.dll")

manual = workflow_text.get("manual V25 release", "")
manual_marker = "@('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')"
if manual_marker not in manual:
    errors.append("manual V25 release reference gate is not synchronized with TD_MgdBrep.dll")

cloud = workflow_text.get("cloud V25 release", "")
shared_helper_marker = ".\\scripts\\acquire-v25-compile-references.ps1"
if shared_helper_marker in cloud:
    cloud_markers = {
        "shared helper result binding": '"BRICSCAD_V25_DIR=$bricsDir"',
        "shared helper exact output selection": "Select-Object -Last 1",
    }
    for label, marker in cloud_markers.items():
        if marker not in cloud:
            errors.append(f"cloud V25 release is missing {label} contract")

    helper = read_required(HELPER, "shared V25 compile-reference helper")
    if helper:
        for filename in sorted(required_files):
            if filename not in helper:
                errors.append(
                    f"shared V25 compile-reference helper does not validate project-required reference {filename}"
                )
        helper_markers = {
            "runtime candidate discovery": "-Filter 'BrxMgd.dll'",
            "Brx co-location": "(Join-Path $_ 'BrxMgd.dll')",
            "TD co-location": "(Join-Path $_ 'TD_Mgd.dll')",
            "BREP co-location": "(Join-Path $_ 'TD_MgdBrep.dll')",
            "fail-closed co-location result": "if ([string]::IsNullOrWhiteSpace($bricsDir))",
            "resolved directory output": "Write-Output $bricsDir",
        }
        for label, marker in helper_markers.items():
            if marker not in helper:
                errors.append(
                    f"shared V25 compile-reference helper is missing {label} contract: {marker}"
                )
else:
    cloud_markers = {
        "BREP discovery": "-Filter 'TD_MgdBrep.dll'",
        "BREP co-location": "(Join-Path $_ 'TD_MgdBrep.dll')",
        "complete discovery count": "$brx.Count -lt 1 -or $td.Count -lt 1 -or $brep.Count -lt 1",
        "complete validation list": "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
    }
    for label, marker in cloud_markers.items():
        if marker not in cloud:
            errors.append(f"cloud V25 release is missing {label} contract for TD_MgdBrep.dll")

snapshot = read_required(SNAPSHOT_HELPER, "V25 compile-reference snapshot helper")
state_assert = read_required(STATE_ASSERT, "V25 compile-reference state verifier")
build_targets = read_required(BUILD_TARGETS, "repository build targets")

if snapshot:
    snapshot_markers = {
        "three-reference contract": "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "ordinary-file admission": "Assert-OrdinaryFile -Path $Path -Label $Label",
        "streaming SHA-256": "[Security.Cryptography.SHA256]::Create()",
        "source length binding": "$length = [int64]$first.Length",
        "source UTC timestamp binding": "$ticks = [int64]$first.LastWriteTimeUtc.Ticks",
        "independent second hash": "$secondHash = Get-StreamingSha256 -Path $secondPath",
        "copy from admitted path": "[IO.File]::Copy($before.path, $destinationPath, $false)",
        "post-copy source rebind": "$after = Get-StableFileState -Path $sourcePath",
        "snapshot byte verification": "$destination = Get-StableFileState -Path $destinationPath",
        "source/snapshot hash parity": "[string]::Equals($destination.sha256, $before.sha256, [StringComparison]::Ordinal)",
        "bounded state": "$maxStateBytes = 32768",
        "strict UTF-8 state write": "New-Object Text.UTF8Encoding($false, $true)",
        "reparse component admission": "Assert-NoExistingReparseComponent -Path $snapshot",
        "state containment": "StatePath must be contained by SnapshotDir",
    }
    for label, marker in snapshot_markers.items():
        if marker not in snapshot:
            errors.append(f"V25 compile-reference snapshot helper is missing {label}: {marker}")

    copy_index = snapshot.find("[IO.File]::Copy($before.path, $destinationPath, $false)")
    after_index = snapshot.find("$after = Get-StableFileState -Path $sourcePath")
    destination_index = snapshot.find("$destination = Get-StableFileState -Path $destinationPath")
    state_write_index = snapshot.find("[IO.File]::WriteAllText($state, $json, $utf8)")
    if min(copy_index, after_index, destination_index, state_write_index) < 0 or not (
        copy_index < after_index < destination_index < state_write_index
    ):
        errors.append(
            "V25 compile-reference snapshot ordering must be copy -> source rebind -> snapshot verification -> state publication"
        )

if state_assert:
    assert_markers = {
        "three-reference contract": "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "bounded state": "$maxStateBytes = 32768",
        "strict UTF-8 state read": "New-Object Text.UTF8Encoding($false, $true)",
        "ordinary state admission": "Assert-OrdinaryFile -Path $StatePath",
        "independent current second hash": "$secondHash = Get-StreamingSha256 -Path $secondPath",
        "path revalidation": "[string]::Equals(([string]$expected.path), $current.path",
        "length revalidation": "[int64]$expected.length -ne $current.length",
        "timestamp revalidation": "[int64]$expected.lastWriteUtcTicks -ne $current.lastWriteUtcTicks",
        "hash revalidation": "([string]$expected.sha256).ToUpperInvariant()",
    }
    for label, marker in assert_markers.items():
        if marker not in state_assert:
            errors.append(f"V25 compile-reference state verifier is missing {label}: {marker}")

if build_targets:
    target_markers = {
        "V25-only target": "'$(MSBuildProjectName)' == 'QS3D.BricsCAD.V25'",
        "assembly-resolution boundary": 'BeforeTargets="ResolveAssemblyReferences"',
        "snapshot helper invocation": "scripts\\snapshot-v25-compile-references.ps1",
        "state verifier invocation": "scripts\\assert-v25-compile-reference-state.ps1",
        "snapshot state path": "$(QS3DV25ReferenceStatePath)",
        "Brx in-memory remap": '<Reference Update="BrxMgd">',
        "TD in-memory remap": '<Reference Update="TD_Mgd">',
        "BREP in-memory remap": '<Reference Update="TD_MgdBrep">',
        "Brx snapshot HintPath": "$(QS3DV25ReferenceSnapshotDir)\\BrxMgd.dll",
        "TD snapshot HintPath": "$(QS3DV25ReferenceSnapshotDir)\\TD_Mgd.dll",
        "BREP snapshot HintPath": "$(QS3DV25ReferenceSnapshotDir)\\TD_MgdBrep.dll",
    }
    for label, marker in target_markers.items():
        if marker not in build_targets:
            errors.append(f"V25 build-boundary target is missing {label}: {marker}")

    snapshot_exec = build_targets.find("scripts\\snapshot-v25-compile-references.ps1")
    assert_exec = build_targets.find("scripts\\assert-v25-compile-reference-state.ps1")
    remap = build_targets.find('<Reference Update="BrxMgd">')
    if min(snapshot_exec, assert_exec, remap) < 0 or not (snapshot_exec < assert_exec < remap):
        errors.append(
            "V25 build-boundary ordering must be stable snapshot -> independent state verification -> Reference HintPath remap"
        )

if errors:
    print("V25 compile-reference contract preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print(
    "V25 compile-reference contract preflight PASS: "
    + ", ".join(sorted(required_files))
    + " are synchronized across workflows and are rebound to a verified stable per-build snapshot before assembly resolution."
)
