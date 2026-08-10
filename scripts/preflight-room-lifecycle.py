#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Domain/AutoRoomLifecycle.cs",
    "src/QS3D.Core/Reporting/ProjectQuantityReportBuilder.cs",
    "src/QS3D.BricsCAD.V25/Commands.cs",
    "src/QS3D.BricsCAD.V25/ReviewCommands.cs",
    "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs",
    "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs",
    "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs",
    "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs",
]
for rel in required:
    if not (ROOT / rel).exists(): errors.append("missing auto-room lifecycle file: " + rel)

lifecycle = ROOT / "src/QS3D.Core/Domain/AutoRoomLifecycle.cs"
if lifecycle.exists():
    text = lifecycle.read_text(encoding="utf-8")
    for needle in (
        "FindBySourceSignature", "MarkStaleForSelection", "IsExcludedFromQuantity",
        "BoundaryStateStale", "NormalizeSourceHandles", "BoundarySourceSignatureKey",
    ):
        if needle not in text: errors.append("auto-room lifecycle guard missing: " + needle)

command = ROOT / "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs"
if command.exists():
    text = command.read_text(encoding="utf-8")
    for needle in (
        "ProjectStateSnapshot.Capture(project)", "rollback.Restore(project)",
        "AutoRoomLifecycle.FindBySourceSignature", "AutoRoomLifecycle.MarkActive",
        "AutoRoomLifecycle.MarkStaleForSelection", "SyncExistingRoomFinishes",
        'audit.Record("RoomBoundaryStale"',
        "RoomBoundarySegmentReader.ReadCurrentSelection(document, arcSagitta, tolerance, splineChord)",
        "LINE, POLYLINE, ARC hoặc SPLINE plan-view",
    ):
        if needle not in text: errors.append("QS3DROOMAUTO lifecycle/rollback/planar-input wiring missing: " + needle)
    if "SourceHandles.Add" in text: errors.append("auto-room discovery must not claim boundary handles as semantic SourceHandles")

reader = ROOT / "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs"
if reader.exists():
    text = reader.read_text(encoding="utf-8")
    for needle in (
        "planarityToleranceM", "referenceElevationM", "entity is Arc", "arc.EndAngle - arc.StartAngle",
        "BulgeArcTessellator.Tessellate", "normal +Z", "toàn bộ boundary đồng phẳng",
        "entity is Spline", "splineChordM", "MaxSplineSegments", "GetPointAtDist",
    ):
        if needle not in text: errors.append("room boundary LINE/POLYLINE/ARC planarity guard missing: " + needle)

references = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticReferenceHandles.cs"
if references.exists():
    text = references.read_text(encoding="utf-8")
    for needle in ("BoundarySourceHandlesKey", "MatchesSelection", "boundary.All(handles.Contains)", "GeneratedSolidHandle"):
        if needle not in text: errors.append("semantic reference-handle resolver missing: " + needle)

source_resolver = ROOT / "src/QS3D.Core/Services/SourceHandleResolver.cs"
if source_resolver.exists():
    text = source_resolver.read_text(encoding="utf-8")
    for needle in ("AutoRoomLifecycle.BoundarySourceHandlesKey", "GeneratedSolidHandle", "element.DependsOn"):
        if needle not in text: errors.append("dependency-aware source Handle resolver missing: " + needle)

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
    if text.count("SemanticReferenceHandles.Get(element)") + text.count("SourceHandleResolver.Resolve") < 3:
        errors.append("BQ/Health/QS3DLOCATE must resolve semantic/dependency reference handles")

review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
if review.exists():
    text = review.read_text(encoding="utf-8")
    if text.count("SemanticReferenceHandles.Get(element)") + text.count("SourceHandleResolver.Resolve") < 2:
        errors.append("BBS/revision locate must resolve semantic/dependency reference handles")

smoke = ROOT / "tests/QS3D.Core.SmokeTests/AutoRoomLifecycleSmoke.cs"
if smoke.exists():
    text = smoke.read_text(encoding="utf-8")
    for needle in (
        "SourceSignatureIsDeterministic();", "ReusesMatchingProvenance();",
        "DuplicateProvenanceIsRejected();", "TopologyChangeMarksStale();",
        "StaleRoomsAndDependentsAreExcludedFromBq();", "ReactivationClearsStaleState();",
    ):
        if needle not in text: errors.append("auto-room lifecycle regression coverage missing: " + needle)

registration = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
if registration.exists() and "AutoRoomLifecycleSmoke.Run();" not in registration.read_text(encoding="utf-8"):
    errors.append("AutoRoomLifecycleSmoke is not registered")

print("QS3D auto-room lifecycle preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: auto-room planar LINE/POLYLINE/ARC/SPLINE input, provenance reuse, stale reconciliation, rollback, quantity exclusion, finish sync, semantic locate and regression guards are present.")
