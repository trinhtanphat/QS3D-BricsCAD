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
        errors.append("missing active no-op audit contract file: " + str(path.relative_to(ROOT)))
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


def require_guard(body, service_call, audit_action, label):
    previous = body.find("var previous = project.Active")
    before = body.find("var beforeVersion = project.ChangeVersion;")
    service = body.find(service_call)
    guard = body.find("if (project.ChangeVersion != beforeVersion)")
    audit = body.find('AuditTrail.ForProject(project).Record("' + audit_action + '"')
    if min(previous, before, service, guard, audit) < 0:
        errors.append(label + " is missing previous/version/service/guard/audit tokens")
        return
    if not (previous < before < service < guard < audit):
        errors.append(label + " must preserve previous detail, capture ChangeVersion before SetActive, and guard audit after SetActive")
    raw_guard_floor = "if (!string.Equals(previous, floor.Id, StringComparison.OrdinalIgnoreCase))"
    raw_guard_zone = "if (!string.Equals(previous, zone.Id, StringComparison.OrdinalIgnoreCase))"
    if raw_guard_floor in body or raw_guard_zone in body:
        errors.append(label + " still decides mutation from a raw active-id comparison")


floor = read(FLOOR)
zone = read(ZONE)
v26 = read(V26)

floor_body = method_body(floor, "private void OnActivateClick", "private void OnAssignClick", "Floor activate")
zone_body = method_body(zone, "private void OnActivateClick", "private void OnAssignClick", "Zone activate")

require_guard(floor_body, "ProjectFloorService.SetActive(project, floor.Id)", "floor.activate", "Floor activate")
require_guard(zone_body, "ProjectZoneService.SetActive(project, zone.Id)", "zone.activate", "Zone activate")

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

print("PASS: Floor/Zone activate audit follows domain SetActive mutation and V26 links the corrected V25 UI source.")
