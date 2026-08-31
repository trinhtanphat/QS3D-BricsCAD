#!/usr/bin/env python3
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

xaml = ROOT / "src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.xaml"
code = ROOT / "src/QS3D.BricsCAD.V25/UI/Rebar3DHubWindow.xaml.cs"
command = ROOT / "src/QS3D.BricsCAD.V25/Rebar3DHubCommands.cs"
for path in (xaml, code, command):
    if not path.is_file(): errors.append("missing Rebar 3D Hub file: " + str(path.relative_to(ROOT)))

if xaml.is_file():
    try: ET.parse(xaml)
    except ET.ParseError as exc: errors.append("Rebar3DHubWindow.xaml is not well formed: " + str(exc))
    text = xaml.read_text(encoding="utf-8")
    for name in (
        "QS3DREBAR3D", "QS3DREBARTIES3D", "QS3DBEAMREBAR3D", "QS3DREBARSTIRRUP3D",
        "QS3DSLABREBAR3D", "QS3DWALLREBAR3D", "QS3DREBAR3DSHAPE", "QS3DBBSVIEW",
        "QS3DHEALTHALL", "QS3DREBARHEALTHALL", "QS3DREBARTIEHEALTH", "QS3DREBARSTIRRUPHEALTH",
        "QS3DREBARSHAPEHEALTH",
    ):
        if 'Tag="' + name + '"' not in text: errors.append("Rebar 3D Hub missing button: " + name)

if code.is_file():
    text = code.read_text(encoding="utf-8")
    for needle in ("OnCommandClick", "SendStringToExecute", "button.Tag", "MdiActiveDocument"):
        if needle not in text: errors.append("Rebar 3D Hub dispatcher missing: " + needle)

if command.is_file():
    text = command.read_text(encoding="utf-8")
    for needle in (
        'CommandMethod("QS3DREBARHUB"',
        "new Rebar3DHubWindow()",
        "private static Rebar3DHubWindow? _pending;",
        "private static Rebar3DHubWindow? _published;",
        'CloseOwnerBeforeReplacement(pending, "pending");',
        "_pending = window;",
        "ShowModelessWindow",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, window))",
        "_pending = null;",
        "_published = window;",
        "if (ReferenceEquals(_pending, window)) _pending = null;",
        "if (ReferenceEquals(_published, window)) _published = null;",
        "ex.GetType().Name",
    ):
        if needle not in text: errors.append("QS3DREBARHUB publication contract missing: " + needle)
    for forbidden in (
        "private static Rebar3DHubWindow? _window;",
        "TryCloseUnpublishedWindow",
        "+ ex.Message",
    ):
        if forbidden in text: errors.append("QS3DREBARHUB retains unsafe legacy publication/error pattern: " + forbidden)

owners = {}
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    for name in re.findall(r'\[CommandMethod\("(QS3D[A-Z0-9_]*)"', text, re.IGNORECASE):
        owners.setdefault(name.upper(), []).append(str(path.relative_to(ROOT)))
if xaml.is_file():
    for name in re.findall(r'\bTag="(QS3D[A-Z0-9_]*)"', xaml.read_text(encoding="utf-8"), re.IGNORECASE):
        found = owners.get(name.upper(), [])
        if len(found) != 1: errors.append("Rebar Hub command " + name.upper() + " must have exactly one owner; found: " + ", ".join(found))

print("QS3D Rebar 3D Hub preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: Rebar 3D Hub XAML/dispatcher workflows are wired and launcher publication is pending-first, duplicate-safe, and host-error redacted.")
