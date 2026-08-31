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
        'CommandMethod("QS3DZONES"', "private static PublishedManager? _pending", "private static PublishedManager? _published",
        "private readonly WeakReference<Document> _document", "NativeDatabaseIdentity", "database.UnmanagedObject == NativeDatabaseIdentity",
        "CloseOwnerBeforeReplacement(pending, \"pending\")", "previous.Matches(document)", "previous.MatchesManagedWrapper(document)",
        "CloseOwnerBeforeReplacement(previous, \"published\")", "var window = new ZoneManagerWindow(document)",
        "var owner = new PublishedManager(window, document)", "window.Closed", "_pending = owner",
        "ShowModelessWindow", "if (!window.IsLoaded)", "if (!ReferenceEquals(_pending, owner))", "_published = owner",
        "candidate != null && ReferenceEquals(_pending, candidate)", "candidate.Window.Close()", "ex.GetType().Name",
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

zone_commands = ROOT / required[3]
if zone_commands.is_file():
    source = zone_commands.read_text(encoding="utf-8")
    if "ex.Message" in source:
        errors.append("Zone Manager command must redact raw exception messages")
    try:
        pending_drain = source.index('CloseOwnerBeforeReplacement(pending, "pending")')
        construct = source.index("var window = new ZoneManagerWindow(document)")
        pending_own = source.index("_pending = owner", construct)
        host_show = source.index("ShowModelessWindow", pending_own)
        loaded_check = source.index("if (!window.IsLoaded)", host_show)
        owner_check = source.index("if (!ReferenceEquals(_pending, owner))", loaded_check)
        publish = source.index("_published = owner", owner_check)
        if not (pending_drain < construct < pending_own < host_show < loaded_check < owner_check < publish):
            errors.append("Zone Manager publication must drain pending before construction and publish only after loaded ownership proof")
    except ValueError as exc:
        errors.append("Zone Manager publication ordering marker missing: " + str(exc))

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
if commands.count("QS3DZONES") != 1: errors.append("QS3DZONES must be declared exactly once")

if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Core-backed Zone semantics and pending-first, native-database-affine, wrapper-bound, veto-safe Zone Manager publication are present.")
