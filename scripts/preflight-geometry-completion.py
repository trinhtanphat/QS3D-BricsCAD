#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Geometry/WallFootprintEngine.cs",
    "src/QS3D.Core/Geometry/OpeningCutPlanner.cs",
    "src/QS3D.Core/Geometry/PolylineOpeningCutPlanner.cs",
    "src/QS3D.Core/Rebar/RectangularRebarLayoutPlanner.cs",
    "src/QS3D.Core/Rebar/LinearRebarLayoutPlanner.cs",
    "src/QS3D.Core/Rebar/RebarShapePath.cs",
    "src/QS3D.Core/Rebar/ProjectRebarShapePlanner.cs",
    "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs",
    "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/OpeningBooleanCommands.cs",
    "src/QS3D.BricsCAD.V25/RebarGeometryCommands.cs",
    "src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs",
    "src/QS3D.BricsCAD.V25/RebarHealthCommands.cs",
    "src/QS3D.BricsCAD.V25/ShapeRebarHealthCommands.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs",
    "src/QS3D.BricsCAD.V25/Commands.cs",
    "src/QS3D.BricsCAD.V25/ModelReviewCommands.cs",
    "src/QS3D.BricsCAD.V25/WallJunctionCommands.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs",
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml",
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs",
    "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs",
    "src/QS3D.BricsCAD.V25/UI/ViewModels/PropertyRowViewModel.cs",
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml",
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/LinearRebarLayoutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/PolylineOpeningCutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/RebarShapeGeometrySmoke.cs",
    "tests/QS3D.Core.SmokeTests/ProjectRebarShapeSmoke.cs",
    "tests/QS3D.Core.SmokeTests/RebarOwnershipHealthSmoke.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing geometry-completion file: " + relative)

