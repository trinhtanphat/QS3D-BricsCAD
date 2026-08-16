#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Domain/AutoRoomLifecycle.cs",
    "src/QS3D.Core/Services/RoomFinishSynchronizationService.cs",
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
    "tests/QS3D.Core.SmokeTests/RoomFinishSynchronizationSmoke.cs",
    "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs",
    "tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs",
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing auto-room lifecycle file: " + rel)

lifecycle = ROOT / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs"
if lifecycle.exists():
    text = lifecycle.read_text(encoding="utf-8")
    for needle in (
        "FindBySourceSignature", "MarkStaleForSelection", "IsExcludedFromQuantity",
        "BoundaryStateStale", "NormalizeSourceHandles", "BoundarySourceSignatureKey",
        "SyncFamilyDefaults", "FamilyDefaultSnapshotPrefix", "RoomSourceIdKey",
        "ResolveRoomReferenceId", "IsRoomFinishCategory", "HasStaleAutoRoomAncestor",
        "Conflicting room provenance",
    ):
        if needle not in text: errors.append("auto-room lifecycle guard missing: " + needle)

sync = ROOT / "src/QS3D.Core/Services/RoomFinishSynchronizationService.cs"
if sync.exists():
    text = sync.read_text(encoding="utf-8")
    for needle in (
        "RoomFinishSynchronizationService", "SynchronizeExisting", "RoomFinishIdentityService.FindExisting",
        "AutoRoomLifecycle.ResolveRoomReferenceId", "AutoRoomLifecycle.RoomSourceIdKey",
        "EnsureSingleRoomDependency(finish, room.Id)", "finish.DependsOn.Add(roomId)", "finish.DrawingFingerprint = room.DrawingFingerprint",
        '"OpeningAreaM2"', '"DoorWidthM"', "finish.Properties.Remove(key)",
        "AutoRoomLifecycle.IsStaleAutoRoom(room)", "ReferenceEquals(owned, element)",
    ):
        if needle not in text: errors.append("Room->HT_Phòng synchronization guard missing: " + needle)

command = ROOT / "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs"
if command.exists():
    text = command.read_text(encoding="utf-8")
    for needle in (
        "ProjectStateSnapshot.Capture(project)", "rollback.Restore(project)",
        "AutoRoomLifecycle.FindBySourceSignature", "AutoRoomLifecycle.MarkActive",
        "AutoRoomLifecycle.MarkStaleForSelection", "AutoRoomLifecycle.SyncFamilyDefaults",
        "SyncExistingRoomFinishes", 'audit.Record("RoomBoundaryStale"',
        "element.MarkDirty(ElementDirtyFlags.All)",
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
        "entity is Spline", "MaxSplineSegments", "splineChordM", "GetPointAtDist",
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
    for needle in (
        "var origin = points[0]",
        "compensation",
        "TranslatedCrossFinite",
        "RestoreScaledCrossFinite",
        "CrossFinite",
        "AddLengthCompensated",
    ):
        if needle not in text: errors.append("stable polyline metric guard missing: " + needle)

references = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs"
if references.exists():
    text = references.read_text(encoding="utf-8")
    for needle in ("BoundarySourceHandlesKey", "MatchesSelection", "boundary.All(handles.Contains)", "GeneratedSolidHandle"):
        if needle not in text: errors.append("semantic reference-handle resolver missing: " + needle)

source_resolver = ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs"
if source_resolver.exists():
    text = source_resolver.read_text(encoding="utf-8")
    for needle in (
        "AutoRoomLifecycle.BoundarySourceHandlesKey", "AddGeneratedOwnerHandles", "GeneratedHandleOwnershipPolicy.EnumerateLogicalOwnerHandles", "element.DependsOn",
        "AutoRoomLifecycle.IsRoomFinishCategory(element.Category)", "AutoRoomLifecycle.ResolveRoomReferenceId(project, element)",
    ):
        if needle not in text: errors.append("dependency/provenance-aware source Handle resolver missing: " + needle)

capture = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"
if capture.exists():
    text = capture.read_text(encoding="utf-8")
    for needle in (
        "SemanticReferenceHandles.Intersects", "SyncExistingRoomFinishes", "GenerateRoomFinishes",
        "RoomFinishSynchronizationService.Categories", "RoomFinishSynchronizationService.Synchronize(project, room, finish)",
        "RoomFinishSynchronizationService.SynchronizeExisting(project, room)", "AutoRoomLifecycle.IsStaleAutoRoom",
    ):
        if needle not in text: errors.append("room finish / auto-room integration missing: " + needle)
    for forbidden in ("private static void SyncFinishFromRoom", "private static void EnsureRoomDependency", "Copy(room, finish"):
        if forbidden in text: errors.append("adapter must not retain duplicate Room->finish synchronization logic: " + forbidden)

report = ROOT / "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs"
if report.exists():
    text = report.read_text(encoding="utf-8")
    if "AutoRoomLifecycle.IsExcludedFromQuantity(project, element)" not in text:
        errors.append("BQ must exclude stale auto rooms and room-linked dependents")
    for needle in ("QFirst(element, \"GrossConcreteM3\", \"GrossVolumeM3\")", "QFirstOrFallback", "QFirst(element, \"BottomAreaM2\", \"AreaM2\")"):
        if needle not in text: errors.append("BQ lazy quantity fallback guard missing: " + needle)
    if 'Q(element, "GrossConcreteM3", Q(' in text or 'Q(element, "NetConcreteM3", Q(' in text:
        errors.append("BQ must not eagerly evaluate unused legacy quantity fallbacks")

commands = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
if commands.exists():
    text = commands.read_text(encoding="utf-8")
    if text.count("SemanticReferenceHandles.Get(element)") + text.count("SourceHandleResolver.Resolve") < 3:
        errors.append("BQ/Health/QS3DLOCATE must resolve semantic/dependency reference handles")

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
if review.exists():
    text = review.read_text(encoding="utf-8")
    for needle in (
        'LocateCurrentElement(doc, row.ElementId, "BBS Locate")',
        'LocateCurrentElement(doc, row.ElementId, "Revision Locate")',
        'SourceHandleResolver.Resolve(currentProject, new[] { element.Id })',
    ):
        if needle not in text:
            errors.append("BBS/revision shared locate must resolve current semantic/dependency reference handles: " + needle)

smoke = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs"
if smoke.exists():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "SourceSignatureIsDeterministic();", "ReusesMatchingProvenance();",
        "DuplicateProvenanceIsRejected();", "TopologyChangeMarksStale();",
        "StaleRoomsAndDependentsAreExcludedFromBq();", "RoomFinishProvenanceUsesCanonicalPropertyAndDependency();",
        "OrphanAndConflictingRoomFinishProvenanceAreSafe();", "ReactivationClearsStaleState();",
        "FamilyDefaultsPreserveInstanceOverrides();",
    ):
        if needle not in text: errors.append("auto-room lifecycle regression coverage missing: " + needle)

