#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/WallFootprintEngine.cs",
    "src/QS3D.Core/Geometry/OpeningCutPlanner.cs",
    "src/QS3D.Core/Geometry/PolylineOpeningCutPlanner.cs",
    "src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs",
    "src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs",
    "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs",
    "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs",
    "src/QS3D.BricsCAD.V25/RebarGeometryCommands.cs",
    "src/QS3D.BricsCAD.V25/RebarHealthCommands.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "src/QS3D.BricsCAD.V25/Commands.cs",
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/LinearRebarLayoutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/PolylineOpeningCutSmoke.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing geometry-completion file: " + relative)

checks = {
    "src/QS3D.Core/Geometry/WallFootprintEngine.cs": [
        "HasSelfIntersection", "HasPolygonSelfIntersection", "miterLimit", "UsedBevelJoin", "Wall footprint self-intersects", "SignedAreaRelative", "Midpoint(previousOffset, nextOffset)"
    ],
    "src/QS3D.Core/Geometry/OpeningCutPlanner.cs": [
        "HostLengthM", "CenterAlongHostM", "CutterDepthM", "extends beyond the host wall length", "extends above the host wall height", "Midpoint(baseElevation, topElevation"
    ],
    "src/QS3D.Core/Geometry/PolylineOpeningCutPlanner.cs": [
        "MaximumCenterlineOffsetM", "SegmentIndex", "ProjectedCenter", "Tangent", "crosses a polyline wall corner/junction"
    ],
    "src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs": [
        "BarsAlongWidth", "BarsAlongDepth", "CoverM", "DiameterMm", "no usable reinforcement envelope"
    ],
    "src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs": [
        "Specify exactly one of Count or SpacingMm", "MaxBars", "usableSpanM", "ActualSpacingM", "OffsetsM"
    ],
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs": [
        "ElementCategory.GlassWall", "ElementCategory.WallPier", "BuildSelectedLineWalls(Document document, ProjectState project, ElementCategory category)",
        "GeneratedGeometryService.PrepareReplacement(document, transaction, project, element)",
        "GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, category)",
        "GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, category)"
    ],
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs": [
        "WallFootprintEngine", "BulgeArcTessellator.Tessellate", "Region.CreateFromCurves", "CreateExtrudedSolid", "WallJoinMode",
        "ElementCategory.GlassWall", "ElementCategory.WallPier", "BuildSelected(Document document, ProjectState project, ElementCategory category)",
        "GeneratedGeometryService.MarkGenerated(document, transaction, solid, project.ProjectId, element.Id, category)",
        "GeneratedGeometryService.CommitReplacement(project, update.Element, update.PreviousHandle, update.GeneratedHandle, category)"
    ],
    "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs": [
<<<<<<< Updated upstream
        "OpeningCutPlanner.Plan", "PolylineOpeningCutPlanner.Plan", "PreparePolylineHost", "PhysicalOpeningCutSolidHandle", "PhysicalOpeningCutFingerprint",
        "BooleanOperationType.BoolSubtract", "FingerprintPart", "HostFingerprint", "curved/bulged wall POLYLINE",
        "ElementCategory.ArchitecturalWall", "ElementCategory.GlassWall", "ElementCategory.WallPier", "ElementCategory.StructuralWall"
=======
        "OpeningCutPlanner.Plan", "PhysicalOpeningCutSolidHandle", "PhysicalOpeningCutFingerprint", "BooleanOperationType.BoolSubtract", "FingerprintPart", "HostFingerprint",
        "ElementCategory.ArchitecturalWall", "ElementCategory.GlassWall", "ElementCategory.WallPier", "ElementCategory.StructuralWall",
        "GeneratedGeometryService.RequireMatchingOwnership"
>>>>>>> Stashed changes
    ],
    "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs": [
        "RectangularRebarLayoutPlanner.Plan", "CreateFrustum", "GeneratedRebarHandles", "RebarBarsAlongWidth", "RebarBarsAlongDepth",
        "processedElements", "Refusing to orphan or overwrite rebar ownership"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs": [
        "REBAR_GENERATED_OWNERSHIP_CONFLICT", "REBAR_GENERATED_SOLID_MISSING", "REBAR_GENERATED_COUNT_MISMATCH"
    ],
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs": [
        "QS3DGLASSWALL", "QS3DWALLPIER", "AxisLeftOffsetM", "AxisRightOffsetM", "ThicknessM"
    ],
    "src/QS3D.BricsCAD.V25/Commands.cs": [
        "PolylineWallSolidBuilder.BuildSelected", "GeneratedRebarHealthService().Inspect", "ParseGeneratedRebarHandles"
    ],
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml": [
        "Bóc chọn", "OnCaptureSelectedClick", "Vẽ 3D"
    ],
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs": [
        "QS3DGLASSWALL", "QS3DWALLPIER", "QS3DFINISH", "CommandFor"
    ],
    "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs": [
        "DisplayNameFor", "GroupFor", "IsNumericProperty", "Bề dày", "CỐT THÉP"
    ],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": [
        'Tag="QS3DGLASSWALL"', 'Tag="QS3DWALLPIER"', 'Tag="QS3DCUTOPENINGS"', 'Tag="QS3DREBAR3D"', 'Tag="QS3DREBARHEALTH"'
    ],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": [
        'new RibbonButtonSpec("Vách Kính", "QS3DGLASSWALL")', 'new RibbonButtonSpec("Trụ Tường", "QS3DWALLPIER")',
        'new RibbonButtonSpec("Khoét Cửa/Lỗ", "QS3DCUTOPENINGS")', 'new RibbonButtonSpec("Cốt thép 3D", "QS3DREBAR3D")'
    ],
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs": [
        "IsTktWall(category.Value)", "ElementCategory.GlassWall", "ElementCategory.WallPier",
        "WallSolidBuilder.BuildSelectedLineWalls(doc, project, category.Value)", "PolylineWallSolidBuilder.BuildSelected(doc, project, category.Value)"
    ],
    "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs": [
        "StraightWallFootprint", "PolylineWallCorner", "FarOriginWallFootprint", "OpeningCutPlan", "RectangularRebarLayout", "GeneratedRebarHealth"
    ],
    "tests/QS3D.Core.SmokeTests/LinearRebarLayoutSmoke.cs": [
        "CountDistributionIsSymmetric", "SpacingDistributionRoundsUpSafely", "AmbiguousModeIsRejected", "ExcessiveBarCountIsRejected"
    ],
    "tests/QS3D.Core.SmokeTests/PolylineOpeningCutSmoke.cs": [
        "ProjectsOntoHorizontalSegment", "ProjectsOntoVerticalSegment", "RejectsCornerCrossingCut", "RejectsFarOpening"
    ],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.is_file():
    registration_text = registration.read_text(encoding="utf-8")
    if "GeometryCompletionSmoke.Run();" not in registration_text:
        errors.append("GeometryCompletionSmoke is not registered")
    if "LinearRebarLayoutSmoke.Run();" not in registration_text:
        errors.append("LinearRebarLayoutSmoke is not registered")
    if "PolylineOpeningCutSmoke.Run();" not in registration_text:
        errors.append("PolylineOpeningCutSmoke is not registered")

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
for required_command in ("QS3DCUTOPENINGS", "QS3DREBAR3D", "QS3DREBARHEALTH", "QS3DBUILD3D", "QS3DGLASSWALL", "QS3DWALLPIER"):
    if required_command not in commands:
        errors.append("missing command: " + required_command)
if len(commands) != len(set(x.upper() for x in commands)):
    errors.append("duplicate CommandMethod names detected")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: TKT line/polyline wall variants, safe LINE + straight-POLYLINE opening cuts, far-origin-safe footprint math, rectangular + linear rebar planning/health and BLT-style UI workflow guards are present.")
