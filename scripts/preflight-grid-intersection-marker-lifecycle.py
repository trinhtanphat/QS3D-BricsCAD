#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
service = (ROOT / "src/QS3D.BricsCAD.V25/Cad/GridIntersectionMarkerService.cs").read_text(encoding="utf-8")
commands = (ROOT / "src/QS3D.BricsCAD.V25/GridCommands.cs").read_text(encoding="utf-8")
planner = (ROOT / "src/QS3D.Core/Geometry/GridIntersectionMarkerPlanner.cs").read_text(encoding="utf-8")
identity = (ROOT / "src/QS3D.Core/Geometry/GridIntersectionIdentityPlanner.cs").read_text(encoding="utf-8")
guard = (ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedNativeSourceGuard.cs").read_text(encoding="utf-8")

required_service = [
    'RegAppName = "QS3D_GRID_INTERSECTION"',
    "GridIntersectionPlanner.FindIntersections",
    "GridIntersectionMarkerPlanner.Plan",
    "GridIntersectionIdentityPlanner.BuildPairToken",
    "GridIntersectionIdentityPlanner.BuildIntersectionOwner",
    "ProjectContextCoordinator.RequireBackingStoreUnchanged",
    "document.LockDocument()",
    "StartTransaction()",
    "ValidateExistingAgainstRecords(records, existing)",
    "RequireMatchingMarker(entity, marker, project.ProjectId)",
    "entity.Erase()",
    "transaction.Commit()",
    "MARKER_STALE_GEOMETRY",
    "MARKER_STALE_OWNER",
    "Foreign Grid intersection marker project ownership",
    "Duplicate live Grid intersection owner token",
    "MaxGridSources = 2000",
    "MaxMarkers = 100000",
]
required_commands = [
    'CommandMethod("QS3DGRIDINTERSECTIONS")',
    'CommandMethod("QS3DGRIDINTERSECTIONSSEL"',
    'CommandMethod("QS3DGRIDINTERSECTIONHEALTH")',
    "TryGetReadOnly(document, out var previewProject)",
    "project.ChangeVersion != previewProject.ChangeVersion",
    "Grid intersection project changed after preview/selection; rerun the command.",
    "GridIntersectionMarkerService.Inspect(document, project)",
]
required_identity = [
    'PairTokenPrefix = "GIP1:"',
    'OwnerTokenPrefix = "GIX1:"',
    "BuildPairToken",
    "BuildIntersectionOwner",
    "MaxElementIdLength = 128",
]
required_planner = ["GridIntersectionIdentityPlanner.Assign", "MaxMarkers = 100000"]

missing = []
for token in required_service:
    if token not in service:
        missing.append("service:" + token)
for token in required_commands:
    if token not in commands:
        missing.append("commands:" + token)
for token in required_identity:
    if token not in identity:
        missing.append("identity:" + token)
for token in required_planner:
    if token not in planner:
        missing.append("planner:" + token)
if ".TryAdd(" in service:
    missing.append("service:net48-incompatible Dictionary.TryAdd")
if "CanonicalizePair(" in planner or "IsOwnerForPair(" in service:
    missing.append("marker lifecycle references undeclared identity helper")
if "GridIntersectionMarkerService.RegAppName" not in guard:
    missing.append("generated-source-guard:intersection RegApp")

selected_marker = "if (selectedOnly)"
bind_marker = "var project = ProjectContextCoordinator.GetOrCreate(document);"
freshness_marker = "project.ChangeVersion != previewProject.ChangeVersion"
freshness_message = "Grid intersection project changed after preview/selection; rerun the command."
refresh_marker = "GridIntersectionMarkerService.Refresh"
if selected_marker not in commands or bind_marker not in commands or commands.index(selected_marker) > commands.index(bind_marker):
    missing.append("commands:selected preview/cancel must complete before canonical mutation binding")
elif freshness_marker not in commands or commands.index(bind_marker) > commands.index(freshness_marker):
    missing.append("commands:project freshness check must follow canonical mutation binding")
elif freshness_message not in commands or commands.index(freshness_marker) > commands.index(freshness_message):
    missing.append("commands:project freshness failure must remain fail-closed")
elif refresh_marker not in commands or commands.index(freshness_message) > commands.index(refresh_marker):
    missing.append("commands:project freshness check must precede native marker refresh")
health_start = commands.find("public void InspectIntersectionMarkers()")
refresh_start = commands.find("private static void RefreshIntersectionMarkers(bool selectedOnly)")
if health_start < 0 or refresh_start < 0 or "GetOrCreate(document)" in commands[health_start:refresh_start]:
    missing.append("commands:health inspection must remain read-only/non-creating")

if missing:
    print("Grid intersection marker lifecycle guard FAILED:")
    for item in missing:
        print(" - " + item)
    sys.exit(1)
print("Grid intersection marker lifecycle guard PASS")
