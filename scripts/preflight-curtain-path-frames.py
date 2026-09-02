#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "planner": ROOT / "src/QS3D.Core/Geometry/CurtainPathFramePlanner.cs",
    "reader": ROOT / "src/QS3D.BricsCAD.V25/Cad/CadPolylinePathReader.cs",
    "line_builder": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "builder": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs",
    "fingerprint": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveFingerprint.cs",
    "live": ROOT / "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameLiveStateService.cs",
    "health": ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainFrameHealthService.cs",
    "frame_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallFrameCommands.cs",
    "build_command": ROOT / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs",
    "planner_smoke": ROOT / "tests/QS3D.Core.SmokeTests/CurtainPathFramePlannerSmoke.cs",
    "health_smoke": ROOT / "tests/QS3D.Core.SmokeTests/CurtainPathFrameHealthSmoke.cs",
}

for path in files.values():
    if not path.is_file():
        errors.append("missing curtain path frame file: " + str(path.relative_to(ROOT)))

checks = {
    "planner": [
        "class CurtainPathFramePiece", "class CurtainPathFramePlan", "class CurtainPathProjection",
        "CurtainPathFramePlan Plan", "CurtainPathProjection ProjectPoint", "MaxPathPoints = 8192",
        "MaxPieces = 20000", "PathSegmentIndex", "StationStartM", "StationEndM",
        "distance < bestDistance - Tolerance", "candidateStation < currentStation",
    ],
    "reader": [
        "ReadOpenWcsXy", "polyline.Closed", "var normal = polyline.Normal",
        "Math.Abs(normal.X)", "Math.Abs(normal.Z - 1d)",
        "BulgeArcTessellator.Tessellate", "maximumSagittaM",
    ],
    "line_builder": [
        'Mode = "LineFrameOverlay"', 'OpeningAwareMode = "LineFrameOverlay.OpeningAware"',
        "BuildSelectedLineWalls", "CurtainFrameOpeningPlanner.Interrupt", "GeneratedCurtainFrameOwnershipGuard.Build",
        "ownership.EnsureOwned", "GeneratedCurtainFrameConfigFingerprint", "ClearGeneratedCurtainFrameStale",
        "MaxFramesPerElement = 4096", "MaxFramesPerBatch = 8192", 'AuditTrail.ForProject(project).Record("geometry.curtain.frames"',
    ],
    "builder": [
        'Mode = "PathFrameOverlay"', 'OpeningAwareMode = "PathFrameOverlay.OpeningAware"',
        "BuildSelectedOpenPolylines", "CadPolylinePathReader.ReadOpenWcsXy", "CurtainPathFramePlanner.Length",
        "CurtainPathFramePlanner.ProjectPoint", "CurtainPathFramePlanner.Plan", "CurtainFrameOpeningPlanner.Interrupt",
        "OpeningCutPlanner.Plan", "GeneratedCurtainFrameOwnershipGuard.Build", "ownership.EnsureOwned",
        'GeneratedCurtainFrameSourceKind"] = "OpenPolyline"', "GeneratedCurtainFramePathSegmentCount",
        "GeneratedCurtainFrameMappedFrameCount", "GeneratedCurtainFrameConfigFingerprint", "ClearGeneratedCurtainFrameStale",
        "CreateBox", "Matrix3d.Rotation", "WallArcSagittaM", "MaxFramesPerElement = 4096", "MaxFramesPerBatch = 8192",
        'AuditTrail.ForProject(project).Record("geometry.curtain.path.frames"',
    ],
    "fingerprint": [
        "AppendHostGeometry", "hostSource is Line", "hostSource is Polyline", "polyline.GetPoint2dAt",
        "polyline.GetBulgeAt", "polyline.Elevation", "polyline.Normal.X", "kind=POLYLINE",
    ],
    "live": [
        "source is Line", "source is Polyline", "CurtainWallFrameLiveFingerprint.Compute", "LINE hoặc POLYLINE",
        "Live curtain fingerprint chưa được cập nhật; hãy chạy lại Curtain Frames 3D hoặc Health trước khi phát hành.",
    ],
    "health": [
        'PathFrameOverlay', 'PathFrameOverlay.OpeningAware', "CURTAIN_FRAME_PATH_SEGMENTS_INVALID",
        "CURTAIN_FRAME_MAPPED_COUNT_INVALID", "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", "OpenPolyline",
    ],
    "frame_command": [
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls", "CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines",
        "open/bulged POLYLINE WCS-XY", "FinalizeUi(document, message, stampWarning)",
        "QS3DCURTAINFRAMES3D không thể hoàn tất.", "var uiSyncFailed = false;",
        "native update đã hoàn tất; một phần UI không thể đồng bộ.", "TryWriteMessage(document",
    ],
    "build_command": [
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls", "CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines",
        "PolylineWallSolidBuilder.BuildSelected", "open/bulged POLYLINE WCS-XY", "RegenerateDirty(project)",
        "FinalizeUi(document, hostSolids, frameSolids, panelSolids, checked(stamped + panelsStamped), regenerated, stampWarning)", '" UI sync warning: " + ex.Message',
    ],
    "planner_smoke": [
        "ModuleInitializer", "BentPathSplitsFrameAtCorner", "ProjectionUsesNearestPathStation",
        "TessellatedBulgeMapsAcrossSegments", "InvalidPathsAndIntervalsFailClosed",
    ],
    "health_smoke": [
        "ModuleInitializer", 'GeneratedCurtainFrameMode"] = "PathFrameOverlay"',
        'GeneratedCurtainFrameSourceKind"] = "OpenPolyline"', "CURTAIN_FRAME_PATH_SEGMENTS_INVALID",
    ],
}

