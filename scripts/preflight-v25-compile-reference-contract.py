#!/usr/bin/env python3
from pathlib import Path, PureWindowsPath
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj"
HELPER = ROOT / "scripts/acquire-v25-compile-references.ps1"
SNAPSHOT_HELPER = ROOT / "scripts/snapshot-v25-compile-references.ps1"
STATE_ASSERT = ROOT / "scripts/assert-v25-compile-reference-state.ps1"
BUILD_WRAPPER = ROOT / "scripts/build-v25-with-stable-references.ps1"
CI_WORKFLOW = ROOT / ".github/workflows/ci.yml"
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

        legacy_runtime_discovery = "-Filter 'BrxMgd.dll'"
        hardened_runtime_discovery = "Get-OrdinaryFilesByNameUnderRoot -Root $extract -Name 'BrxMgd.dll'"
        hardened_discovery_helper = "function Get-OrdinaryFilesByNameUnderRoot"
        hardened_reparse_rejection = "Extracted V25 tree must not contain filesystem reparse points"
        if hardened_discovery_helper in helper:
            if hardened_runtime_discovery not in helper:
                errors.append(
                    "shared V25 compile-reference helper is missing hardened non-reparse runtime candidate discovery: "
                    + hardened_runtime_discovery
                )
            if hardened_reparse_rejection not in helper:
                errors.append(
                    "shared V25 compile-reference helper is missing extracted-tree reparse rejection: "
                    + hardened_reparse_rejection
                )
            if legacy_runtime_discovery in helper:
                errors.append(
                    "shared V25 compile-reference helper retains retired recursive Brx discovery after hardened discovery was introduced"
                )
        elif legacy_runtime_discovery not in helper:
            errors.append(
                "shared V25 compile-reference helper is missing both legacy and hardened runtime candidate discovery contracts"
            )

        helper_markers = {
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
build_wrapper = read_required(BUILD_WRAPPER, "V25 locked-reference build wrapper")
ci_workflow = read_required(CI_WORKFLOW, "Shared CI workflow")

if snapshot:
    snapshot_markers = {
        "three-reference contract": "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "ordinary-file admission": "must be an ordinary non-reparse file",
        "streaming SHA-256": "[Security.Cryptography.SHA256]::Create()",
        "simultaneous source lock": "function Open-LockedReferenceState",
        "write/delete denying source lock": "[IO.FileShare]::Read",
        "held-stream source digest": "$hash = Get-StreamSha256 -Stream $stream",
        "all-source lock collection": "$locked = New-Object 'System.Collections.Generic.List[object]'",
        "lock admission": "state = Open-LockedReferenceState -Path $sourcePath",
        "copy from held source stream": "$source.stream.CopyTo($destinationStream)",
        "post-copy held source rehash": "$sourceHashAfterCopy = Get-StreamSha256 -Stream $source.stream",
        "snapshot byte verification": "$destination = Get-StableFileState -Path $destinationPath",
        "source/snapshot hash parity": "[string]::Equals($destination.sha256, [string]$source.sha256, [StringComparison]::Ordinal)",
        "whole-set rebind": "# Rebind every source member while every lock is still held.",
        "failed manifest cleanup": "Remove-Item -LiteralPath $state -Force -ErrorAction SilentlyContinue",
        "source lock disposal": "$locked[$index].state.stream.Dispose()",
        "bounded state": "$maxStateBytes = 32768",
        "strict UTF-8 state write": "New-Object Text.UTF8Encoding($false, $true)",
        "reparse component admission": "Assert-NoExistingReparseComponent -Path $snapshot",
        "state containment": "StatePath must be contained by SnapshotDir",
        "source-in-snapshot overlap rejection": "SnapshotDir must not equal or contain the V25 source reference directory.",
        "snapshot-in-source overlap rejection": "SnapshotDir must not be located inside the V25 source reference directory.",
    }
    for label, marker in snapshot_markers.items():
        if marker not in snapshot:
            errors.append(f"V25 compile-reference snapshot helper is missing {label}: {marker}")

    admission_index = snapshot.find("# Admission is deliberately a separate phase")
    lock_index = snapshot.find("state = Open-LockedReferenceState -Path $sourcePath", admission_index)
    copy_index = snapshot.find("$source.stream.CopyTo($destinationStream)", lock_index)
    source_rehash_index = snapshot.find("$sourceHashAfterCopy = Get-StreamSha256 -Stream $source.stream", copy_index)
    destination_index = snapshot.find("$destination = Get-StableFileState -Path $destinationPath", source_rehash_index)
    whole_set_rebind_index = snapshot.find("# Rebind every source member while every lock is still held.", destination_index)
    state_write_index = snapshot.find("[IO.File]::WriteAllText($state, $json, $utf8)", whole_set_rebind_index)
    dispose_index = snapshot.find("$locked[$index].state.stream.Dispose()", state_write_index)
    if min(
        admission_index,
        lock_index,
        copy_index,
        source_rehash_index,
        destination_index,
        whole_set_rebind_index,
        state_write_index,
        dispose_index,
    ) < 0 or not (
        admission_index
        < lock_index
        < copy_index
        < source_rehash_index
        < destination_index
        < whole_set_rebind_index
        < state_write_index
        < dispose_index
    ):
        errors.append(
            "V25 compile-reference snapshot ordering must be all-source admission -> held-stream copy -> source rehash -> snapshot verification -> whole-set rebind -> state publication -> lock disposal"
        )

    retired_snapshot_markers = {
        "sequential admitted-path copy": "[IO.File]::Copy($before.path, $destinationPath, $false)",
        "per-file source generation before whole-set admission": "$before = Get-StableFileState -Path $sourcePath",
        "per-file post-copy source reopen": "$after = Get-StableFileState -Path $sourcePath",
    }
    for label, marker in retired_snapshot_markers.items():
        if marker in snapshot:
            errors.append(f"V25 compile-reference snapshot helper retains retired {label}: {marker}")

if state_assert:
    assert_markers = {
        "three-reference contract": "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "bounded state": "$maxStateBytes = 32768",
        "strict UTF-8 state read": "New-Object Text.UTF8Encoding($false, $true)",
        "stable state admission": "$stateBefore = Get-CurrentStableState -Path $StatePath",
        "bounded materialization": "[IO.File]::ReadAllBytes($stateBefore.path)",
        "materialized-byte hash": "$materializedHash = Get-ByteArraySha256 -Bytes $rawBytes",
        "post-read state rebind": "$stateAfter = Get-CurrentStableState -Path $StatePath",
        "state ancestry admission": "Assert-NoExistingReparseComponent -Path $StatePath",
        "schema version validation": "[int]$state.schemaVersion -ne 1",
        "independent current second hash": "$secondHash = Get-StreamingSha256 -Path $secondPath",
        "path revalidation": "[string]::Equals(([string]$expected.path), $current.path",
        "length revalidation": "[int64]$expected.length -ne $current.length",
        "timestamp revalidation": "[int64]$expected.lastWriteUtcTicks -ne $current.lastWriteUtcTicks",
        "hash revalidation": "([string]$expected.sha256).ToUpperInvariant()",
    }
    for label, marker in assert_markers.items():
        if marker not in state_assert:
            errors.append(f"V25 compile-reference state verifier is missing {label}: {marker}")

    before_read = state_assert.find("$stateBefore = Get-CurrentStableState -Path $StatePath")
    materialize = state_assert.find("[IO.File]::ReadAllBytes($stateBefore.path)")
    materialized_hash = state_assert.find("$materializedHash = Get-ByteArraySha256 -Bytes $rawBytes")
    after_read = state_assert.find("$stateAfter = Get-CurrentStableState -Path $StatePath")
    parse = state_assert.find("$state = $raw | ConvertFrom-Json")
    if min(before_read, materialize, materialized_hash, after_read, parse) < 0 or not (
        before_read < materialize < materialized_hash < after_read < parse
    ):
        errors.append(
            "V25 compile-reference state ordering must be admit -> bounded materialize -> byte hash -> post-read rebind -> parse"
        )

if build_wrapper:
    wrapper_markers = {
        "three-reference lock contract": "@('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')",
        "stable snapshot creation": "snapshot-v25-compile-references.ps1",
        "state verifier": "assert-v25-compile-reference-state.ps1",
        "write/delete denying lock": "[IO.FileShare]::Read",
        "lock collection": "System.Collections.Generic.List[System.IO.FileStream]",
        "process-only reference rebind": "[Environment]::SetEnvironmentVariable('BRICSCAD_V25_DIR', $snapshot, 'Process')",
        "child dotnet build": "& dotnet @arguments",
        "build result capture": "$buildExitCode = $LASTEXITCODE",
        "environment restoration": "[Environment]::SetEnvironmentVariable('BRICSCAD_V25_DIR', $previousBricsCadDir, 'Process')",
        "lock disposal": "$locks[$index].Dispose()",
    }
    for label, marker in wrapper_markers.items():
        if marker not in build_wrapper:
            errors.append(f"V25 locked-reference build wrapper is missing {label}: {marker}")

    snapshot_call = build_wrapper.find("snapshot-v25-compile-references.ps1")
    lock_call = build_wrapper.find("[IO.File]::Open")
    first_assert = build_wrapper.find("assert-v25-compile-reference-state.ps1")
    child_build = build_wrapper.find("& dotnet @arguments")
    second_assert = build_wrapper.find("assert-v25-compile-reference-state.ps1", first_assert + 1)
    dispose = build_wrapper.find("$locks[$index].Dispose()")
    if min(snapshot_call, lock_call, first_assert, child_build, second_assert, dispose) < 0 or not (
        snapshot_call < lock_call < first_assert < child_build < second_assert < dispose
    ):
        errors.append(
            "V25 locked-reference build ordering must be snapshot -> acquire locks -> verify -> child build -> reverify -> dispose locks"
        )

if ci_workflow:
    ci_markers = {
        "locked plugin build name": "Build BricsCAD V25 plugin against locked reference generations",
        "locked local-qualification build name": "Build #3681 local V25 qualification harness against locked reference generations",
        "shared wrapper invocation": ".\\scripts\\build-v25-with-stable-references.ps1",
        "plugin snapshot": "qs3d-v25-plugin-reference-snapshot",
        "local qualification snapshot": "qs3d-v25-local-qualification-reference-snapshot",
        "plugin project": "src\\QS3D.BricsCAD.V25\\QS3D.BricsCAD.V25.csproj",
        "qualification project": "tests\\QS3D.BricsCAD.V25.LocalQualification\\QS3D.BricsCAD.V25.LocalQualification.csproj",
    }
    for label, marker in ci_markers.items():
        if marker not in ci_workflow:
            errors.append(f"Shared CI is missing {label}: {marker}")
    if ci_workflow.count(".\\scripts\\build-v25-with-stable-references.ps1") < 2:
        errors.append("Shared CI must route both V25 hosted build boundaries through the locked-reference wrapper")

if errors:
    print("V25 compile-reference contract preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print(
    "V25 compile-reference contract preflight PASS: "
    + ", ".join(sorted(required_files))
    + " are synchronized across workflows and Shared CI binds both hosted V25 builds to locked, verified reference snapshots from one admitted source set."
)
