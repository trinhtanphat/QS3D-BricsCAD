#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FLOOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.xaml.cs"
ZONE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ZoneManagerWindow.xaml.cs"
V26 = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing assignment audit contract file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def method_body(text, start_token, end_token, label):
    start = text.find(start_token)
    if start < 0:
        errors.append("missing " + label + " handler")
        return ""
    end = text.find(end_token, start + len(start_token))
    if end < 0:
        errors.append("cannot bound " + label + " handler")
        return ""
    return text[start:end]


def require_assignment_audit(body, relation_name, service_call, old_name, audit_action, label):
    snapshot = body.find("var previous = elements.ToDictionary(")
    relation = body.find("element => element." + relation_name, snapshot)
    service = body.find(service_call)
    audit_guard = body.find("!string.Equals(" + old_name + ", element." + relation_name + ", StringComparison.Ordinal)")
    audit = body.find('AuditTrail.ForProject(project).Record("' + audit_action + '"')
    if min(snapshot, relation, service, audit_guard, audit) < 0:
        errors.append(label + " is missing relation snapshot/service/actual-change audit tokens")
        return
    if not (snapshot < relation < service < audit_guard < audit):
        errors.append(label + " must snapshot raw relation before Assign and gate audit on the post-service actual relation change")


floor = read(FLOOR)
zone = read(ZONE)
v26 = read(V26)

floor_body = method_body(floor, "private void OnAssignClick", "private void OnInspectSelectionClick", "Floor assign")
zone_body = method_body(zone, "private void OnAssignClick", "private void OnInspectClick", "Zone assign")

require_assignment_audit(
    floor_body,
    "FloorId",
    "ProjectFloorService.Assign(project, floor.Id, elements)",
    "oldFloor",
    "floor.assign",
    "Floor assign")
require_assignment_audit(
    zone_body,
    "ZoneId",
    "ProjectZoneService.Assign(project, zone.Id, elements)",
    "oldZone",
    "zone.assign",
    "Zone assign")

for forbidden, label in (
    (".Where(element => !string.Equals(element.FloorId, floor.Id, StringComparison.OrdinalIgnoreCase))", "Floor assign pre-service raw target filter"),
    (".Where(x => !string.Equals(x.ZoneId, zone.Id, StringComparison.OrdinalIgnoreCase))", "Zone assign pre-service raw target filter"),
):
    if forbidden in floor_body or forbidden in zone_body:
        errors.append(label + " must not decide assignment audit candidates")

for token in (
    '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"',
    '<RootNamespace>QS3D.BricsCAD.V25</RootNamespace>',
    '<DefineConstants>$(DefineConstants);BRICSCAD_V26</DefineConstants>',
):
    if token not in v26:
        errors.append("V26 shared V25-source contract missing token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Floor/Zone assignment audit follows actual before/after relation mutation and V26 links the corrected V25 UI source.")