sync_smoke = ROOT / "tests/QS3D.Core.SmokeTests/RoomFinishSynchronizationSmoke.cs"
if sync_smoke.exists():
    text = sync_smoke.read_text(encoding="utf-8")
    for needle in (
        "RepairsLegacyDependencyScopeAndFingerprint();", "RemovedRoomMetricsClearOldDeductions();",
        "QuantityFallbackIsCanonicalized();", "RejectsInvalidRoomMetric();", "RejectsStaleAutoRoom();", "RejectsForeignProjectObject();",
        "NetFinishAreaM2", "SkirtingLengthM",
    ):
        if needle not in text: errors.append("Room->HT_Phòng lifecycle regression coverage missing: " + needle)

quantity_smoke = ROOT / "tests/QS3D.Core.SmokeTests/ProjectQuantitySmoke.cs"
if quantity_smoke.exists() and "PreferredBqQuantityDoesNotEvaluateUnusedFallbacks();" not in quantity_smoke.read_text(encoding="utf-8"):
    errors.append("BQ lazy-fallback regression coverage missing")

geometry_smoke = ROOT / "tests/QS3D.Core.SmokeTests/GeometryCompletionSmoke.cs"
if geometry_smoke.exists():
    text = geometry_smoke.read_text(encoding="utf-8")
    for needle in ("StableDistanceAndPolylineMetrics();", "RoomBoundaryLargeCoordinates();", "FarOriginWallFootprint();"):
        if needle not in text: errors.append("large-coordinate geometry regression coverage missing: " + needle)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.exists():
    registration_text = registration.read_text(encoding="utf-8")
    for needle in ("AutoRoomLifecycleSmoke.Run();", "RoomFinishSynchronizationSmoke.Run();"):
        if needle not in registration_text: errors.append("room lifecycle smoke is not registered: " + needle)

print("QS3D auto-room lifecycle preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: auto-room input/identity, Room->finish resynchronization, stale/orphan quantity exclusion, lazy BQ fallbacks, rollback, shared current-project semantic locate and overflow-safe large-coordinate geometry guards are present.")
