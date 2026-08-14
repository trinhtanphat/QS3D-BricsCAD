#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PROBE = ROOT / "src/QS3D.BricsCAD.V25/PlanTo3DP02RuntimeProbeCommands.cs"
PRODUCTION = ROOT / "src/QS3D.BricsCAD.V25/PlanTo3DCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-plan-to-3d-p02.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
WORKFLOW = ROOT / "docs/PLAN-TO-3D-WORKFLOW.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-14-codex-local014-plan2d-p02-preparation.md"
errors = []

for path in (PROBE, PRODUCTION, RUNNER, HELPER, WORKFLOW, INBOX, CLAIM):
    if not path.is_file():
        errors.append("missing Plan-to-3D P02 file: " + str(path.relative_to(ROOT)))

if PRODUCTION.is_file():
    text = PRODUCTION.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DCONVERT2D", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        '[CommandMethod("QS3DPLAN2WALLS", CommandFlags.Modal | CommandFlags.UsePickSet)]',
        'public void Convert2D() => ConvertPlanWalls("QS3DCONVERT2D", promptStyle: false);',
        'public void PlanToWalls() => ConvertPlanWalls("QS3DPLAN2WALLS", promptStyle: false);',
        'PolylineWallSolidBuilder.BuildSelected(document, project, ElementCategory.ArchitecturalWall)',
        'regenerator.RegenerateDirtySubset(project, new[] { element.Id })',
        'var family = PreferredWallFamily(project);',
    ):
        if token not in text:
            errors.append("production Plan-to-3D quick contract missing token: " + token)
    if "PlanTo3DP02" in text or "QS3DPLAN2DP02" in text:
        errors.append("production PlanTo3DCommands must not depend on the automation-only P02 probe")

