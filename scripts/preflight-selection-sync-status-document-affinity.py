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

for needle in [
    "private static readonly Dictionary<Document, object> AttachmentTokens",
    "private static readonly Dictionary<Document, EventHandler> AttachmentHandlers",
    "AttachmentTokens[document] = attachmentToken;",
    "AttachmentHandlers[document] = attachmentHandler;",
    "AttachmentTokens.Remove(document);",
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

# A selection event can already be queued when Detach unsubscribes. A generation-specific handler
# must retain its original token, reject stale ownership, and pass that same token into scheduling.
event_start = selection.find("private static void OnImpliedSelectionChanged(Document document, object attachmentToken)")
event_end = selection.find("private static void ScheduleRefresh", event_start if event_start >= 0 else 0)
event_body = selection[event_start:event_end if event_end >= 0 else len(selection)] if event_start >= 0 else ""
if not event_body:
    errors.append("selection-sync event handler must be generation-specific")
else:
    attached = event_body.find("IsCurrentAttachment(document, attachmentToken)")
    active = event_body.find("ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)")
    schedule = event_body.find("ScheduleRefresh(document, attachmentToken);")
    if min(attached, active, schedule) < 0 or not (attached < active < schedule):
        errors.append("queued selection event must retain exact generation + active-document authority before scheduling refresh")

schedule_start = selection.find("private static void ScheduleRefresh(Document document, object attachmentToken)")
schedule_end = selection.find("private static bool IsCurrentAttachment", schedule_start if schedule_start >= 0 else 0)
schedule_body = selection[schedule_start:schedule_end if schedule_end >= 0 else len(selection)] if schedule_start >= 0 else ""
if not schedule_body:
    errors.append("ScheduleRefresh must retain the exact attachment generation")
else:
    detached_guard = schedule_body.find("IsCurrentAttachment(document, attachmentToken)")
    first_pending_lookup = schedule_body.find("Pending.TryGetValue(document")
    if detached_guard < 0 or first_pending_lookup < 0 or detached_guard > first_pending_lookup:
        errors.append("ScheduleRefresh must fail closed on exact stale generation before creating/reusing Pending timer state")

# A DispatcherTimer Tick can already be queued when Detach removes/stops its timer. An older
# non-canonical timer must never remove a newer timer, and the canonical timer must revalidate the
# exact token captured by its generation before invoking refresh.
tick_start = selection.find("timer.Tick +=")
tick_end = selection.find("};", tick_start if tick_start >= 0 else 0)
tick_body = selection[tick_start:tick_end if tick_end >= 0 else len(selection)] if tick_start >= 0 else ""
for needle in [
    "Pending.TryGetValue(document, out var current)",
    "ReferenceEquals(current, timer)",
    "Pending.Remove(document);",
    "IsCurrentAttachment(document, attachmentToken)",
    "Refresh(document, attachmentToken);",
]:
    if needle not in tick_body:
        errors.append("selection-sync queued Tick generation/lifetime fence missing token: " + needle)
if tick_body:
    authority = tick_body.find("Pending.TryGetValue(document, out var current)")
    identity = tick_body.find("ReferenceEquals(current, timer)")
    remove = tick_body.find("Pending.Remove(document);")
    attached = tick_body.find("IsCurrentAttachment(document, attachmentToken)")
    refresh = tick_body.find("Refresh(document, attachmentToken);")
    if min(authority, identity, remove, attached, refresh) < 0 or not (authority < identity < remove < attached < refresh):
        errors.append("canonical Tick must validate timer identity, consume Pending, then verify exact generation before Refresh")

# External lifecycle/UI callers may request a refresh without owning a historical token. That
# boundary may capture the current token exactly once. Generation-bound queued work must call the
# two-argument overload directly and must never recapture a newer token.
compat_start = selection.find("public static void Refresh(Document? document)")
exact_start = selection.find("public static void Refresh(Document? document, object attachmentToken)")
stop_start = selection.find("public static void Stop()", exact_start if exact_start >= 0 else 0)
compat_body = selection[compat_start:exact_start] if compat_start >= 0 and exact_start > compat_start else ""
exact_body = selection[exact_start:stop_start if stop_start >= 0 else len(selection)] if exact_start >= 0 else ""
if not compat_body:
    errors.append("SelectionSync must retain the one-argument lifecycle refresh compatibility boundary")
else:
    capture = compat_body.find("AttachmentTokens.TryGetValue(document, out var attachmentToken)")
    delegate = compat_body.find("Refresh(document, attachmentToken);")
    if capture < 0 or delegate < 0 or capture > delegate:
        errors.append("one-argument Refresh must capture the current attachment token once before delegating")
    for forbidden in ["PaletteCoordinator.EnsureCreated", "EntitySnapshotReader.ReadImpliedSelection", "PaletteCoordinator.SetInspection"]:
        if forbidden in compat_body:
            errors.append("one-argument Refresh must delegate only and not perform modeless/native work: " + forbidden)

if not exact_body:
    errors.append("SelectionSync exact-token Refresh overload is missing")
else:
    if "AttachmentTokens.TryGetValue(document, out var attachmentToken)" in exact_body:
        errors.append("exact-token Refresh must never recapture a newer attachment generation")
    entry = exact_body.find("IsCurrentAttachment(document, attachmentToken)")
    claim = exact_body.find("Refreshing[document] = attachmentToken;")
    work = exact_body.find("PaletteCoordinator.EnsureCreated();")
    snapshot = exact_body.find("EntitySnapshotReader.ReadImpliedSelection(document)")
    revalidate = exact_body.find("IsCurrentAttachment(document, attachmentToken)", snapshot if snapshot >= 0 else 0)
    publish = exact_body.find("PaletteCoordinator.SetInspection(snapshots)")
    release = exact_body.find("ReleaseRefresh(document, attachmentToken);")
    if min(entry, claim, work, snapshot, revalidate, publish, release) < 0 or not (entry < claim < work < snapshot < revalidate < publish < release):
        errors.append("exact-token Refresh must validate/claim generation, revalidate after snapshot, publish, then release exact ownership")

print("QS3D selection-sync status document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: selection-sync status, lifecycle refresh compatibility, queued callbacks and exact attachment generations retain document affinity without stale-generation authority.")
