#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SELECTION = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs"
PUBLISHER = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncStatusPublisher.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")

selection = read(SELECTION)
publisher = read(PUBLISHER)

required_selection = [
    "SelectionSyncStatusPublisher.SetStatusForDocument(document, \"Selection sync lỗi. Vui lòng thử lại.\")",
]
for needle in required_selection:
    if needle not in selection:
        errors.append("selection-sync catch must publish status through exact source Document: " + needle)

required_publisher = [
    "internal static class SelectionSyncStatusPublisher",
    "public static void SetStatusForDocument(Document? sourceDocument, string status)",
    "ReferenceEquals(sourceDocument, Application.DocumentManager.MdiActiveDocument)",
    "PaletteCoordinator.SetStatus(status);",
]
for needle in required_publisher:
    if needle not in publisher:
        errors.append("status publication affinity fence missing token: " + needle)

catch_start = selection.find("catch (Exception)")
finally_start = selection.find("finally", catch_start if catch_start >= 0 else 0)
catch_body = selection[catch_start:finally_start if finally_start >= 0 else len(selection)] if catch_start >= 0 else ""
if catch_body and "PaletteCoordinator.SetStatus(" in catch_body:
    errors.append("selection-sync exception path must not publish through unbound process-wide SetStatus")

method_start = publisher.find("public static void SetStatusForDocument")
method_body = publisher[method_start:] if method_start >= 0 else ""
if method_body:
    null_guard = method_body.find("sourceDocument == null")
    affinity_guard = method_body.find("ReferenceEquals(sourceDocument, Application.DocumentManager.MdiActiveDocument)")
    publish = method_body.find("PaletteCoordinator.SetStatus(status);")
    if null_guard < 0 or affinity_guard < 0 or publish < 0 or null_guard > publish or affinity_guard > publish:
        errors.append("null/source-document identity guards must execute before status publication")
    for forbidden in [
        "ProjectContextCoordinator",
        "GetOrCreate",
        "DocumentLock",
        "StartTransaction",
        "SetImpliedSelection",
        "SendStringToExecute",
        "Dispatcher.BeginInvoke",
    ]:
        if forbidden in method_body:
            errors.append("status affinity fence must remain synchronous presentation-only: " + forbidden)

print("QS3D selection-sync status document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: selection-sync errors retain exact source-document affinity and fail closed before stale cross-DWG status publication.")
