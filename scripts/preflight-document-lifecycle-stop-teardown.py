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
    start = text.find("public static void Stop()")
    end = text.find("private static void OnDocumentCreated", start + 1)
    stop = text[start:end] if start >= 0 and end > start else ""
    required = (
        "if (!_started) return;",
        "_started = false;",
        "var docs = Application.DocumentManager;",
        "try { docs.DocumentCreated -= OnDocumentCreated; } catch { }",
        "try { docs.DocumentActivated -= OnDocumentActivated; } catch { }",
        "try { docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; } catch { }",
        "try { docs.DocumentDestroyed -= OnDocumentDestroyed; } catch { }",
        "StopPendingLifecycleWork();",
        "DetachProjectPersistence(document);",
        "try { SourceReconcileUndoCoordinator.Stop(); }",
        "try { CurtainWallUndoCoordinator.Stop(); }",
        "try { SelectionSyncCoordinator.Stop(); }",
    )
    cursor = 0
    for token in required:
        pos = stop.find(token, cursor)
        if pos < 0:
            errors.append("Stop missing ordered teardown contract: " + token)
            break
        cursor = pos + len(token)

    clear = stop.find("_started = false;")
    unsubscribe = stop.find("docs.DocumentCreated -= OnDocumentCreated;")
    pending = stop.find("StopPendingLifecycleWork();")
    if min(clear, unsubscribe, pending) < 0 or not clear < unsubscribe < pending:
        errors.append("Stop must clear started ownership before native teardown and then cancel queued lifecycle work")
    if stop.count("_started = false;") != 1:
        errors.append("Stop must clear started ownership exactly once")

    for token in (
        "ScheduleReconcile(docs.MdiActiveDocument, false);",
        "SelectionSyncCoordinator.Attach(document);",
        "CancelPendingReconcile(document);",
        "SourceReconcileUndoCoordinator.Detach(document);",
        "CurtainWallUndoCoordinator.Detach(document);",
        "SelectionSyncCoordinator.Detach(document);",
        "ProjectContextCoordinator.Forget(document);",
        "PaletteCoordinator.ResetForNoDocument();",
    ):
        if token not in text:
            errors.append("lifecycle contract missing: " + token)

print("QS3D document lifecycle stop teardown preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Stop clears lifecycle ownership before best-effort teardown, cancels queued idle work, and preserves document cleanup contracts.")