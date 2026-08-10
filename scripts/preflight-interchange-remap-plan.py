#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
planner = root / "src/QS3D.Core/Export/ProjectInterchangeRemapPlanner.cs"
command = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapCommands.cs"
project_tools = root / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml"

errors = []
for path in (planner, command, project_tools):
    if not path.exists():
        errors.append(f"missing remap contract source: {path.relative_to(root)}")

if not errors:
    p = planner.read_text(encoding="utf-8")
    c = command.read_text(encoding="utf-8")
    ui = project_tools.read_text(encoding="utf-8")

    required_planner = [
        "ProjectInterchangeValidatedSnapshotReader.Read(json)",
        'private const int MaxIdLength = 128;',
        'private const int MaxNameLength = 512;',
        'private const string HostWallIdKey = "HostWallId";',
        'suffix == 1 ? "-import" : "-import-" + suffix',
        'suffix == 1 ? " (Imported)" : " (Imported " + suffix + ")"',
        "StringComparer.OrdinalIgnoreCase",
        'AddTypedRewrite(rewrites, element.Id, "FamilyId"',
        'AddTypedRewrite(rewrites, element.Id, "FloorId"',
        'AddTypedRewrite(rewrites, element.Id, "ZoneId"',
        'AddTypedRewrite(rewrites, element.Id, "DependsOn"',
        'AddTypedRewrite(rewrites, element.Id, "PropertyElementId", property.Key, hostId, elementMap)',
        "HostWallId is drawing/project-local but does not resolve to an Element inside the source snapshot",
        "Property looks like an Element identity/reference but no explicit rewrite policy is registered for this key",
        "public bool CanAppendAsNew => OpaqueReferenceWarnings.Count == 0;",
    ]
    for needle in required_planner:
        if needle not in p:
            errors.append("remap planner missing deterministic/fail-closed contract: " + needle)

    forbidden_planner = [
        "target.Elements.Add(",
        "target.Zones.Add(",
        "target.Floors.Add(",
        "target.Families.Add(",
        "ProjectStateSnapshot.Capture",
        "GeneratedDependentGeometryInvalidator",
    ]
    for needle in forbidden_planner:
        if needle in p:
            errors.append("dry-run planner must not mutate project/native state: " + needle)

    required_command = [
        '[CommandMethod("QS3DINTERCHANGEREMAPPLAN", CommandFlags.Modal)]',
        "ProjectInterchangeRemapPlanner.Plan(project, json)",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        '" • append-as-new=" + (plan.CanAppendAsNew ? "READY" : "BLOCKED")',
        "Chưa mutate project; chưa import",
        "EnsureActive(document",
    ]
    for needle in required_command:
        if needle not in c:
            errors.append("remap dry-run command missing UX/validation contract: " + needle)

    if "ProjectInterchangeAppendOnlyImporter.Import" in c or "ProjectInterchangeRemapAppend" in c:
        errors.append("QS3DINTERCHANGEREMAPPLAN must remain plan-only and never import")

    all_cs = "\n".join(path.read_text(encoding="utf-8", errors="ignore") for path in (root / "src").rglob("*.cs"))
    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEREMAPPLAN"', all_cs))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEREMAPPLAN registration count must be 1, got {registrations}")

    if ui.count('Tag="QS3DINTERCHANGEREMAPPLAN"') != 1:
        errors.append("Project Tools must expose QS3DINTERCHANGEREMAPPLAN exactly once")
    for needle in ["Dry-run Import As New remap", "không mutate", "HostWallId"]:
        if needle not in ui:
            errors.append("Project Tools missing remap dry-run boundary: " + needle)

if errors:
    print("preflight-interchange-remap-plan: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-remap-plan: PASS")
print("Import-as-new remap planning is deterministic and plan-only; typed references are explicit and opaque property-carried Element IDs block execution instead of being guessed.")
