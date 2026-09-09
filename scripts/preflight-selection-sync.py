#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "reader": ROOT / "src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs",
    "sync": ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs",
    "palette": ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs",
    "lifecycle": ROOT / "src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs",
}
for path in files.values():
    if not path.is_file():
        errors.append("missing selection-sync file: " + str(path.relative_to(ROOT)))

checks = {
    "reader": [
        "restoreInteractiveSelection = false",
        "restoreInteractiveSelection = true",
        "if (restoreInteractiveSelection) editor.SetImpliedSelection(objectIds);",
        "Never call SetImpliedSelection while merely reading an existing implied",
    ],
    "sync": [
        "Dictionary<Document, DispatcherTimer> Pending",
        "Dictionary<Document, object> Refreshing",
        "TimeSpan.FromMilliseconds(80d)",
        "if (!PaletteCoordinator.IsWorkspaceVisible) return;",
        "ScheduleRefresh(document, attachmentToken)",
        "timer.Stop();",
        "timer.Start();",
        "RemovePending(document)",
        "Pending.Remove(document)",
        "Refreshing[document] = attachmentToken",
        "ReleaseRefresh(document, attachmentToken)",
        "public static void Refresh(Document? document)",
        "public static void Refresh(Document? document, object attachmentToken)",
    ],
    "palette": [
        "SelectionSyncCoordinator.Refresh(Application.DocumentManager.MdiActiveDocument);",
    ],
    "lifecycle": [
        "ScheduleReconcile(e.Document, false)",
        "ScheduleReconcile(e.Document, true)",
        "SelectionSyncCoordinator.Attach(document)",
        "SelectionSyncCoordinator.Detach(document)",
        "SelectionSyncCoordinator.Stop()",
        "DispatcherPriority.ApplicationIdle",
    ],
}
for key, needles in checks.items():
    path = files[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing selection-sync token: " + needle)

if files["reader"].is_file():
    text = files["reader"].read_text(encoding="utf-8")
    set_calls = text.count("editor.SetImpliedSelection(objectIds)")
    if set_calls != 1:
        errors.append("EntitySnapshotReader must contain exactly one SetImpliedSelection(objectIds) call, guarded for interactive GetSelection only; found %d" % set_calls)

if files["sync"].is_file():
    text = files["sync"].read_text(encoding="utf-8")

    event_start = text.find("private static void OnImpliedSelectionChanged(Document document, object attachmentToken)")
    event_end = text.find("private static void ScheduleRefresh", event_start if event_start >= 0 else 0)
    if event_start < 0 or event_end <= event_start:
        errors.append("ImpliedSelectionChanged must retain the exact attachment-generation token.")
    else:
        event_body = text[event_start:event_end]
        if "EntitySnapshotReader.ReadImpliedSelection" in event_body or "Refresh(document" in event_body:
            errors.append("ImpliedSelectionChanged must schedule/coalesce generation-bound work instead of synchronously reading snapshots.")
        generation_guard = event_body.find("IsCurrentAttachment(document, attachmentToken)")
        active_guard = event_body.find("ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)")
        schedule = event_body.find("ScheduleRefresh(document, attachmentToken);")
        if min(generation_guard, active_guard, schedule) < 0 or not (generation_guard < active_guard < schedule):
            errors.append("ImpliedSelectionChanged must prove exact generation and active-document authority before scheduling.")

    schedule_start = text.find("private static void ScheduleRefresh(Document document, object attachmentToken)")
    schedule_end = text.find("private static bool IsCurrentAttachment", schedule_start if schedule_start >= 0 else 0)
    schedule_body = text[schedule_start:schedule_end] if schedule_start >= 0 and schedule_end > schedule_start else ""
    if not schedule_body:
        errors.append("ScheduleRefresh must retain the exact attachment-generation token.")
    else:
        generation_guard = schedule_body.find("IsCurrentAttachment(document, attachmentToken)")
        pending = schedule_body.find("Pending.TryGetValue(document")
        tick_refresh = schedule_body.find("Refresh(document, attachmentToken);")
        if min(generation_guard, pending, tick_refresh) < 0 or not (generation_guard < pending < tick_refresh):
            errors.append("debounced selection refresh must preserve/revalidate exact attachment generation through the queued Tick.")
        if "Refresh(document);" in schedule_body:
            errors.append("queued selection refresh must not discard generation ownership by calling the one-argument Refresh boundary.")

    compat_start = text.find("public static void Refresh(Document? document)")
    exact_start = text.find("public static void Refresh(Document? document, object attachmentToken)")
    stop_start = text.find("public static void Stop()", exact_start if exact_start >= 0 else 0)
    compat_body = text[compat_start:exact_start] if compat_start >= 0 and exact_start > compat_start else ""
    exact_body = text[exact_start:stop_start] if exact_start >= 0 and stop_start > exact_start else ""
    if not compat_body:
        errors.append("SelectionSync must retain one-argument Refresh for lifecycle/palette callers.")
    else:
        capture = compat_body.find("AttachmentTokens.TryGetValue(document, out var attachmentToken)")
        delegate = compat_body.find("Refresh(document, attachmentToken);")
        if capture < 0 or delegate < 0 or capture > delegate:
            errors.append("one-argument Refresh must capture current attachment generation once and delegate to exact-token Refresh.")
    if not exact_body:
        errors.append("SelectionSync exact-token Refresh overload is missing.")
    else:
        if "AttachmentTokens.TryGetValue(document, out var attachmentToken)" in exact_body:
            errors.append("generation-bound Refresh must not recapture a newer token.")
        claim = exact_body.find("Refreshing[document] = attachmentToken;")
        work = exact_body.find("PaletteCoordinator.EnsureCreated();")
        release = exact_body.find("ReleaseRefresh(document, attachmentToken);")
        if min(claim, work, release) < 0 or not (claim < work < release):
            errors.append("exact-token Refresh must claim exact generation before modeless/native work and release only that generation afterward.")

if files["lifecycle"].is_file():
    text = files["lifecycle"].read_text(encoding="utf-8")
    reconcile = text.find("private static void ReconcileDocument")
    active = text.find("var refreshActiveUi = refreshUi && IsActiveDocument(document);", reconcile)
    attach = text.find("SelectionSyncCoordinator.Attach(document);", active)
    ensure = text.find("EnsureProject(document, refreshActiveUi);", attach)
    refresh = text.find("if (refreshActiveUi) SelectionSyncCoordinator.Refresh(document);", ensure)
    if min(reconcile, active, attach, ensure, refresh) < 0 or not reconcile < active < attach < ensure < refresh:
        errors.append("selection attachment must remain inside deferred document reconciliation after the execution-time active-document fence and before project/UI refresh")

print("QS3D selection-sync preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: implied-selection reads stay side-effect free; lifecycle refresh compatibility is preserved; and debounce/refresh ownership remains exact-generation and active-document fenced.")