if PROBE.is_file():
    text = PROBE.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DPLAN2DP02PREPARE", CommandFlags.Modal)]',
        '[CommandMethod("QS3DPLAN2DP02SELECTALIAS", CommandFlags.Modal)]',
        '[CommandMethod("QS3DPLAN2DP02VERIFY", CommandFlags.Modal)]',
        'ResultVariable = "QS3D_PLAN_TO_3D_P02_RESULT"',
        'NonceVariable = "QS3D_PLAN_TO_3D_P02_NONCE"',
        'schema=QS3D_PLAN_TO_3D_P02_RUNTIME_V1',
        'EndsWith(".plan-to-3d-p02-probe-copy.dwg"',
        'nativeUnit != LengthUnit.Millimeter',
        'ProjectFamilyService.Create(',
        'ProjectFamilyService.SetProperty(project, family.Id, "ThicknessM"',
        'ProjectFamilyService.SetProperty(project, family.Id, "HeightM"',
        'ProjectFamilyService.SetProperty(project, family.Id, "BottomOffsetM"',
        'ProjectFamilyActivationService.SetActive(project, family.Id)',
        'PreferredThicknessM = 0.31d',
        'PreferredHeightM = 4.2d',
        'PreferredBottomOffsetM = 0.45d',
        'ProbeSourceKind.QuickLine',
        'ProbeSourceKind.AliasOpenPolyline',
        'polyline.AddVertexAt(0',
        'polyline.AddVertexAt(1',
        'polyline.Closed = false',
        'SemanticCaptureService.Capture(document, ElementCategory.Beam)',
        'unrelated.MarkDirty(ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity)',
        'RequireUnrelatedState(state)',
        'current.Dirty != snapshot.Dirty',
        'current.UpdatedUtc != snapshot.UpdatedUtc',
        'RequireSourcesUnchanged(state)',
        'GeneratedGeometryService.FindMatchingOwnedHandles',
        'GeneratedGeometryService.HasMatchingOwnership',
        'CadHandleService.GetLiveSolidHandles',
        'preferred_family_applied_count=2',
        'unrelated_dirty_preserved=true',
        'qualification_boundary=P02_QUICK_ALIAS_POLYLINE_FAMILY_DIRTY_ONLY',
        'production_local014_qualified=false',
        'WriteMarkerAtomic(resultPath',
        'FileMode.CreateNew',
        'File.Move(tempPath, fullPath)',
    ):
        if token not in text:
            errors.append("Plan-to-3D P02 probe missing contract token: " + token)

    for forbidden in (
        "new PlanTo3DCommands()",
        "WallSolidBuilder.BuildSelectedLineWalls",
        "PolylineWallSolidBuilder.BuildSelected",
        "ProjectContextCoordinator.Save",
        "QS3DCONVERT2DADV",
    ):
        if forbidden in text:
            errors.append("Plan-to-3D P02 probe must not bypass/expand the production command boundary: " + forbidden)

    marker_start = text.find("WriteMarkerAtomic(resultPath, new[]")
    marker_end = text.find("_state = null;", marker_start)
    marker = text[marker_start:marker_end] if marker_start >= 0 and marker_end > marker_start else ""
    for forbidden in (
        "handle=", "element_id=", "project_id=", "family_id=", "family_name=",
        "drawing_path=", "profile=", "layer=", "xdata=", "exception=", "message="
    ):
        if forbidden in marker.lower():
            errors.append("Plan-to-3D P02 PASS marker leaks identity/detail field: " + forbidden)

    failure_start = text.find("private static void TryWriteFailure")
    failure_end = text.find("private static void WriteMarkerAtomic", failure_start)
    failure = text[failure_start:failure_end]
    for forbidden in ("Exception", ".Message", ".StackTrace", ".GetType("):
        if forbidden in failure:
            errors.append("Plan-to-3D P02 FAIL marker exposes exception detail: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[switch]$ConfirmDisposableCopy',
        '[ValidateRange(30, 900)][int]$StartupTimeoutSeconds = 240',
        '[string]::IsNullOrWhiteSpace($Profile)',
        '*.plan-to-3d-p02-probe-copy.dwg',
        'QS3D_PLAN_TO_3D_P02_RESULT',
        'QS3D_PLAN_TO_3D_P02_NONCE',
        '. $windowInteropPath',
        'Close-Qs3dProxyInformationDialog -Process $process',
        '"PICKFIRST", "1"',
        '"QS3DPLAN2DP02PREPARE"',
        '"QS3DCONVERT2D"',
        '"QS3DPLAN2DP02SELECTALIAS"',
        '"QS3DPLAN2WALLS"',
        '"QS3DPLAN2DP02VERIFY"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'PluginDll must be the exact repository x64 Release V25 build output.',
        'ArtifactDir must stay outside the repository',
        'status --porcelain --untracked-files=normal',
        'Plan-to-3D P02 runtime qualification requires a clean exact-SHA worktree.',
        'Close existing BricsCAD processes before starting the isolated Plan-to-3D P02 runtime probe.',
        '$projectSidecar + ".lock"',
        '[IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")',
        '[IO.Path]::ChangeExtension($DrawingCopy, ".bak")',
        'ArtifactDir must be empty.',
        'Stop-Qs3dLaunchedProcess -Process $process',
        'Stop-Process -Id $Process.Id -Force -ErrorAction Stop',
        'Remove-Qs3dPrivateArtifacts -Paths $privateDrawingArtifacts',
        'drawing_copy_sha256_before',
        'drawing_copy_sha256_after',
        'process_cleanup_verified = $true',
        'script_cleanup_verified = $true',
        'private_drawing_state_cleanup_verified = $true',
        'Require-Qs3dValue -Marker $marker -Key "quick_command_count" -Expected "2"',
        'Require-Qs3dValue -Marker $marker -Key "source_open_polyline_count" -Expected "1"',
        'Require-Qs3dValue -Marker $marker -Key "preferred_family_applied_count" -Expected "2"',
        'Require-Qs3dValue -Marker $marker -Key "unrelated_dirty_preserved" -Expected "true"',
        'Require-Qs3dValue -Marker $marker -Key "qualification_boundary" -Expected "P02_QUICK_ALIAS_POLYLINE_FAMILY_DIRTY_ONLY"',
        'Require-Qs3dValue -Marker $marker -Key "production_local014_qualified" -Expected "false"',
        'Restore-EnvironmentValue -Name "QS3D_PLAN_TO_3D_P02_RESULT"',
        'Restore-EnvironmentValue -Name "QS3D_PLAN_TO_3D_P02_NONCE"',
    ):
        if token not in text:
            errors.append("Plan-to-3D P02 runner missing contract token: " + token)

    for forbidden in ("Get-Process -Name '*'", "Process.GetProcesses", "SendKeys", "SetForegroundWindow"):
        if forbidden in text:
            errors.append("Plan-to-3D P02 runner contains broad process/window action: " + forbidden)
    stop_start = text.find("function Stop-Qs3dLaunchedProcess")
    stop_end = text.find("function Remove-Qs3dPrivateArtifacts", stop_start)
    stop_body = text[stop_start:stop_end]
    for forbidden in ("SilentlyContinue", "catch { }"):
        if forbidden in stop_body:
            errors.append("Plan-to-3D P02 launched-process cleanup must fail visible: " + forbidden)
    success_start = text.find("Require-Qs3dValue -Marker $marker -Key \"production_local014_qualified\"")
    success_end = text.find("$metadata = [ordered]@{", success_start)
    success = text[success_start:success_end]
    success_stop = success.find("Stop-Qs3dLaunchedProcess -Process $process")
    success_script = success.find("if (Test-Path -LiteralPath $scriptPath) { Remove-Item")
    success_private = success.find("Remove-Qs3dPrivateArtifacts -Paths $privateDrawingArtifacts")
    success_hash = success.find("$drawingHashAfter =")
    if min(success_stop, success_script, success_private, success_hash) < 0 or not (
        success_stop < success_script < success_private < success_hash
    ):
        errors.append("Plan-to-3D P02 success cleanup must stop the launched process, delete the private script and remove disposable drawing state before hash/metadata verification")
    if "Plan-to-3D P02 unexpectedly created private drawing state." in text:
        errors.append("Plan-to-3D P02 runner must clean allowlisted disposable drawing state before asserting its absence")
    metadata_start = text.find("$metadata = [ordered]@{")
    metadata_end = text.find("$metadata | ConvertTo-Json", metadata_start)
    metadata = text[metadata_start:metadata_end]
    for forbidden in ("profile =", "drawing_path", "artifact_path", "plugin_path", "handle", "element_id", "family_id"):
        if forbidden in metadata.lower():
            errors.append("Plan-to-3D P02 metadata leaks private/identity field: " + forbidden)

