#!/usr/bin/env python3
from pathlib import Path
import re
import sys

root = Path(__file__).resolve().parents[1]
ui_path = root / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml"
command_path = root / "src/QS3D.BricsCAD.V25/ProjectInterchangeRemapAppendCommands.cs"
planner_path = root / "src/QS3D.Core/Export/ProjectInterchangeRemapPlanner.cs"
importer_path = root / "src/QS3D.Core/Export/ProjectInterchangeRemapAppendImporter.cs"

errors = []
for path in (ui_path, command_path, planner_path, importer_path):
    if not path.exists():
        errors.append(f"missing Import As New source: {path.relative_to(root)}")

if not errors:
    ui = ui_path.read_text(encoding="utf-8")
    command = command_path.read_text(encoding="utf-8")
    planner = planner_path.read_text(encoding="utf-8")
    importer = importer_path.read_text(encoding="utf-8")

    for needle in [
        'Tag="QS3DINTERCHANGEREMAPPLAN"',
        'Tag="QS3DINTERCHANGEREMAPAPPEND"',
        'Content="Dry-run Import As New remap"',
        'Content="Nạp Snapshot (Import As New)"',
        "Import As New",
    ]:
        if needle not in ui:
            errors.append("Project Tools missing Import As New UI contract: " + needle)
    if "semantic-only" not in ui.lower():
        errors.append("Import As New Project Tools tooltip must explicitly state semantic-only boundary")
    if "strip source handles/native ownership" not in ui.lower():
        errors.append("Import As New Project Tools tooltip must state source ownership stripping")

    registrations = len(re.findall(r'\[CommandMethod\("QS3DINTERCHANGEREMAPAPPEND"', command))
    if registrations != 1:
        errors.append(f"QS3DINTERCHANGEREMAPAPPEND command registration count must be 1, got {registrations}")

    for needle in [
        "private const int ZoneMaxIdLength = 64;",
        "private const int ZoneMaxNameLength = 120;",
        "private const int FloorMaxIdLength = 64;",
        "private const int FloorMaxNameLength = 120;",
        "private const int FamilyMaxIdLength = 80;",
        "private const int FamilyMaxNameLength = 160;",
        "private const int ElementMaxIdLength = 128;",
    ]:
        if needle not in planner:
            errors.append("Import As New planner missing runtime-bound identity limit: " + needle)

    for needle in [
        "source.Families.Sum(x => x.Properties.Count(p => IsImportedOwnershipMetadata(p.Key)))",
        "ownershipDiscarded != plan.OwnershipPropertiesToDiscard",
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot(k)",
        "foreach (var family in source.Families)",
        "ProjectInterchangeSemanticReferencePolicy.LooksLikeSemanticReferenceKey(property.Key)",
        "ProjectInterchangeSemanticReferencePolicy.TryGetPropertyReference(property.Key, out var reference)",
        "EnsureFamilyPropertyRuntimeCompatible",
        "DrawingFingerprint = string.Empty",
    ]:
        if needle not in importer:
            errors.append("Import As New executor missing Family/reference/native-ownership safety contract: " + needle)

if errors:
    print("preflight-interchange-remap-project-tools: FAIL")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("preflight-interchange-remap-project-tools: PASS")
print("Project Tools exposes dry-run/apply Import As New; shared reference policy, runtime bounds, Family fail-closed checks and native-ownership stripping remain source-guarded.")
