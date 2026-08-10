#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DomainHubWindow.xaml"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "RuntimeDiagnosticsCommands.cs"
SUPPORT = ROOT / "src" / "QS3D.BricsCAD.V25" / "SupportBundleCommands.cs"
errors = []

for path in (UI, RUNTIME, SUPPORT):
    if not path.is_file():
        errors.append("missing diagnostics wiring source: " + str(path.relative_to(ROOT)))

if UI.is_file():
    try:
        ET.parse(UI)
    except ET.ParseError as exc:
        errors.append("DomainHubWindow.xaml is not well-formed XML/XAML: " + str(exc))
    text = UI.read_text(encoding="utf-8")
    required = (
        'Text="KIỂM TRA / RELEASE"',
        'Content="Kiểm tra runtime V25" Tag="QS3DRUNTIMECHECK"',
        'Content="Xuất Support Bundle" Tag="QS3DSUPPORTBUNDLE"',
    )
    for needle in required:
        if needle not in text:
            errors.append("Domain Hub missing customer diagnostics wiring: " + needle)
    if 'Content="Kiểm tra runtime V25" Tag="QS3DRUNTIMEPROBE"' in text:
        errors.append("customer-facing runtime button must use QS3DRUNTIMECHECK, not automation-only QS3DRUNTIMEPROBE")

if RUNTIME.is_file() and '[CommandMethod("QS3DRUNTIMECHECK", CommandFlags.Modal)]' not in RUNTIME.read_text(encoding="utf-8"):
    errors.append("RuntimeDiagnosticsCommands.cs no longer exposes QS3DRUNTIMECHECK")

if SUPPORT.is_file() and '[CommandMethod("QS3DSUPPORTBUNDLE", CommandFlags.Modal)]' not in SUPPORT.read_text(encoding="utf-8"):
    errors.append("SupportBundleCommands.cs no longer exposes QS3DSUPPORTBUNDLE")

print("QS3D Domain Hub diagnostics wiring preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: the human-facing Domain Hub routes runtime diagnostics to QS3DRUNTIMECHECK and exposes the privacy-safe support bundle command without repurposing the automation probe.")
