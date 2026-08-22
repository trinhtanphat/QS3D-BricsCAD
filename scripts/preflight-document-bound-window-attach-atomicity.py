#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "DocumentBoundWindowLifetime.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DocumentBoundWindowLifetime.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    attach_start = text.find("public void Attach(Document document)")
    bind_start = text.find("private void BindProjectAffinityIfPresent()", attach_start + 1)
    attach = text[attach_start:bind_start] if attach_start >= 0 and bind_start > attach_start else ""

    required_attach = (
        "if (!ReferenceEquals(document, _document))",
        "if (_attached) return;",
        "try",
        "BindProjectAffinityIfPresent();",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "_window.Activated += OnWindowActivated;",
        "_window.PreviewMouseDown += OnPreviewMouseDown;",
        "_window.PreviewKeyDown += OnPreviewKeyDown;",
        "_window.Closed += OnWindowClosed;",
        "_attached = true;",
        "catch",
        "_attached = true;",
        "Detach();",
        "_projectAffinityBound = false;",
        "_projectId = string.Empty;",
        "throw;",
    )
    cursor = 0
    for token in required_attach:
        pos = attach.find(token, cursor)
        if pos < 0:
            errors.append("modeless Attach missing ordered failure-rollback contract: " + token)
            break
        cursor = pos + len(token)

    if "catch\n                {\n                    throw;" in attach:
        errors.append("modeless Attach must clean partial event ownership before rethrow")

    detach_start = text.find("private void Detach()")
    detach = text[detach_start:] if detach_start >= 0 else ""
    for token in (
        "if (!_attached) return;",
        "BcadApplication.DocumentManager.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "_window.Activated -= OnWindowActivated;",
        "_window.PreviewMouseDown -= OnPreviewMouseDown;",
        "_window.PreviewKeyDown -= OnPreviewKeyDown;",
        "_window.Closed -= OnWindowClosed;",
        "_attached = false;",
    ):
        if token not in detach:
            errors.append("modeless Detach lost best-effort handler cleanup contract: " + token)

    # Preserve the existing safety/identity boundaries.
    for token in (
        "Registrations.GetValue(window, key => new Registration(key, document))",
        'throw new InvalidOperationException("A modeless QS3D window cannot be rebound to a different BricsCAD document.")',
        "ReferenceEquals(e.Document, _document)",
        "CloseForProjectChange();",
        "_window.Dispatcher.BeginInvoke(new Action(_window.Close))",
    ):
        if token not in text:
            errors.append("modeless lifetime atomicity change lost existing safety contract: " + token)

    if attach.count("_attached = true;") != 2:
        errors.append("Attach must mark successful ownership once and temporarily enable Detach exactly once in rollback")

print("QS3D document-bound window attach atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: document-bound modeless lifetime attachment rolls partial subscriptions back through the existing best-effort Detach path, clears failed project affinity, remains retryable, and preserves source-DWG/project fail-closed identity behavior.")
