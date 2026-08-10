#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Services/AutomaticRoomLifecycleService.cs",
    "src/QS3D.Core/Geometry/RoomBoundaryEngine.cs",
    "src/QS3D.BricsCAD.V25/Cad/RoomBoundarySegmentReader.cs",
    "src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs",
    "tests/QS3D.Core.SmokeTests/AutomaticRoomLifecycleSmoke.cs",
    "tests/QS3D.Core.SmokeTests/AutomaticRoomLifecycleRegistration.cs",
]
for rel in required:
    if not (ROOT / rel).exists():
        errors.append("missing auto-room file: " + rel)


def require(rel, tokens):
    path = ROOT / rel
    if not path.exists():
        return
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            errors.append(rel + ": missing " + token)

require("src/QS3D.Core/Services/AutomaticRoomLifecycleService.cs", (
    "NormalizeSourceSignature", "GetSourceSignature", "BuildStableElementId", "ReconcileStale",
    "RemovedRoomIds", "RemovedDependentIds", "RetainedStaleRoomIds", "AutoBoundaryStale",
    "ElementCategory.FloorFinish", "ElementCategory.Waterproofing", "ElementCategory.Skirting",
    "ElementCategory.WallFinish", "ElementCategory.CeilingFinish"
))
require("src/QS3D.BricsCAD.V25/RoomBoundaryCommands.cs", (
    "ProjectStateSnapshot.Capture(project)", "AutomaticRoomLifecycleService.NormalizeSourceSignature",
    "AutomaticRoomLifecycleService.BuildStableElementId", "AutomaticRoomLifecycleService.GetSourceSignature",
    "element.SourceHandles.Clear()", "AutomaticRoomLifecycleService.ReconcileStale",
    "BoundarySourceSignature", "AutoBoundaryManaged", "RoomBoundaryRemove", "RoomBoundaryStale",
    "rollback.Restore(project)"
))
require("tests/QS3D.Core.SmokeTests/AutomaticRoomLifecycleSmoke.cs", (
    "StableIdentityUsesSourceHandles();", "LegacySourceSignatureIsRecovered();",
    "GeneratedFinishesAreRemovedWithStaleRoom();", "ProtectedDependentsRetainStaleRoom();",
    "UnselectedAndCurrentRoomsAreUntouched();"
))
require("tests/QS3D.Core.SmokeTests/AutomaticRoomLifecycleRegistration.cs", (
    "ModuleInitializer", "AutomaticRoomLifecycleSmoke.Run();"
))

print("QS3D auto-room lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: stable auto-room identity, source-handle ownership, stale cleanup, rollback wiring and lifecycle smoke registration are present.")
