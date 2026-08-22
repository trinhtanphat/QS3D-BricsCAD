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
        'private const int ZoneMaxIdLength = 64;',
        'private const int ZoneMaxNameLength = 120;',
        'private const int FloorMaxIdLength = 64;',
        'private const int FloorMaxNameLength = 120;',
        'private const int FamilyMaxIdLength = 80;',
        'private const int FamilyMaxNameLength = 160;',
        'private const int ElementMaxIdLength = 128;',
        'suffix == 1 ? "-import" : "-import-" + suffix',
        'suffix == 1 ? " (Imported)" : " (Imported " + suffix + ")"',
        "StringComparer.OrdinalIgnoreCase",
        'AddTypedRewrite(rewrites, element.Id, "FamilyId"',
        'AddTypedRewrite(rewrites, element.Id, "FloorId"',
        'AddTypedRewrite(rewrites, element.Id, "ZoneId"',
        'AddTypedRewrite(rewrites, element.Id, "DependsOn"',
        "ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference(property.Key, out var reference)",
        '"Property" + reference.Kind + "Id"',
        "ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(property.Key)",
        "Property looks like a semantic identity/reference but no explicit rewrite policy is registered for this key",
        "Family property looks like a semantic identity/reference but no explicit Family-property rewrite policy is registered for this key",
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
        "ProjectInterchangeRemapAppendImporter.Plan(project, json)",
        "var plan = appendPlan.Remap;",
        "ProjectInterchangeJsonValidator.MaxFileBytes",
        "new UTF8Encoding(false, true)",
        '" • append-as-new=" + (appendPlan.CanImport ? "READY" : "BLOCKED")',
        "runtime compatibility blockers",
        "Chưa mutate project; chưa import",
        "EnsureActive(document",
    ]
    for needle in required_command:
        if needle not in c:
            errors.append("remap dry-run command missing UX/validation contract: " + needle)

    if "ProjectInterchangeAppendOnlyImporter.Import" in c or "ProjectInterchangeRemapAppendImporter.Import" in c:
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
print("Import-as-new remap planning is deterministic and plan-only; shared semantic-reference policy and runtime compatibility blockers fail closed before execution.")
