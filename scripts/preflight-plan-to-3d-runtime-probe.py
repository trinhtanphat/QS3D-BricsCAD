#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/PlanTo3DRuntimeProbeCommands.cs"
WALL_BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-plan-to-3d.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
WORKFLOW = ROOT / "docs/PLAN-TO-3D-WORKFLOW.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
errors = []

for path in (COMMAND, WALL_BUILDER, RUNNER, HELPER, WORKFLOW, INBOX):
    if not path.is_file():
        errors.append("missing Plan-to-3D runtime probe file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        'CommandMethod("QS3DPLAN2DPROBE", CommandFlags.Modal)',
        'ResultVariable = "QS3D_PLAN_TO_3D_RESULT"',
        'NonceVariable = "QS3D_PLAN_TO_3D_NONCE"',
        'schema=QS3D_PLAN_TO_3D_RUNTIME_V1',
        'EndsWith(".plan-to-3d-probe-copy.dwg"',
        'nativeUnit != LengthUnit.Millimeter',
        'existingProject.Elements.Count != 0',
        'document.Editor.SetImpliedSelection(seeds.Select(x => x.Id).ToArray())',
        'new PlanTo3DCommands().Convert2D()',
        'x.Category == ElementCategory.ArchitecturalWall',
        'QS3D.PlanTo3D',
        'RequireNumber(wall, "ThicknessM", 0.2d)',
        'RequireNumber(wall, "HeightM", 3d)',
        'RequireNumber(wall, "BottomOffsetM", 0d)',
        'GeneratedGeometryService.FindMatchingOwnedHandles',
        'GeneratedGeometryService.HasMatchingOwnership',
        'RequireSeedGeometryUnchanged(document, seeds)',
        'PLAN_TO_3D_RUNTIME_SOURCE_GEOMETRY_FAILED',
        'PLAN_TO_3D_RUNTIME_SOURCE_OWNERSHIP_FAILED',
        'PLAN_TO_3D_RUNTIME_FALLBACK_VALUES_FAILED',
        'PLAN_TO_3D_RUNTIME_GENERATED_METADATA_FAILED',
        'PLAN_TO_3D_RUNTIME_GENERATED_OWNERSHIP_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_BOUNDS_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_HANDLE_RESOLUTION_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_SOLID_TYPE_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_XDATA_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_LENGTH_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_THICKNESS_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_MIN_Z_FAILED',
        'PLAN_TO_3D_RUNTIME_NATIVE_MAX_Z_FAILED',
        'PLAN_TO_3D_RUNTIME_SOURCE_SET_FAILED',
        'RequireGeneratedSolidBounds(document, project, wall, generatedHandle, seed.LengthM)',
        'CadHandleService.GetLiveSolidHandles(document, generatedHandles)',
        '.Inspect(project, liveSources, liveGenerated)',
        'GeneratedSolidRuntimeHealthService',
        'qualification_boundary=P01_QUICK_POSITIVE_ONLY',
        'production_local014_qualified=false',
        'WriteMarkerAtomic(resultPath',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
    ):
        if token not in text:
            errors.append("Plan-to-3D runtime command missing contract token: " + token)

    marker_start = text.find('WriteMarkerAtomic(resultPath, new[]')
    marker_end = text.find('document.Editor.WriteMessage', marker_start)
    marker = text[marker_start:marker_end] if marker_start >= 0 and marker_end > marker_start else ""
    for forbidden in ("handle=", "element_id=", "drawing_path=", "layer=", "family_name=", "project_id="):
        if forbidden in marker.lower():
            errors.append("Plan-to-3D runtime marker leaks identity field: " + forbidden)

