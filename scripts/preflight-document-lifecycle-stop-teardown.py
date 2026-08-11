#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "DocumentLifecycleCoordinator.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DocumentLifecycleCoordinator.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    stop_start = text.find("public static void Stop()")
    created_start = text.find("private static void OnDocumentCreated", stop_start + 1)
    stop = text[stop_start:created_start] if stop_start >= 0 and created_start > stop_start else ""

    required = (
        "if (!_started) return;",
        "var docs = Application.DocumentManager;",
        "try { docs.DocumentCreated -= OnDocumentCreated; } catch { }",
        "try { docs.DocumentActivated -= OnDocumentActivated; } catch { }",
        "try { docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; } catch { }",
        "try { docs.DocumentDestroyed -= OnDocumentDestroyed; } catch { }",
        "try",
        "foreach (var document in SaveCompleteHandlers.Keys.ToArray()) DetachProjectPersistence(document);",
        "catch",
        "try { SelectionSyncCoordinator.Stop(); }",
        "catch { }",
        "_started = false;",
    )
    cursor = 0
    for token in required:
        pos = stop.find(token, cursor)
        if pos < 0:
            errors.append("DocumentLifecycleCoordinator.Stop missing ordered best-effort teardown contract: " + token)
            break
        cursor = pos + len(token)

    for forbidden in (
        "docs.DocumentCreated -= OnDocumentCreated;\n            docs.DocumentActivated -= OnDocumentActivated;",
        "SelectionSyncCoordinator.Stop();\n            _started = false;",
    ):
        if forbidden in stop:
            errors.append("Stop contains unguarded cleanup sequence that can short-circuit teardown: " + forbidden.replace("\n", " | "))

    clear_pos = stop.rfind("_started = false;")
    selection_pos = stop.find("SelectionSyncCoordinator.Stop();")
    persistence_pos = stop.find("DetachProjectPersistence(document);")
    if clear_pos < 0 or selection_pos < 0 or persistence_pos < 0 or clear_pos <= max(selection_pos, persistence_pos):
        errors.append("Stop must clear _started only after persistence and selection teardown attempts")

    # Preserve startup rollback and exact-document lifecycle contracts.
    for token in (
        "public static void Start()",
        "try { docs.DocumentCreated -= OnDocumentCreated; } catch { }",
        "SelectionSyncCoordinator.Attach(docs.MdiActiveDocument);",
        "private static void OnDocumentToBeDestroyed",
        "SelectionSyncCoordinator.Detach(document);",
        "ProjectContextCoordinator.Forget(document);",
        "private static void OnDocumentDestroyed",
        "PaletteCoordinator.ResetForNoDocument();",
    ):
        if token not in text:
            errors.append("Stop hardening lost existing lifecycle contract: " + token)

print("QS3D document lifecycle stop teardown preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: document lifecycle Stop isolates manager unsubscriptions, persistence cleanup and selection teardown so one native cleanup failure cannot short-circuit the remaining teardown, then deterministically clears started ownership.")
