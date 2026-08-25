#!/usr/bin/env python3
from pathlib import Path
import re
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
        "using BcadApplication = Bricscad.ApplicationServices.Application;",
        "private readonly IntPtr _nativeDatabaseIdentity;",
        "public AuditLogWindow(Document document)",
        "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "DocumentBoundWindowLifetime.Attach(this, document);",
        "Activated += (_, __) => Reload();",
        "TryResolveBoundDocument(out var document)",
        "foreach (Document candidate in BcadApplication.DocumentManager)",
        "if (candidate == null || candidate.IsDisposed) continue;",
        "if (database.UnmanagedObject != _nativeDatabaseIdentity) continue;",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "DrawingLabel(document)",
    ):
        if token not in text:
            errors.append("AuditLogWindow.xaml.cs missing live source-document read-only refresh token: " + token)

    retained_document = re.search(
        r"\b(?:private|protected|public|internal)\s+(?:readonly\s+)?Document\s+_[A-Za-z0-9_]+\s*;",
        text,
    )
    if retained_document:
        errors.append("Audit Log must not retain a BricsCAD Document wrapper across modeless lifetime: " + retained_document.group(0))
    if "private readonly ProjectState _project" in text:
        errors.append("Audit Log must not retain a stale ProjectState reference across modeless project reload/replacement")
    if "ProjectContextCoordinator.GetOrCreate" in text:
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

print("[PASS] modeless Audit Log binds to its source native database identity, resolves a live Document wrapper for each read-only refresh, and does not retain stale host/project wrappers")