if WALL_BUILDER.is_file():
    text = WALL_BUILDER.read_text(encoding="utf-8")
    create = text.find("solid.CreateBox(length, thickness, height);")
    rotate = text.find("solid.TransformBy(Matrix3d.Rotation(angle, Vector3d.ZAxis, Point3d.Origin));", create)
    place = text.find("solid.TransformBy(Matrix3d.Displacement(new Vector3d(mid.X, mid.Y, mid.Z)));", rotate)
    if min(create, rotate, place) < 0 or not (create < rotate < place):
        errors.append("LINE wall builder must create a centered native box, rotate it, then place it at the resolved wall midpoint")
    forbidden = "solid.TransformBy(Matrix3d.Displacement(new Vector3d(-length / 2d, -thickness / 2d, -height / 2d)));"
    if forbidden in text:
        errors.append("LINE wall builder must not apply a second negative half-dimension displacement to centered Solid3d.CreateBox geometry")

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[switch]$ConfirmDisposableCopy',
        '[ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 240',
        '[string]::IsNullOrWhiteSpace($Profile)',
        '*.plan-to-3d-probe-copy.dwg',
        'QS3D_PLAN_TO_3D_RESULT',
        'QS3D_PLAN_TO_3D_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"TILEMODE", "1"',
        '"INSUNITS", "4"',
        '"UCS", "W"',
        '"NETLOAD", (\'"\' + $PluginDll + \'"\')',
        '"QS3DPLAN2DPROBE"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository',
        'Get-Command git -CommandType Application -ErrorAction Stop | Select-Object -First 1',
        'Git executable is unavailable.',
        'rev-parse HEAD',
        'status --porcelain --untracked-files=normal',
        'Plan-to-3D runtime qualification requires a clean exact-SHA worktree.',
        'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Stop-Process -Id $Process.Id -Force -ErrorAction Stop',
        'Launched BricsCAD Plan-to-3D process did not exit.',
        'git_sha = $gitHead',
        'process_cleanup_verified = $true',
        'script_cleanup_verified = $true',
        'Remove-Item -LiteralPath $scriptPath -Force -ErrorAction Stop',
        'Plan-to-3D runtime script cleanup failed.',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'Require-Qs3dValue -Marker $marker -Key "source_line_count" -Expected "2"',
        'Require-Qs3dValue -Marker $marker -Key "semantic_wall_count" -Expected "2"',
        'Require-Qs3dValue -Marker $marker -Key "generated_solid_count" -Expected "2"',
        'Require-Qs3dValue -Marker $marker -Key "qualification_boundary" -Expected "P01_QUICK_POSITIVE_ONLY"',
        'Require-Qs3dValue -Marker $marker -Key "production_local014_qualified" -Expected "false"',
        'Restore-EnvironmentValue -Name "QS3D_PLAN_TO_3D_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_PLAN_TO_3D_NONCE"',
    ):
        if token not in text:
            errors.append("Plan-to-3D runtime runner missing contract token: " + token)

    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("Plan-to-3D runtime runner contains broad process/window action: " + forbidden)
    stop_start = text.find("function Stop-Qs3dLaunchedProcess")
    stop_end = text.find("if ([Environment]::OSVersion.Platform", stop_start)
    stop_body = text[stop_start:stop_end]
    for forbidden in ("SilentlyContinue", "catch { }"):
        if forbidden in stop_body:
            errors.append("Plan-to-3D runtime process cleanup must fail visible: " + forbidden)

if WORKFLOW.is_file():
    text = WORKFLOW.read_text(encoding="utf-8")
    for token in ("LOCAL-014", "P01", "QS3DPLAN2DPROBE", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Plan-to-3D workflow docs missing runtime-boundary token: " + token)

if INBOX.is_file():
    text = INBOX.read_text(encoding="utf-8")
    start = text.find("## LOCAL-014")
    end = text.find("\n## LOCAL-", start + 1)
    section = text[start:] if start >= 0 and end < 0 else text[start:end]
    for token in ("P01", "QS3DPLAN2DPROBE", "PENDING_LOCAL", "test-bricscad-v25-plan-to-3d.ps1"):
        if token not in section:
            errors.append("LOCAL-014 handoff missing Plan-to-3D P01 runtime token: " + token)
    if "Status: PASS" in section:
        errors.append("LOCAL-014 must not be promoted by its P01 quick-positive probe")

print("QS3D Plan-to-3D runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: automation-only Plan-to-3D P01 uses a clean exact-SHA disposable synthetic copy, exercises the real quick command, validates retained sources plus live owned wall solids and health, records sanitized aggregate evidence, and keeps the broader LOCAL-014 matrix PENDING_LOCAL.")
