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
        "ProjectContextCoordinator.TryGetReadOnly(_document, out var project)",
        "DrawingLabel(_document)",
    ):
        if token not in text:
            errors.append("AuditLogWindow.xaml.cs missing source-document read-only refresh token: " + token)
    if "private readonly ProjectState _project" in text:
        errors.append("Audit Log must not retain a stale ProjectState reference across modeless project reload/replacement")
    if "ProjectContextCoordinator.GetOrCreate(_document)" in text:
        errors.append("Audit Log is read-only and must not create/cache project state while refreshing")

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    if "new AuditLogWindow(document)" not in text:
        errors.append("QS3DAUDIT must pass the source drawing into the modeless Audit Log")
    if "new AuditLogWindow(project)" in text:
        errors.append("QS3DAUDIT must not construct Audit Log from a captured ProjectState")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append("QS3DAUDIT must inspect existing audit state through the read-only project lookup")
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append("QS3DAUDIT is read-only and must not create/cache project state just to open Audit Log")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] modeless Audit Log is bound to its source DWG and re-resolves existing audit state read-only on activation")
