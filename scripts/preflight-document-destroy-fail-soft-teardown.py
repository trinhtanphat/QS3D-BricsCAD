#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs"
errors = []

text = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
if not text:
    errors.append("missing DocumentLifecycleCoordinator.cs")
else:
    start = text.find("private static void OnDocumentToBeDestroyed")
    end = text.find("private static void OnDocumentDestroyed", start + 1)
    body = text[start:end] if start >= 0 and end > start else ""
    if not body:
        errors.append("missing OnDocumentToBeDestroyed lifecycle callback")
    else:
        ordered = (
            "CancelPendingReconcile(document);",
            "FailedProjectReconciliations.Remove(document);",
            "DetachProjectPersistence(document);",
            "SourceReconcileUndoCoordinator.Detach(document);",
            "CurtainWallUndoCoordinator.Detach(document);",
            "SelectionSyncCoordinator.Detach(document);",
            "ProjectContextCoordinator.Forget(document);",
        )
        cursor = 0
        for token in ordered:
            pos = body.find(token, cursor)
            if pos < 0:
                errors.append("document-destroy teardown missing ordered action: " + token)
                break
            cursor = pos + len(token)

        # Every cleanup action must be independently attempted. A single broad try/catch is
        # insufficient because an early throw would still suppress later native detach calls.
        for token in ordered:
            pos = body.find(token)
            if pos < 0:
                continue
            prefix = body[max(0, pos - 120):pos]
            suffix = body[pos:pos + len(token) + 80]
            if "try" not in prefix or "catch" not in suffix:
                errors.append("teardown action is not independently fail-soft: " + token)

        if "ReportDocumentDestroyTeardownErrors(document" not in body:
            errors.append("document-destroy teardown must report bounded cleanup diagnostics after all attempts")

print("QS3D document destroy fail-soft teardown preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: document destruction preserves ordered synchronous cleanup and independently attempts every teardown action before bounded diagnostics.")
