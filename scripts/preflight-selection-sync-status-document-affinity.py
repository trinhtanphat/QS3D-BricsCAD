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
    attachment_guard = catch_body.find("IsCurrentAttachment(document, attachmentToken)")
    status_publish = catch_body.find("SelectionSyncStatusPublisher.SetStatusForDocument")
    if attachment_guard < 0 or status_publish < 0 or attachment_guard > status_publish:
        errors.append("selection-sync exception path must retain exact attachment-generation authority before status publication")

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

# Detach followed by reattach of the same Document wrapper is an ABA boundary. An in-flight
# Refresh from the old attachment must not regain authority merely because Attached.Contains
# becomes true again. Capture a per-attachment identity token and require exact token identity
# after native/modeless work and again before exception status publication.
for needle in [
    "private static readonly Dictionary<Document, object> AttachmentTokens",
    "AttachmentTokens[document] = new object();",
    "AttachmentTokens.Remove(document);",
    "AttachmentTokens.TryGetValue(document, out var attachmentToken)",
    "IsCurrentAttachment(document, attachmentToken)",
]:
    if needle not in selection:
        errors.append("selection-sync attachment-generation ABA fence missing token: " + needle)

helper_start = selection.find("private static bool IsCurrentAttachment")
helper_body = selection[helper_start:] if helper_start >= 0 else ""
for needle in [
    "AttachmentTokens.TryGetValue(document, out var currentToken)",
    "ReferenceEquals(currentToken, attachmentToken)",
]:
    if needle not in helper_body:
        errors.append("attachment-generation helper must require exact current token identity: " + needle)

# A DispatcherTimer Tick can already be queued when Detach removes/stops its timer. The
# callback must prove that its timer is still the canonical pending timer for the exact
# attached Document before it can re-enter Refresh. This closes queued-timer detach/reattach
# ABA independently of the in-flight Refresh attachment-token fence above.
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
if refresh_body and "AttachmentTokens.TryGetValue(document, out var attachmentToken)" not in refresh_body:
    errors.append("Refresh must capture exact attachment-generation identity before native/modeless work")
if refresh_body:
    snapshot = refresh_body.find("EntitySnapshotReader.ReadImpliedSelection(document)")
    revalidate = refresh_body.find("IsCurrentAttachment(document, attachmentToken)", snapshot if snapshot >= 0 else 0)
    publish = refresh_body.find("PaletteCoordinator.SetInspection(snapshots)")
    if snapshot < 0 or revalidate < 0 or publish < 0 or not (snapshot < revalidate < publish):
        errors.append("Refresh must revalidate exact attachment generation after native snapshot work before inspection publication")

print("QS3D selection-sync status document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: selection-sync status retains exact document and attachment-generation affinity; stale timers and detach/reattach ABA fail closed before palette work.")
