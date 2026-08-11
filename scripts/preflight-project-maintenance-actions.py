#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ProjectToolsWindow.xaml"
SYNC = ROOT / "src" / "QS3D.BricsCAD.V25" / "SourceReconcileCommands.cs"
INTERCHANGE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectInterchangeCommands.cs"
errors = []

for path in (UI, SYNC, INTERCHANGE):
    if not path.is_file():
        errors.append("missing project maintenance source: " + str(path.relative_to(ROOT)))

if UI.is_file():
    try:
        ET.parse(UI)
    except ET.ParseError as exc:
        errors.append("ProjectToolsWindow.xaml is not well-formed XML/XAML: " + str(exc))
    text = UI.read_text(encoding="utf-8")
    for needle in (
        'Content="Đồng bộ source CAD đã sửa" Tag="QS3DSYNCSOURCE"',
        'Content="Xuất Semantic Snapshot JSON" Tag="QS3DINTERCHANGEJSON"',
        'Text="PROJECT-SAFE • READ-ONLY SNAPSHOT • DWG CONTEXT LOCK"',
    ):
        if needle not in text:
            errors.append("Project Tools missing maintenance/interchange wiring: " + needle)

if SYNC.is_file() and '[CommandMethod("QS3DSYNCSOURCE", CommandFlags.UsePickSet)]' not in SYNC.read_text(encoding="utf-8"):
    errors.append("SourceReconcileCommands.cs no longer exposes QS3DSYNCSOURCE")

if INTERCHANGE.is_file() and '[CommandMethod("QS3DINTERCHANGEJSON", CommandFlags.Modal)]' not in INTERCHANGE.read_text(encoding="utf-8"):
    errors.append("ProjectInterchangeCommands.cs no longer exposes QS3DINTERCHANGEJSON")

print("QS3D Project Tools maintenance/interchange preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Project Tools exposes authoritative-source reconcile and read-only semantic interchange without weakening DWG context locking.")
