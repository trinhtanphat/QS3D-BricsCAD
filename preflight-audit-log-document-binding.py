#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/AuditLogWindow.xaml.cs"
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/AuditCommands.cs"
errors = []

for path in (WINDOW, COMMAND):
    if not path.is_file():
        errors.append("missing audit-log document-binding contract file: " + str(path.relative_to(ROOT)))

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    for token in (
        "private readonly Document _document;",
        "public AuditLogWindow(Document document)",
        "Activated += (_, __) => Reload();",
        "ProjectContextCoordinator.GetOrCreate(_document)",
        "DrawingLabel(_document)",
    ):
        if token not in text:
            errors.append("AuditLogWindow.xaml.cs missing source-document refresh token: " + token)
    if "private readonly ProjectState _project" in text:
        errors.append("Audit Log must not retain a stale ProjectState reference across modeless project reload/replacement")

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    if "new AuditLogWindow(document)" not in text:
        errors.append("QS3DAUDIT must pass the source drawing into the modeless Audit Log")
    if "new AuditLogWindow(project)" in text:
        errors.append("QS3DAUDIT must not construct Audit Log from a captured ProjectState")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] modeless Audit Log is bound to its source DWG and re-resolves current project audit state on activation")