for xaml_relative in ("src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml", "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"):
    xaml_path = ROOT / xaml_relative
    if xaml_path.is_file():
        try:
            ET.parse(xaml_path)
        except ET.ParseError as exc:
            errors.append(xaml_relative + " is not well-formed XML/XAML: " + str(exc))

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
    "src/QS3D.Core/Rebar/RebarShapePath.cs": [
        "MaxLegs", "RebarShapeLegsM", "RebarShapeTurnsDeg", "ValidateTotal", "Unsupported RebarShapeCode"
    ],
    "src/QS3D.Core/Rebar/ProjectRebarShapePlanner.cs": [
        "ProjectRebarScheduleBuilder.Build(project)", "RebarShapePathBuilder.Build", "RebarShapeLegsM", "RebarShapeTurnsDeg", "ProjectRebarShapePlan"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs": [
        "CodePrefix = \"REBAR\"", "CodePrefix = \"SHAPE_REBAR\"",
        "spec.CodePrefix + \"_GENERATED_OWNERSHIP_CONFLICT\"", "spec.CodePrefix + \"_GENERATED_SOLID_MISSING\"",
        "spec.CodePrefix + \"_GENERATED_COUNT_MISMATCH\"", "GeneratedShapeRebarHandles", "InspectShape", "InspectAll",
        "BuildOwnershipIndex", "GeneratedHandleOwnershipPolicy.IsOwnerSlot(property.Key)", "SourceHandles"
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedRebarOwnershipGuard.cs": [
        "CoreOwnershipPolicy.IsOwnerSlot", "CoreOwnershipPolicy.IsRebarOwnerSlot", "CoreOwnershipPolicy.RebarHandleKeys",
        "EnsureOwned", "ownership conflict", "Refusing destructive erase", "SourceHandles", "AddProtected"
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
        "OpeningCutPlanner.Plan", "PolylineOpeningCutPlanner.Plan", "PreparePolylineHost", "PhysicalOpeningCutSolidHandle", "PhysicalOpeningCutFingerprint",
        "BooleanOperationType.BoolSubtract", "FingerprintPart", "HostFingerprint", "curved/bulged wall POLYLINE",
        "ElementCategory.ArchitecturalWall", "ElementCategory.GlassWall", "ElementCategory.WallPier", "ElementCategory.StructuralWall",
        "GeneratedGeometryService.RequireMatchingOwnership"
    ],
    "src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs": [
        "CurvedOpeningFootprintPlanner.Plan", "GeneratedGeometryService.RequireMatchingOwnership",
        "IsGeneratedSolidStale", "PhysicalOpeningCutSolidHandle", "BooleanOperationType.BoolSubtract"
    ],
    "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs": [
        "RectangularRebarLayoutPlanner.Plan", "CreateFrustum", "GeneratedRebarHandles", "RebarBarsAlongWidth", "RebarBarsAlongDepth",
        "processedElements", "GeneratedRebarOwnershipGuard.Build(project)", "ownership.EnsureOwned", "Refusing destructive erase"
    ],
    "src/QS3D.BricsCAD.V25/Cad/ShapeRebarSolidBuilder.cs": [
        "RebarShapePathBuilder.Build", "GeneratedShapeRebarHandles", "GeneratedRebarOwnershipGuard.Build(project)", "ownership.EnsureOwned",
        "BooleanOperationType.BoolUnite", "MaxBarsPerBatch", "OpenSelectedSource", "selectedHandles",
        "Multiple shape rebars require a positive usable distribution span", "Refusing destructive erase",
        "DistributionCentered", "AxisStartsAtBoundary", "edgeInset", "ElementCategory.GlassWall", "ElementCategory.WallPier"
    ],
    "src/QS3D.BricsCAD.V25/ShapeRebarGeometryCommands.cs": [
        'CommandMethod("QS3DREBAR3DSHAPE"', "ShapeRebarSolidBuilder.BuildSelected"
    ],
    "src/QS3D.BricsCAD.V25/ShapeRebarHealthCommands.cs": [
        'CommandMethod("QS3DREBARSHAPEHEALTH"', "GeneratedRebarHealthService().InspectShape", "ParseShapeHandles"
    ],
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs": [
        "QS3DGLASSWALL", "QS3DWALLPIER", "AxisLeftOffsetM", "AxisRightOffsetM", "ThicknessM"
    ],
    "src/QS3D.BricsCAD.V25/Commands.cs": [
        "GeneratedHandleOwnershipPolicy.CollectOwnerHandles(project)",
        "new ComprehensiveModelHealthService().Inspect(project, liveSources, liveGeneratedSolids)"
    ],
    "src/QS3D.BricsCAD.V25/Build3DCommands.cs": [
        'CommandMethod("QS3DBUILD3D"',
        "NativeBuildCapability.Supports(x.Category)", "NativeBuildCapability.IsWallCategory(category)",
        "category == ElementCategory.WallPier", "WallPierProfileSolidBuilder.BuildSelectedLinePiers(document, project)",
        "WallSolidBuilder.BuildSelectedLineWalls(document, project, category)",
        "PolylineWallSolidBuilder.BuildSelected(document, project, category)",
        "StructuralSolidBuilder.BuildSelected(document, project, category)"
    ],
    "src/QS3D.BricsCAD.V25/Cad/NativeBuildCapability.cs": [
        "ElementCategory.ArchitecturalWall", "ElementCategory.GlassWall", "ElementCategory.WallPier",
        "StructuralSolidBuilder.Supports(category)"
    ],
    "src/QS3D.BricsCAD.V25/ModelReviewCommands.cs": [
        "QS3DHIGHLIGHT", "QS3DFOCUS", "QS3DISOLATE", "QS3DUNISOLATE"
    ],
    "src/QS3D.BricsCAD.V25/WallJunctionCommands.cs": [
        "QS3DWALLJUNCTIONS", "WallJunctionAdjustmentPlanner().Plan", "WallJunctionKind.L", "WallJunctionKind.T", "WallJunctionKind.X"
    ],
    "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs": [
        "MatchesSelection", "BoundarySourceHandlesKey", "GeneratedSolidHandle"
    ],
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml": [
        "Bóc chọn", "OnCaptureSelectedClick", "Vẽ 3D", "OnWallJunctionsClick", "PropertyBooleanEditor", "PropertyChoiceEditor",
        "PropertyScopes", "SelectedPropertyScope", "OnResetPropertyClick", "CanReset", "OnFocusSelectedClick", "OnIsolateSelectedClick", "OnUnisolateClick"
    ],
    "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs": [
        "QS3DGLASSWALL", "QS3DWALLPIER", "QS3DFINISH", "QS3DWALLJUNCTIONS", "CommandFor", "QS3DFOCUS", "QS3DISOLATE", "QS3DUNISOLATE", "SelectInspection",
        "SemanticReferenceHandles.MatchesSelection", "SetSelectedElement(element)", "ShowFamilyProperties", "OnResetPropertyClick"
    ],
    "src/QS3D.BricsCAD.V25/UI/ViewModels/PropertyRowViewModel.cs": [
        "BooleanEditor", "ChoiceEditor", "BooleanValue", "Choices", "IsEditable", "CanReset", "ResetValue", "Action? Reset"
    ],
    "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs": [
        "FamilyScope", "InstanceScope", "PropertyScopes", "SelectedPropertyScope", "SetSelectedElement", "LoadInstanceProperties", "ApplyInstanceProperty",
        "DisplayNameFor", "GroupFor", "IsNumericProperty", "Bề dày", "CỐT THÉP", "EditorKindFor", "ChoicesFor", "IsBooleanProperty",
        "ProjectFamilyService.SetProperty", "InheritedInstancesUpdated", "OverridesPreserved", "instance override", "row.CanReset", "Đã đưa", "element.SetProperty(key, next)",
        "string.Equals(previous, next, StringComparison.Ordinal)", "ProjectFamilyService.Rename", "Chọn một cấu kiện semantic trước khi chuyển sang thuộc tính Instance"
    ],
    "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml": [
        'Tag="QS3DGLASSWALL"', 'Tag="QS3DWALLPIER"', 'Tag="QS3DCUTOPENINGS"', 'Tag="QS3DWALLJUNCTIONS"',
        'Tag="QS3DREBAR3D"', 'Tag="QS3DREBAR3DSHAPE"', 'Tag="QS3DREBARHEALTH"', 'Tag="QS3DREBARSHAPEHEALTH"'
    ],
    "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapper.cs": [
        'Button("Vách Kính", "QS3DGLASSWALL")', 'Button("Trụ Tường", "QS3DWALLPIER")',
        'Button("Giao tường", "QS3DWALLJUNCTIONS")', 'Button("Khoét Cửa/Lỗ", "QS3DCUTOPENINGS")',
        'Button("Focus", "QS3DFOCUS")', 'Button("Cô lập", "QS3DISOLATE")', 'Button("Khôi phục", "QS3DUNISOLATE")',
        'Button("Cốt thép cột 3D", "QS3DREBAR3D")', 'Button("Cốt thép shape 3D", "QS3DREBAR3DSHAPE")',
        'Button("Health shape", "QS3DREBARSHAPEHEALTH")'
    ],
    "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs": [
        "StraightWallFootprint", "PolylineWallCorner", "FarOriginWallFootprint", "OpeningCutPlan", "RectangularRebarLayout", "GeneratedRebarHealth",
        "InspectShape", "SHAPE_REBAR_GENERATED_SOLID_MISSING", "InspectAll"
    ],
    "tests/QS3D.Core.SmokeTests/LinearRebarLayoutSmoke.cs": [
        "CountDistributionIsSymmetric", "SpacingDistributionRoundsUpSafely", "AmbiguousModeIsRejected", "ExcessiveBarCountIsRejected"
    ],
    "tests/QS3D.Core.SmokeTests/PolylineOpeningCutSmoke.cs": [
        "ProjectsOntoHorizontalSegment", "ProjectsOntoVerticalSegment", "RejectsCornerCrossingCut", "RejectsFarOpening"
    ],
    "tests/QS3D.Core.SmokeTests/RebarShapeGeometrySmoke.cs": [
        "ModuleInitializer", "Straight()", "LShape()", "UShape()", "CustomTurns"
    ],
    "tests/QS3D.Core.SmokeTests/ProjectRebarShapeSmoke.cs": [
        "BuildsLShapeFromElementProperties", "StraightShapeNeedsNoLegMetadata", "MismatchedLegTotalIsRejected"
    ],
    "tests/QS3D.Core.SmokeTests/RebarOwnershipHealthSmoke.cs": [
        "RebarCannotClaimHostGeneratedSolid", "ShapeCannotClaimAnotherElementsSource", "ShapeHealthSeesColumnRebarConflict"
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
    if "ProjectRebarShapeSmoke.Run();" not in registration_text:
        errors.append("ProjectRebarShapeSmoke is not registered")
    if "RebarOwnershipHealthSmoke.Run();" not in registration_text:
        errors.append("RebarOwnershipHealthSmoke is not registered")

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
for required_command in (
    "QS3DCUTOPENINGS", "QS3DREBAR3D", "QS3DREBAR3DSHAPE", "QS3DREBARHEALTH", "QS3DREBARSHAPEHEALTH",
    "QS3DBUILD3D", "QS3DGLASSWALL", "QS3DWALLPIER", "QS3DWALLJUNCTIONS", "QS3DFOCUS", "QS3DISOLATE", "QS3DUNISOLATE"):
    if required_command not in commands:
        errors.append("missing command: " + required_command)
if len(commands) != len(set(x.upper() for x in commands)):
    errors.append("duplicate CommandMethod names detected")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: TKT wall/opening geometry, far-origin-safe wall/junction math, protected semantic/host handles, rectangular/linear/BBS-shape rebar ownership+health, well-formed BLT-style XAML, typed Family/Instance editors with reset, semantic selection sync and junction/Focus/Isolate workflows are present.")
