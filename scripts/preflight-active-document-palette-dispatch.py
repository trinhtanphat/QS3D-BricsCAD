#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RIGHT = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
errors = []

if not RIGHT.is_file():
    errors.append("missing RightPanel.xaml.cs")
else:
    text = RIGHT.read_text(encoding="utf-8")
    for token in (
        "private bool TrySend(Document document, string command)",
        "document.SendStringToExecute(normalized + \" \", true, false, false);",
        'if (TrySend(doc, "_MOVE"))',
        "catch (Exception ex)",
    ):
        if token not in text:
            errors.append("RightPanel missing active-document dispatch token: " + token)
    if "private static void Send(string command)" in text:
        errors.append("RightPanel must not re-resolve a second active document through the old static Send helper.")

if not WORKSPACE.is_file():
    errors.append("missing WorkspacePanel.xaml.cs")
else:
    text = WORKSPACE.read_text(encoding="utf-8")
    if "DocumentBoundWindowLifetime.Attach" in text:
        errors.append("WorkspacePanel is palette-scoped and active-document dynamic; it must not bind to one source DWG.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: active-document palette dispatch keeps the captured RightPanel document for composed CAD operations and does not turn WorkspacePanel into a source-DWG-bound surface.")
