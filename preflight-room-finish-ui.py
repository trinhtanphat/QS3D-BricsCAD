#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
required = [
    "src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/RoomFinishScheduleWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/RoomFinishScheduleWindowCommands.cs",
]
for relative in required:
    if not (ROOT / relative).is_file(): errors.append("missing room-finish UI file: " + relative)
checks = {
    required[0]: [
        'x:Class="QS3D.BricsCAD.V25.UI.RoomFinishScheduleWindow"', 'x:Name="SearchBox"', 'x:Name="ScheduleGrid"',
        'x:Name="GroupCountText"', 'x:Name="LengthText"', 'x:Name="AreaText"', 'Click="OnRefreshClick"', 'Click="OnExportClick"',
        'Header="Loại hoàn thiện"', 'Header="Room IDs"',
    ],
    required[1]: [
        "private readonly Document _document", "RoomFinishScheduleWindow(Document document)",
<<<<<<< HEAD
        "ProjectStateSnapshot.CreateDetachedCopy(project)", "RegenerateDirty(snapshot)", "RoomFinishScheduleBuilder.Build(snapshot)",
=======
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)", "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)", "RoomFinishScheduleBuilder.Build(snapshot)",
>>>>>>> origin/main
        "RoomFinishXlsxExporter.Export", "RegenerationEngine", "SearchText.Contains(query)", "DrawingLabel(_document)",
        "EnsureActive", "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, _document)",
        'EnsureActive("đọc HT_Phòng Schedule hiện hành")', 'EnsureActive("xuất HT_Phòng XLSX")',
    ],
    required[2]: ['CommandMethod("QS3DFINISHSCHEDULE"', "new RoomFinishScheduleWindow(document)", "ShowModelessWindow"],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file(): continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text: errors.append(relative + " missing room-finish UI guard/token: " + needle)
<<<<<<< HEAD
window_source = ROOT / required[1]
if window_source.is_file():
    text = window_source.read_text(encoding="utf-8")
    if "RegenerateDirty(project)" in text or "RoomFinishScheduleBuilder.Build(project)" in text:
        errors.append(required[1] + " must not mutate or build from the live project")
=======

window = ROOT / required[1]
if window.is_file():
    text = window.read_text(encoding="utf-8")
    for forbidden in (
        "ProjectContextCoordinator.GetOrCreate(_document)",
        "ExistingProjectMutationContext",
        "RegenerateDirty(project)",
        "RoomFinishScheduleBuilder.Build(project)",
    ):
        if forbidden in text:
            errors.append("Room Finish modeless UI must remain read-only against live project state: " + forbidden)
>>>>>>> origin/main

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for command in ("QS3DFINISHSCHEDULE", "QS3DFINISHXLSX"):
    if commands.count(command) != 1: errors.append(command + " must be declared exactly once")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: document-bound HT_Phòng schedule UI rejects cross-DWG refresh/export and uses detached read-only freshness before filtering/XLSX workflows.")
