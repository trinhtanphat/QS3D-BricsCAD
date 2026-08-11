#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

planner = ROOT / "src/QS3D.Core/Geometry/WallJunctionPlanner.cs"
adjustment = ROOT / "src/QS3D.Core/Geometry/WallJunctionAdjustmentPlanner.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/WallJunctionCommands.cs"
snap_command = ROOT / "src/QS3D.BricsCAD.V25/WallJunctionSnapCommands.cs"
hub = ROOT / "src/QS3D.BricsCAD.V25/UI/DomainHubWindow.xaml"
smoke = ROOT / "tests/QS3D.Core.SmokeTests/WallJunctionRegressionSmoke.cs"
adjustment_smoke = ROOT / "tests/QS3D.Core.SmokeTests/WallJunctionAdjustmentSmoke.cs"
registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

for path in (planner, adjustment, command, snap_command, hub, smoke, adjustment_smoke, registration):
    if not path.is_file():
        errors.append("missing wall-junction file: " + str(path.relative_to(ROOT)))

if planner.is_file():
    text = planner.read_text(encoding="utf-8")
    for needle in (
        "WallJunctionKind",
        "WallJunctionKind.Straight",
        "WallJunctionKind.L",
        "WallJunctionKind.T",
        "WallJunctionKind.X",
        "active = new List<SegmentInfo>()",
        "Intersections(other, current, tolerance)",
        "CandidateIndex",
        "TryQuantize",
        "_unindexed",
        "segment.Start.DistanceTo(segment.End)",
        "ParallelDirectionEpsilon",
        "CrossFinite",
        "angularToleranceRadians",
        "Duplicate wall segment id",
        "MaxSegments",
    ):
        if needle not in text:
            errors.append("wall junction planner guard missing: " + needle)

if adjustment.is_file():
    text = adjustment.read_text(encoding="utf-8")
    for needle in (
        "WallEndpointKind",
        "WallEndpointAdjustment",
        "WallJunctionAdjustmentPlan",
        "new WallJunctionPlanner().Plan",
        "junctionTolerance",
        "movementEpsilon",
        "ambiguous equally-near junction targets",
        "would collapse segment",
    ):
        if needle not in text:
            errors.append("wall junction adjustment guard missing: " + needle)

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DWALLJUNCTIONS"',
        "CadSelectionGuard.AcquireCurrentSelection(document)",
        "if (selectedIds.Length == 0)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "project = previewProject;",
        "project == null ? 0.005d",
        "project == null ? 0.002d",
        "ReadSelection(document, selectedIds, sagitta, planarityTolerance)",
        "new WallJunctionAdjustmentPlanner().Plan",
        'MetadataNumber(project, "WallJunctionToleranceM", 0.005d',
        'MetadataNumber(project, "WallArcSagittaM", 0.002d',
        'MetadataNumber(project, "WallJunctionPlanarityToleranceM", tolerance',
        "referenceElevationM",
        "line.StartPoint.Z",
        "line.EndPoint.Z",
        "polyline.Elevation",
        "EnsureElevation",
        "plan-view đồng phẳng",
        "BulgeArcTessellator.Tessellate",
        "SnapPlan=",
    ):
        if needle not in text:
            errors.append("wall junction command guard missing: " + needle)

    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("QS3DWALLJUNCTIONS analysis must not create a replacement project")

    method_start = text.find("public void AnalyzeWallJunctions()")
    helper_start = text.find("private static IReadOnlyList<WallAxisSegment> ReadSelection", method_start + 1)
    method = text[method_start:helper_start] if method_start >= 0 and helper_start > method_start else ""
    lifecycle = (
        method.find("CadSelectionGuard.AcquireCurrentSelection(document)"),
        method.find("if (selectedIds.Length == 0)"),
        method.find("ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)"),
        method.find("project = previewProject;"),
        method.find("var tolerance = project == null ? 0.005d"),
        method.find("ReadSelection(document, selectedIds, sagitta, planarityTolerance)"),
        method.find("new WallJunctionAdjustmentPlanner().Plan"),
    )
    if min(lifecycle) < 0:
        errors.append("cannot isolate QS3DWALLJUNCTIONS selection/read-only-project/analysis lifecycle")
    elif tuple(sorted(lifecycle)) != lifecycle:
        errors.append("QS3DWALLJUNCTIONS must acquire/nonempty-check selection before read-only project lookup, then read geometry and plan")

    for forbidden in (
        "ExistingProjectMutationContext",
        "AuditTrail.ForProject",
        "ProjectContextCoordinator.GetOrCreate",
        "ProjectContextCoordinator.Save(",
        "ProjectContextCoordinator.TrySavePending",
        ".Touch(",
        ".Record(",
    ):
        if forbidden in method:
            errors.append("QS3DWALLJUNCTIONS analysis must remain read-only; forbidden mutation surface: " + forbidden)

if snap_command.is_file():
    text = snap_command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DWALLSNAPPREVIEW"',
        'CommandMethod("QS3DWALLSNAPAPPLY"',
        "WallJunctionAdjustmentPlanner",
        "PreviewPlanHashKey",
        "PreviewSourceFingerprintKey",
        "SHA256.Create()",
        "preview không còn khớp" if False else "Preview không còn khớp",
        "ElementDirtyFlags.Geometry | ElementDirtyFlags.Quantity",
        "wallHandles.Contains(handle)",
        "Wall Snap không tự chỉnh bulged/curved POLYLINE",
        'AuditTrail.ForProject(project).Record("wall.junction.snap.preview"',
        'AuditTrail.ForProject(project).Record("wall.junction.snap.apply"',
    ):
        if needle not in text:
            errors.append("wall snap preview/apply guard missing: " + needle)

if hub.is_file():
    text = hub.read_text(encoding="utf-8")
    if 'Tag="QS3DWALLJUNCTIONS"' not in text:
        errors.append("Domain Hub does not expose QS3DWALLJUNCTIONS")
    for command_name in ("QS3DWALLSNAPPREVIEW", "QS3DWALLSNAPAPPLY"):
        if 'Tag="' + command_name + '"' not in text:
            errors.append("Domain Hub does not expose " + command_name)

if smoke.is_file():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "StraightContinuation();",
        "LCorner();",
        "TJunction();",
        "XJunction();",
        "NearEndpointSnapsByTolerance();",
        "HugeCoordinateCrossingUsesFallbackIndex();",
        "RejectsDuplicateIdsAndInvalidCoordinates();",
    ):
        if needle not in text:
            errors.append("wall junction regression missing: " + needle)

if adjustment_smoke.is_file():
    text = adjustment_smoke.read_text(encoding="utf-8")
    for needle in (
        "NearEndpointProducesSnap();",
        "ExactJunctionNeedsNoMove();",
        "TJunctionInteriorNeedsNoEndpointMove();",
        "RejectsCollapsingAdjustment();",
    ):
        if needle not in text:
            errors.append("wall junction adjustment regression missing: " + needle)

if registration.is_file():
    text = registration.read_text(encoding="utf-8")
    if "WallJunctionRegressionSmoke.Run();" not in text:
        errors.append("WallJunctionRegressionSmoke is not registered")
    if "WallJunctionAdjustmentSmoke.Run();" not in text:
        errors.append("WallJunctionAdjustmentSmoke is not registered")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: deterministic wall-junction topology, non-creating selection-first read-only analysis, review-gated endpoint snap apply, spatial indexing, finite-safe/coplanar CAD analysis, command/UI wiring and regression coverage are present.")
