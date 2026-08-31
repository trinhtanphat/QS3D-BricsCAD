#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml",
    "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml.cs",
    "src/QS3D.BricsCAD.V25/GeometryExtensionsCommands.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing geometry extension UI file: " + relative)

xaml = ROOT / required[0]
if xaml.is_file():
    text = xaml.read_text(encoding="utf-8")
    for tag in (
        'Tag="QS3DWALLJUNCTIONS"',
        'Tag="QS3DWALLSNAPPREVIEW"',
        'Tag="QS3DWALLSNAPAPPLY"',
        'Tag="QS3DAUTOLINKHOSTS"',
        'Tag="QS3DCUTOPENINGS"',
        'Tag="QS3DCUTOPENINGSCURVED"',
        'Tag="QS3DREBAR3D"',
        'Tag="QS3DREBARTIES3D"',
        'Tag="QS3DREBAR3DSHAPE"',
        'Tag="QS3DREBARHEALTHALL"',
        'Click="OnCommandClick"',
    ):
        if tag not in text:
            errors.append("GeometryExtensionsWindow missing tag/handler: " + tag)

code = ROOT / required[1]
if code.is_file():
    text = code.read_text(encoding="utf-8")
    for needle in (
        "OnCommandClick",
        "SendStringToExecute",
        "StatusText.Text",
        "Application.DocumentManager.MdiActiveDocument",
    ):
        if needle not in text:
            errors.append("GeometryExtensionsWindow code-behind missing: " + needle)

command = ROOT / required[2]
if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DGEOMETRYEXT"',
        "private static GeometryExtensionsWindow? _published;",
        "var previous = _published;",
        "if (previous.IsLoaded)",
        "previous.Activate();",
        "window = new GeometryExtensionsWindow()",
        "window.Closed += (_, __) =>",
        "if (ReferenceEquals(_published, published)) _published = null;",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "_published = published;",
    ):
        if needle not in text:
            errors.append("Geometry Extensions command missing lifecycle contract: " + needle)

    show = text.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
    loaded = text.find("if (!window.IsLoaded)", show)
    publish = text.find("_published = published;", loaded)
    if min(show, loaded, publish) < 0 or not (show < loaded < publish):
        errors.append("Geometry Extensions must show, confirm Loaded, then publish its host-global singleton")

adapter = ROOT / "src/QS3D.BricsCAD.V25"
commands = []
if adapter.is_dir():
    for path in adapter.rglob("*.cs"):
        commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', path.read_text(encoding="utf-8"))
for required_command in (
    "QS3DGEOMETRYEXT", "QS3DCUTOPENINGSCURVED", "QS3DREBARTIES3D", "QS3DREBARHEALTHALL",
    "QS3DWALLSNAPPREVIEW", "QS3DWALLSNAPAPPLY"):
    if commands.count(required_command) != 1:
        errors.append(required_command + " must be declared exactly once")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Geometry Extensions remains active-document-dispatched while its host-global launcher is single-instance and Loaded-before-published.")
