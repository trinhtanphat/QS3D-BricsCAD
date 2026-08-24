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

health_start = commands.find("public void InspectIntersectionMarkers()")
refresh_start = commands.find("private static void RefreshIntersectionMarkers(bool selectedOnly)")
next_method = commands.find("private static string ResolveActiveGridSubtype", refresh_start)
if health_start < 0 or refresh_start < 0 or "GetOrCreate(document)" in commands[health_start:refresh_start]:
    missing.append("commands:health inspection must remain read-only/non-creating")
if refresh_start < 0:
    missing.append("commands:missing RefreshIntersectionMarkers method")
else:
    refresh_body = commands[refresh_start:next_method if next_method >= 0 else len(commands)]
    selected_marker = "if (selectedOnly)"
    bind_marker = "var project = ProjectContextCoordinator.GetOrCreate(document);"
    freshness_marker = "project.ChangeVersion != previewProject.ChangeVersion"
    freshness_message = "Grid intersection project changed after preview/selection; rerun the command."
    refresh_marker = "GridIntersectionMarkerService.Refresh"
    selected_at = refresh_body.find(selected_marker)
    bind_at = refresh_body.find(bind_marker)
    freshness_at = refresh_body.find(freshness_marker)
    message_at = refresh_body.find(freshness_message)
    native_refresh_at = refresh_body.find(refresh_marker)
    if selected_at < 0 or bind_at < 0 or selected_at > bind_at:
        missing.append("commands:selected preview/cancel must complete before canonical mutation binding")
    elif freshness_at < 0 or bind_at > freshness_at:
        missing.append("commands:project freshness check must follow canonical mutation binding")
    elif message_at < 0 or freshness_at > message_at:
        missing.append("commands:project freshness failure must remain fail-closed")
    elif native_refresh_at < 0 or message_at > native_refresh_at:
        missing.append("commands:project freshness check must precede native marker refresh")

if missing:
    print("Grid intersection marker lifecycle guard FAILED:")
    for item in missing:
        print(" - " + item)
    sys.exit(1)
print("Grid intersection marker lifecycle guard PASS")
