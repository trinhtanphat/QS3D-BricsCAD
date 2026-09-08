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
if catch_body:
    attached_guard = catch_body.find("Attached.Contains(document)")
    status_publish = catch_body.find("SelectionSyncStatusPublisher.SetStatusForDocument")
    if attached_guard < 0 or status_publish < 0 or attached_guard > status_publish:
        errors.append("selection-sync exception path must verify source Document is still attached before status publication")

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

# A DispatcherTimer Tick can already be queued when Detach removes/stops its timer. The
# callback must prove that its timer is still the canonical pending timer for the exact
# attached Document before it can re-enter Refresh. This also closes detach -> reattach
# ABA, where a stale timer from the first attachment must not gain authority from the
# second attachment merely because the same Document wrapper is attached again.
tick_start = selection.find("timer.Tick +=")
tick_end = selection.find("};", tick_start if tick_start >= 0 else 0)
tick_body = selection[tick_start:tick_end if tick_end >= 0 else len(selection)] if tick_start >= 0 else ""
for needle in [
    "Pending.TryGetValue(document, out var current)",
    "ReferenceEquals(current, timer)",
    "Attached.Contains(document)",
]:
    if needle not in tick_body:
        errors.append("selection-sync queued Tick must fail closed after detach/ABA before Refresh: " + needle)
if tick_body:
    authority = tick_body.find("Pending.TryGetValue(document, out var current)")
    refresh = tick_body.find("Refresh(document);")
    if authority < 0 or refresh < 0 or authority > refresh:
        errors.append("queued Tick canonical-timer authority check must execute before Refresh")

refresh_start = selection.find("public static void Refresh(Document? document)")
refresh_end = selection.find("public static void Stop()", refresh_start if refresh_start >= 0 else 0)
refresh_body = selection[refresh_start:refresh_end if refresh_start >= 0 and refresh_end >= 0 else len(selection)] if refresh_start >= 0 else ""
if refresh_body and "!Attached.Contains(document)" not in refresh_body:
    errors.append("Refresh must fail closed for detached Documents even when a stale callback is already queued")

print("QS3D selection-sync status document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: selection-sync errors retain exact source-document affinity; detached status paths and stale timers fail closed before palette work.")
