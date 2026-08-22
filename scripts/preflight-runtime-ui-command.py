#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25"
UI = ADAPTER / "UI"
RIBBON = ADAPTER / "Ribbon"
PROJECT_TOOLS = UI / "ProjectToolsWindow.xaml"
RUNTIME_CHECK = ADAPTER / "RuntimeDiagnosticsCommands.cs"
RUNTIME_PROBE = ADAPTER / "RuntimeProbeCommands.cs"

errors = []

for path in (PROJECT_TOOLS, RUNTIME_CHECK, RUNTIME_PROBE):
    if not path.is_file():
        errors.append("missing runtime diagnostics contract file: " + str(path.relative_to(ROOT)))

if PROJECT_TOOLS.is_file():
    try:
        ET.parse(PROJECT_TOOLS)
    except ET.ParseError as exc:
        errors.append("ProjectToolsWindow.xaml is not well-formed XAML/XML: " + str(exc))

    text = PROJECT_TOOLS.read_text(encoding="utf-8")
    if 'Tag="QS3DRUNTIMECHECK"' not in text:
        errors.append("ProjectToolsWindow.xaml must expose user-facing QS3DRUNTIMECHECK")

if RUNTIME_CHECK.is_file():
    text = RUNTIME_CHECK.read_text(encoding="utf-8")
    if '[CommandMethod("QS3DRUNTIMECHECK", CommandFlags.Modal)]' not in text:
        errors.append("RuntimeDiagnosticsCommands.cs must register modal QS3DRUNTIMECHECK")

if RUNTIME_PROBE.is_file():
    text = RUNTIME_PROBE.read_text(encoding="utf-8")
    if '[CommandMethod("QS3DRUNTIMEPROBE", CommandFlags.Modal)]' not in text:
        errors.append("RuntimeProbeCommands.cs must register modal QS3DRUNTIMEPROBE")
    if 'QS3D_RUNTIME_RESULT' not in text:
        errors.append("QS3DRUNTIMEPROBE must remain bound to QS3D_RUNTIME_RESULT automation output")

# QS3DRUNTIMEPROBE is an automation qualification command. It must never be wired into
# modeless user-facing XAML or Ribbon source; users should invoke QS3DRUNTIMECHECK instead.
for folder in (UI, RIBBON):
    if not folder.is_dir():
        continue
    for path in sorted(folder.rglob("*")):
        if not path.is_file() or path.suffix.lower() not in (".xaml", ".cs"):
            continue
        text = path.read_text(encoding="utf-8")
        if "QS3DRUNTIMEPROBE" in text:
            errors.append(
                str(path.relative_to(ROOT))
                + " exposes automation-only QS3DRUNTIMEPROBE in user-facing UI/Ribbon source"
            )

print("QS3D runtime UI command separation preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: Project Tools exposes modal QS3DRUNTIMECHECK while the result-file "
    "QS3DRUNTIMEPROBE remains modal automation-only."
)
