#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FAMILY_MANAGER = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FamilyManagerWindow.xaml.cs"
V26_PROJECT = ROOT / "src" / "QS3D.BricsCAD.V26" / "QS3D.BricsCAD.V26.csproj"

errors = []


def read(path):
    if not path.is_file():
        errors.append("missing Family property no-op audit contract file: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


def method_body(text, start_token, end_token):
    start = text.find(start_token)
    if start < 0:
        errors.append("missing Family Manager handler: " + start_token)
        return ""
    end = text.find(end_token, start + len(start_token))
    if end < 0:
        errors.append("cannot bound Family Manager handler after: " + start_token)
        return ""
    return text[start:end]


def require_mutation_only_audit(body, service_call, audit_action, label):
    before = body.find("var beforeVersion = project.ChangeVersion;")
    service = body.find(service_call)
    guard = body.find("if (project.ChangeVersion != beforeVersion)")
    audit = body.find('AuditTrail.ForProject(project).Record("' + audit_action + '"')
    if min(before, service, guard, audit) < 0:
        errors.append(label + " is missing pre-version/service/audit guard tokens")
        return
    if not (before < service < guard < audit):
        errors.append(label + " must capture ChangeVersion before the domain service and guard audit after the service")
    if body.count('AuditTrail.ForProject(project).Record("' + audit_action + '"') != 1:
        errors.append(label + " must contain exactly one guarded " + audit_action + " audit record")


family = read(FAMILY_MANAGER)
v26 = read(V26_PROJECT)

save_body = method_body(
    family,
    "private void OnSavePropertyClick",
    "private void OnRemovePropertyClick")
remove_body = method_body(
    family,
    "private void OnRemovePropertyClick",
    "private void OnAssignClick")

require_mutation_only_audit(
    save_body,
    "ProjectFamilyService.SetProperty(project, family.Id, key, value)",
    "family.property.set",
    "Family property Save")
require_mutation_only_audit(
    remove_body,
    "ProjectFamilyService.RemoveProperty(project, family.Id, key)",
    "family.property.remove",
    "Family property Remove")

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

print("PASS: Family property Save/Remove audit only real domain mutations, and V26 continues to link the corrected V25 UI source.")