if WORKFLOW.is_file():
    text = WORKFLOW.read_text(encoding="utf-8")
    for token in (
        "LOCAL-014", "P02", "QS3DPLAN2DP02PREPARE", "QS3DPLAN2DP02VERIFY",
        "test-bricscad-v25-plan-to-3d-p02.ps1", "SOURCE_READY", "PENDING_LOCAL",
        "QS3DCONVERT2D", "QS3DPLAN2WALLS", "open straight POLYLINE",
    ):
        if token not in text:
            errors.append("Plan-to-3D workflow missing P02 handoff token: " + token)

if INBOX.is_file():
    text = INBOX.read_text(encoding="utf-8")
    start = text.find("## LOCAL-014")
    end = text.find("\n## LOCAL-", start + 1)
    section = text[start:] if start >= 0 and end < 0 else text[start:end]
    for token in (
        "P02 source-ready handoff", "QS3DPLAN2DP02PREPARE", "QS3DPLAN2DP02VERIFY",
        "test-bricscad-v25-plan-to-3d-p02.ps1", "open straight POLYLINE",
        "unrelated dirty", "SOURCE_READY", "PENDING_LOCAL",
    ):
        if token not in section:
            errors.append("LOCAL-014 missing Plan-to-3D P02 handoff token: " + token)
    if "Status: PASS" in section:
        errors.append("LOCAL-014 must not be promoted by source-only P02 preparation")

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ("LOCAL-014/P02", "automation-only", "No BricsCAD process", "PENDING_LOCAL"):
        if token not in text:
            errors.append("Plan-to-3D P02 claim missing source/runtime boundary token: " + token)
    if "Status: `ACTIVE`" not in text and "Status: `COMPLETED`" not in text:
        errors.append("Plan-to-3D P02 claim must remain ACTIVE during work or COMPLETED at closeout")

print("QS3D Plan-to-3D P02 runtime probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: automation-only LOCAL-014/P02 prepares both quick aliases, one open straight POLYLINE, preferred ArchitecturalWall Family defaults and unrelated-dirty preservation under an exact-SHA/privacy/cleanup runner while production Plan-to-3D source remains unchanged and licensed evidence stays PENDING_LOCAL.")
