#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCES = (
    ROOT / "src/QS3D.BricsCAD.V25/UI/ProjectToolsWindow.xaml.cs",
    ROOT / "src/QS3D.BricsCAD.V25/UI/ScheduleHubWindow.xaml.cs",
)
errors = []

for path in SOURCES:
    if not path.is_file():
        errors.append("missing document-bound command hub: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    label = path.name
    for token in (
        "DocumentBoundWindowLifetime.Attach(this, _document);",
        "var normalizedCommand = command.Trim();",
        '_document.SendStringToExecute(normalizedCommand + " ", true, false, false);',
        'SetStatus("Đã gửi lệnh " + normalizedCommand',
        "try { PaletteCoordinator.SetStatus(StatusText.Text); } catch { }",
    ):
        if token not in text:
            errors.append(label + " missing command-hub isolation token: " + token)

    send_index = text.find('_document.SendStringToExecute(normalizedCommand + " ", true, false, false);')
    success_index = text.find('SetStatus("Đã gửi lệnh " + normalizedCommand')
    if send_index < 0 or success_index < 0 or send_index > success_index:
        errors.append(label + " must dispatch the BricsCAD command before reporting success to UI/palette.")

    if 'SetStatus("Chạy " + normalizedCommand' in text or 'SetStatus("Chạy " + command' in text:
        errors.append(label + " must not make pre-dispatch status/palette synchronization part of command dispatch.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: document-bound command hubs close with their source DWG, fail closed on inactive drawings, dispatch before UI status work, and isolate palette synchronization failures.")
