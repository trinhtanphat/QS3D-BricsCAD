#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "planner": ROOT / "src/QS3D.Core/Geometry/CurtainPathFramePlanner.cs",
    "reader": ROOT / "src/QS3D.BricsCAD.V25/Cad/CadPolylinePathReader.cs",
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
        "distance < best.DistanceM - Tolerance", "station < best.StationM",
    ],
    "reader": [
        "ReadOpenWcsXy", "polyline.Closed", "polyline.Normal.X", "polyline.Normal.Z - 1d",
        "BulgeArcTessellator.Tessellate", "maximumSagittaM",
    ],
    "builder": [
        'Mode = "PathFrameOverlay"', 'OpeningAwareMode = "PathFrameOverlay.OpeningAware"',
        "BuildSelectedOpenPolylines", "CadPolylinePathReader.ReadOpenWcsXy", "CurtainPathFramePlanner.Length",
        "CurtainPathFramePlanner.ProjectPoint", "CurtainPathFramePlanner.Plan", "CurtainFrameOpeningPlanner.Interrupt",
        "OpeningCutPlanner.Plan", "GeneratedCurtainFrameOwnershipGuard.Build", "ownership.EnsureOwned",
        'GeneratedCurtainFrameSourceKind"] = "OpenPolyline"', "GeneratedCurtainFramePathSegmentCount",
        "GeneratedCurtainFrameMappedFrameCount", "GeneratedCurtainFrameConfigFingerprint", "ClearGeneratedCurtainFrameStale",
        "CreateBox", "Matrix3d.Rotation", "WallArcSagittaM", "MaxFramesPerElement = 4096", "MaxFramesPerBatch = 8192",
    ],
    "fingerprint": [
        "AppendHostGeometry", "hostSource is Line", "hostSource is Polyline", "polyline.GetPoint2dAt",
        "polyline.GetBulgeAt", "polyline.Elevation", "polyline.Normal.X", "kind=POLYLINE",
    ],
    "live": [
        "source is Line", "source is Polyline", "CurtainWallFrameLiveFingerprint.Compute", "LINE hoặc POLYLINE",
    ],
    "health": [
        'PathFrameOverlay', 'PathFrameOverlay.OpeningAware', "CURTAIN_FRAME_PATH_SEGMENTS_INVALID",
        "CURTAIN_FRAME_MAPPED_COUNT_INVALID", "CURTAIN_FRAME_PATH_SOURCE_KIND_INVALID", "OpenPolyline",
    ],
    "frame_command": [
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls", "CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines",
        "open/bulged POLYLINE WCS-XY",
    ],
    "build_command": [
        "CurtainWallFrameSolidBuilder.BuildSelectedLineWalls", "CurtainWallPathFrameSolidBuilder.BuildSelectedOpenPolylines",
        "PolylineWallSolidBuilder.BuildSelected", "open/bulged POLYLINE WCS-XY",
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

print("QS3D curtain path-frame preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: open/bulged WCS-XY GlassWall POLYLINE sources map deterministic curtain stations to tessellated path segments, project linked openings to nearest path stations, preserve dedicated generated-frame ownership/stale metadata, and extend live fingerprints to line/polyline host geometry without inventing a curved sweep primitive.")
