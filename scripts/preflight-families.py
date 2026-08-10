#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Domain/ProjectFamilyService.cs",
    "src/QS3D.Core/Domain/ProjectFamilyActivationService.cs",
    "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/FamilyManagerWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs",
    "src/QS3D.BricsCAD.V25/TktVariantCommands.cs",
    "tests/QS3D.Core.SmokeTests/ProjectFamilyServiceSmoke.cs",
    "tests/QS3D.Core.SmokeTests/ProjectFamilyServiceRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing Family Manager file: " + relative)

checks = {
    required[0]: [
        "MaxFamilies = 10000", "Create(ProjectState project", "Duplicate(ProjectState project", "Rename(ProjectState project",
        "SetProperty(ProjectState project", "RemoveProperty(ProjectState project", "Assign(ProjectState project", "Delete(ProjectState project",
        "ReferenceCount(ProjectState project", "EnsureUniqueName", "InheritedInstancesUpdated", "OverridesPreserved",
        "element.SetProperty(normalizedKey, normalizedValue)", "element.MarkDirty(ElementDirtyFlags.All)",
        "cannot be assigned to element", "Reassign them before deletion", "Cannot delete the active Family",
        "Project contains duplicate semantic element id", "ReferenceEquals(owned, element)", "Element does not belong to the project instance",
    ],
    required[1]: [
        "ProjectFamilyActivationService", "GetActive(ProjectState project)", "SetActive(ProjectState project", "ClearIfMissing(ProjectState project)",
        'project.Metadata["ActiveFamilyId"] = family.Id', "Family not found",
    ],
    required[2]: [
        'x:Class="QS3D.BricsCAD.V25.UI.FamilyManagerWindow"', 'x:Name="CategoryFilter"', 'x:Name="FamilyList"', 'x:Name="FamilyNameBox"',
        'x:Name="PropertyList"', 'x:Name="PropertyKeyBox"', 'x:Name="PropertyValueBox"',
        'Click="OnNewClick"', 'Click="OnDuplicateClick"', 'Click="OnRenameClick"', 'Click="OnDeleteClick"',
        'Click="OnSavePropertyClick"', 'Click="OnRemovePropertyClick"', 'Click="OnAssignClick"',
        "override instance khác biệt được giữ nguyên",
    ],
    required[3]: [
        "private readonly Document _document", "FamilyManagerWindow(Document document)", "ProjectFamilyService.Create", "ProjectFamilyService.Duplicate",
        "ProjectFamilyService.Rename", "ProjectFamilyService.SetProperty", "ProjectFamilyService.RemoveProperty", "ProjectFamilyService.Assign",
        "ProjectFamilyService.Delete", "ProjectFamilyService.ReferenceCount", "SemanticSelectionResolver.ResolveImplied(_document, project)",
        "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        'AuditTrail.ForProject(project).Record("family.create"', 'AuditTrail.ForProject(project).Record("family.property.set"',
        'AuditTrail.ForProject(project).Record("family.assign"',
    ],
    required[4]: ['CommandMethod("QS3DFAMILIES"', "new FamilyManagerWindow(document)", "ShowModelessWindow"],
    required[5]: [
        "ProjectFamilyActivationService.GetActive(project)", "active.Category == category", "ProjectFamilyActivationService.SetActive(project, family.Id)",
        'EnsureDefault(family, "CurtainFrameDepthM", "0.05")', 'EnsureDefault(family, "WallPierProfileMode", "Rectangular")',
    ],
    required[6]: [
        "PropertyUpdatesPreserveOverrides", "FamilyAssignmentDropsOldInheritedDefaultsButKeepsOverrides", "FamilyAssignmentRejectsSpoofedSameIdElement",
        "DuplicateRenameDeleteGuards", "IsGeneratedSolidStale()", "Explicit instance override did not survive Family assignment",
        "Rejected spoofed Family assignment must not mutate",
    ],
    required[7]: ["ProjectFamilyServiceSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing Family Manager guard/token: " + needle)

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DFAMILIES") != 1: errors.append("QS3DFAMILIES must be declared exactly once")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: inheritance-safe Family CRUD/properties/assignment, exact project ownership, explicit active Family semantics, TKT active-Family capture and document-bound Family Manager are present.")
