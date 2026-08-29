#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.Core/Domain/ProjectZoneService.cs",
    "src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/ZoneManagerWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/ZoneManagerCommands.cs",
    "tests/QS3D.Core.SmokeTests/ProjectZoneServiceSmoke.cs",
    "tests/QS3D.Core.SmokeTests/ProjectZoneServiceRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing zone file: " + relative)

checks = {
    required[0]: [
        "MaxZones = 2000", "Create(ProjectState project", "Update(ProjectState project", "SetActive(ProjectState project",
        "Assign(ProjectState project", "Delete(ProjectState project", "ReferenceCount(ProjectState project", "EnsureUniqueName",
        "ElementDirtyFlags.Relations | ElementDirtyFlags.Quantity", "Cannot delete the active zone", "Reassign them before deletion",
    ],
    required[1]: [
        'x:Class="QS3D.BricsCAD.V25.UI.ZoneManagerWindow"', 'x:Name="ZoneList"', 'x:Name="ZoneNameBox"',
        'x:Name="ReferenceCountText"', 'Click="OnNewClick"', 'Click="OnSaveClick"', 'Click="OnDeleteClick"',
        'Click="OnActivateClick"', 'Click="OnAssignClick"', "không Move CAD source",
    ],
    required[2]: [
        "private readonly Document _document", "ZoneManagerWindow(Document document)", "ProjectZoneService.Create", "ProjectZoneService.Update",
        "ProjectZoneService.Delete", "ProjectZoneService.SetActive", "ProjectZoneService.Assign", "ProjectZoneService.ReferenceCount",
        "SemanticSelectionResolver.ResolveImplied(_document, project)", "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        'AuditTrail.ForProject(project).Record("zone.create"', 'AuditTrail.ForProject(project).Record("zone.assign"',
    ],
    required[3]: [
        'CommandMethod("QS3DZONES"', "candidate = new ZoneManagerWindow(document)", "ShowModelessWindow",
        "private static PublishedManager? _published", "private readonly WeakReference<Document> _document",
        "NativeDatabaseIdentity", "database.UnmanagedObject == NativeDatabaseIdentity",
        "previous.Matches(document) && previous.MatchesManagedWrapper(document)",
        "previous.Window.Activate()", "previous.Window.Close()", "publishedWindow.Closed", "_published = published",
    ],
    required[4]: [
        "CreateUpdateAssignAndDelete", "AssignmentMarksGeneratedGeometryStale", "DeleteGuardsActiveAndReferencedZones", "RejectsDuplicateNames",
        "IsGeneratedSolidStale()",
    ],
    required[5]: ["ProjectZoneServiceSmoke.Run();"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing zone guard/token: " + needle)

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DZONES") != 1: errors.append("QS3DZONES must be declared exactly once")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Core-backed Zone semantics and native-database-affine, wrapper-bound, veto-safe single-instance Zone Manager are present.")