for key, needles in checks.items():
    path = files[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing curtain path token: " + needle)

for key in ("line_builder", "builder"):
    if files[key].is_file():
        text = files[key].read_text(encoding="utf-8")
        if "project.Touch();" in text:
            errors.append(str(files[key].relative_to(ROOT)) + " must keep redundant batch project.Touch removed; AuditTrail.Record owns revision advancement")

if files["builder"].is_file():
    text = files["builder"].read_text(encoding="utf-8")
    forbidden = [
        'GeneratedSolidHandle"] = string.Join',
        "polyline.Closed = true",
        "SweepAlongPath",
    ]
    for needle in forbidden:
        if needle in text:
            errors.append("curtain path frame builder contains forbidden ownership/geometry shortcut: " + needle)

for key in ("line_builder", "builder"):
    if files[key].is_file():
        text = files[key].read_text(encoding="utf-8")
        if "Editor.Regen(" in text:
            errors.append(str(files[key].relative_to(ROOT)) + " native builder must remain UI-free after CAD/semantic commit; Regen belongs to command FinalizeUi")

if files["build_command"].is_file():
    text = files["build_command"].read_text(encoding="utf-8")
    regen_index = text.find("RegenerateDirty(project)")
    first_native_index = text.find("WallSolidBuilder.BuildSelectedLineWalls")
    if regen_index < 0 or first_native_index < 0 or regen_index > first_native_index:
        errors.append("QS3DCURTAIN3D must resolve semantic regeneration before the first native host/frame transaction")

if files["frame_command"].is_file():
    text = files["frame_command"].read_text(encoding="utf-8")
    finalize_index = text.find("private static void FinalizeUi")
    if finalize_index < 0:
        errors.append("CurtainWallFrameCommands.cs missing non-fatal post-commit FinalizeUi boundary")
    else:
        finalize = text[finalize_index:]
        for token in (
            "var uiSyncFailed = false;",
            "try { PaletteCoordinator.RefreshProject(); } catch { uiSyncFailed = true; }",
            "try { document.Editor.Regen(); } catch { uiSyncFailed = true; }",
            "try { PaletteCoordinator.SetStatus(message); } catch { uiSyncFailed = true; }",
            "TryWriteMessage",
        ):
            if token not in finalize:
                errors.append("CurtainWallFrameCommands.cs stable post-commit FinalizeUi missing: " + token)
        for forbidden in ("ex.Message", "UI sync warning: ", "catch (Exception ex)"):
            if forbidden in text:
                errors.append("CurtainWallFrameCommands.cs must not expose caught host/UI detail: " + forbidden)

if files["live"].is_file():
    text = files["live"].read_text(encoding="utf-8")
    stamp_start = text.find("public static int TryStampSelected")
    inspect_start = text.find("public static IReadOnlyList<ModelHealthIssue> Inspect")
    if min(stamp_start, inspect_start) < 0:
        errors.append("cannot resolve TryStampSelected live-state boundary")
    else:
        stamp = text[stamp_start:inspect_start]
        if "ex.Message" in stamp or "catch (Exception ex)" in stamp:
            errors.append("TryStampSelected must keep post-commit fingerprint warning stable and redact host detail")

if files["build_command"].is_file():
    text = files["build_command"].read_text(encoding="utf-8")
    finalize_index = text.find("private static void FinalizeUi")
    if finalize_index < 0:
        errors.append("CurtainWallBuildCommands.cs missing non-fatal post-commit FinalizeUi boundary")
    else:
        finalize = text[finalize_index:]
        if "catch (Exception ex)" not in finalize or "TryWriteMessage" not in finalize:
            errors.append("CurtainWallBuildCommands.cs post-commit FinalizeUi must fail best-effort")

print("QS3D curtain path-frame preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: line/open-polyline frame builders remain UI-free and AuditTrail-owned, live fingerprint stamping uses a stable post-commit warning, the standalone frame command redacts caught host/UI detail with independent best-effort synchronization, Curtain3D keeps its existing validated orchestration contract, and path/opening/health invariants remain guarded.")
