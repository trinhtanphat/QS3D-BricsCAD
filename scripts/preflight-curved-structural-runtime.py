#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurvedStructuralRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts" / "test-bricscad-v25-curved-structural.ps1"


def fail(message: str) -> None:
    print(f"curved-structural-runtime preflight: FAIL: {message}", file=sys.stderr)
    raise SystemExit(1)


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        fail(f"{label} is missing required token: {token}")


def forbid(text: str, token: str, label: str) -> None:
    if token in text:
        fail(f"{label} contains forbidden token: {token}")


if not COMMAND.is_file():
    fail(f"missing command source: {COMMAND.relative_to(ROOT)}")
if not RUNNER.is_file():
    fail(f"missing runtime runner: {RUNNER.relative_to(ROOT)}")

command = COMMAND.read_text(encoding="utf-8")
runner = RUNNER.read_text(encoding="utf-8")

for token in (
    '[CommandMethod("QS3DCURVEDSTRUCTURALPROBE", CommandFlags.Modal)]',
    'QS3D_CURVED_STRUCTURAL_RESULT',
    'QS3D_CURVED_STRUCTURAL_NONCE',
    'QS3D_CURVED_STRUCTURAL_SOURCE_SHA',
    'curved-structural-probe-copy.dwg',
    'QS3D_CURVED_STRUCTURAL_RUNTIME_V1',
    'RequireAssemblyRevision(typeof(CurvedStructuralRuntimeProbeCommands).Assembly',
    'RequireAssemblyRevision(typeof(ProjectState).Assembly',
    'EntitySnapshotReader.ReadHandles',
    'snapshot.LengthDrawingUnits',
    'snapshot.AreaDrawingUnitsSquared',
    'StructuralSolidBuilder.BuildSelected',
    'GeneratedNativeSourceGuard.HasKnownOwnershipMarker',
    'CadHandleService.Resolve(document, new[] { firstHandle }).Count == 0',
    'new Arc(',
    'new Circle(',
    'new Polyline()',
    'Math.Tan(Math.PI / 8d)',
    'Vector3d.YAxis',
    'polyline.Closed = closed',
    'closed_beam_polyline_fail_closed=true',
    'non_wcs_beam_circle_fail_closed=true',
    'positive_case_count=',
    'rebuild_count=',
    'WriteMarkerAtomic',
):
    require(command, token, "command source")

for case_name in (
    'beam_line',
    'beam_arc',
    'beam_circle',
    'beam_polyline_straight',
    'beam_polyline_curved',
    'slab_circle',
    'column_circle',
    'closed_beam_polyline',
    'non_wcs_beam_circle',
):
    require(command, f'"{case_name}"', "command source")

for token in (
    '[Parameter(Mandatory = $true)][switch]$ConfirmDisposableCopy',
    'ExpectedSourceSha',
    'git -C $repoRoot rev-parse --verify HEAD',
    'git -C $repoRoot status --porcelain=v1 --untracked-files=all',
    'ProductVersion',
    'Get-FileHash -LiteralPath $PluginDll -Algorithm SHA256',
    'curved-structural-probe-copy.dwg',
    'QS3D_CURVED_STRUCTURAL_RESULT',
    'QS3D_CURVED_STRUCTURAL_NONCE',
    'QS3D_CURVED_STRUCTURAL_SOURCE_SHA',
    'QS3DCURVEDSTRUCTURALPROBE',
    'QS3D_CURVED_STRUCTURAL_RUNTIME_V1',
    'Close-Qs3dProxyInformationDialog',
    '[IO.FileAttributes]::ReadOnly',
    'drawingUnwrittenVerified',
    'drawingRestoreVerified',
    'processCleanupVerified',
    'privateStateCleanupVerified',
    'positive_case_count',
    'rebuild_count',
    'closed_beam_polyline_fail_closed',
    'non_wcs_beam_circle_fail_closed',
    'beam_arc_length_m',
    'beam_circle_length_m',
    'beam_polyline_curved_length_m',
    'slab_circle_area_m2',
    'column_circle_area_m2',
):
    require(runner, token, "PowerShell runner")

for forbidden in (
    'source_handle=',
    'generated_handle=',
    'project_id=',
    'error_message=',
    'exception_message=',
    'drawing_path=',
    'plugin_path=',
):
    forbid(command.lower(), forbidden, "command marker contract")

# This is deliberately a source contract only. It must never manufacture a licensed PASS.
forbidden_pass_artifacts = (
    'LOCAL_PASS',
    'P10_PASS',
)
for token in forbidden_pass_artifacts:
    forbid(command, token, "command source")
    forbid(runner, token, "PowerShell runner")

print("curved-structural-runtime preflight: PASS")
