#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Domain/AutoRoomLifecycle.cs",
    "src/QS3D.Core/Geometry/Point2.cs",
    "src/QS3D.Core/Geometry/PolylineMetrics.cs",
    "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.BricsCAD.V25/Commands.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs",
    "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs",
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing auto-room lifecycle file: " + rel)

lifecycle = ROOT / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs"
if lifecycle.exists():
    text = lifecycle.read_text(encoding="utf-8")
    for needle in (
        "FindBySourceSignature", "MarkStaleForSelection", "IsExcludedFromQuantity",
        "BoundaryStateStale", "NormalizeSourceHandles", "BoundarySourceSignatureKey",
        "SyncFamilyDefaults", "FamilyDefaultSnapshotPrefix",
    ):
        if needle not in text: errors.append("auto-room lifecycle guard missing: " + needle)

command = ROOT / "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs"
if command.exists():
    text = command.read_text(encoding="utf-8")
    for needle in (
        "ProjectStateSnapshot.Capture(project)", "rollback.Restore(project)",
        "AutoRoomLifecycle.FindBySourceSignature", "AutoRoomLifecycle.MarkActive",
        "AutoRoomLifecycle.MarkStaleForSelection", "AutoRoomLifecycle.SyncFamilyDefaults",
        "SyncExistingRoomFinishes", 'audit.Record("RoomBoundaryStale"',
        "RoomBoundarySegmentReader.ReadCurrentSelection(document, arcSagitta, tolerance, splineChord)",
        "RoomBoundarySplineChordM", "LINE, POLYLINE, ARC hoặc SPLINE plan-view",
        "MetadataNonNegative", "signatureCounts", "legacyId", "activeRoomIds.Add",
        "IdentitySeed(project.ActiveFloorId, project.ActiveZoneId, boundary.Key)",
    ):
        if needle not in text: errors.append("QS3DROOMAUTO lifecycle/identity/planar-input wiring missing: " + needle)
    if "SourceHandles.Add" in text: errors.append("auto-room discovery must not claim boundary handles as semantic SourceHandles")

reader = ROOT / "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs"
if reader.exists():
    text = reader.read_text(encoding="utf-8")
    for needle in (
        "planarityToleranceM", "referenceElevationM", "entity is Arc", "arc.EndAngle - arc.StartAngle",
        "entity is Spline", "MaxSplineSegments", "splineChordM",
        "BulgeArcTessellator.Tessellate", "normal +Z", "toàn bộ boundary đồng phẳng",
    ):
        if needle not in text: errors.append("room boundary LINE/POLYLINE/ARC/SPLINE planarity guard missing: " + needle)

point = ROOT / "src/QS3D.Core/Geometry/Point2.cs"
if point.exists():
    text = point.read_text(encoding="utf-8")
    for needle in ("var scale = Math.Max(ax, ay)", "var ratio = Math.Min(ax, ay) / scale", "Point distance exceeds the supported numeric range"):
        if needle not in text: errors.append("stable Point2 distance guard missing: " + needle)

metrics = ROOT / "src/QS3D.Core/Geometry/PolylineMetrics.cs"
if metrics.exists():
    text = metrics.read_text(encoding="utf-8")
    for needle in ("var origin = points[0]", "compensation", "MultiplyFinite", "AddFinite"):
        if needle not in text: errors.append("stable polyline metric guard missing: " + needle)

references = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs"
if references.exists():
    text = references.read_text(encoding="utf-8")
    for needle in ("BoundarySourceHandlesKey", "MatchesSelection", "boundary.All(handles.Contains)", "GeneratedSolidHandle"):
        if needle not in text: errors.append("semantic reference-handle resolver missing: " + needle)

capture = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"
if capture.exists():
    text = capture.read_text(encoding="utf-8")
    for needle in ("SemanticReferenceHandles.Intersects", "SyncExistingRoomFinishes", "RoomSourceId", "AutoRoomLifecycle.IsStaleAutoRoom"):
        if needle not in text: errors.append("room finish / auto-room integration missing: " + needle)

report = ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs"
if report.exists() and "AutoRoomLifecycle.IsExcludedFromQuantity(project, element)" not in report.read_text(encoding="utf-8"):
    errors.append("BQ must exclude stale auto rooms and direct dependents")

commands = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
if commands.exists():
    text = commands.read_text(encoding="utf-8")
    if text.count("SemanticReferenceHandles.Get(element)") < 3:
        errors.append("BQ/Health/QS3DLOCATE must resolve semantic reference handles")

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
if review.exists() and review.read_text(encoding="utf-8").count("SemanticReferenceHandles.Get(element)") < 2:
    errors.append("BBS/revision locate must resolve semantic reference handles")

smoke = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs"
if smoke.exists():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "SourceSignatureIsDeterministic();", "ReusesMatchingProvenance();",
        "DuplicateProvenanceIsRejected();", "TopologyChangeMarksStale();",
        "StaleRoomsAndDependentsAreExcludedFromBq();", "ReactivationClearsStaleState();",
        "FamilyDefaultsPreserveInstanceOverrides();",
    ):
        if needle not in text: errors.append("auto-room lifecycle regression coverage missing: " + needle)

geometry_smoke = ROOT / "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs"
if geometry_smoke.exists():
    text = geometry_smoke.read_text(encoding="utf-8")
    for needle in ("StableDistanceAndPolylineMetrics();", "RoomBoundaryLargeCoordinates();", "FarOriginWallFootprint();"):
        if needle not in text: errors.append("large-coordinate geometry regression coverage missing: " + needle)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.exists() and "AutoRoomLifecycleSmoke.Run();" not in registration.read_text(encoding="utf-8"):
    errors.append("AutoRoomLifecycleSmoke is not registered")

print("QS3D auto-room lifecycle preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: auto-room LINE/POLYLINE/ARC/SPLINE input, scoped identity, override-safe family sync, stale reconciliation, rollback, quantity exclusion, finish sync, semantic locate and large-coordinate geometry guards are present.")
