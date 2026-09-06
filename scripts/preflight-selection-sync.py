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
        "TimeSpan.FromMilliseconds(80d)",
        "if (!PaletteCoordinator.IsWorkspaceVisible) return;",
        "ScheduleRefresh(document)",
        "timer.Stop();",
        "timer.Start();",
        "RemovePending(document)",
        "Pending.Remove(document)",
        "Refreshing.Add(document)",
        "Refreshing.Remove(document)",
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
    event_start = text.find("private static void OnImpliedSelectionChanged")
    event_end = text.find("private static void ScheduleRefresh", event_start)
    if event_start >= 0 and event_end > event_start:
        event_body = text[event_start:event_end]
        if "EntitySnapshotReader.ReadImpliedSelection" in event_body or "\n            Refresh(document);" in event_body:
            errors.append("ImpliedSelectionChanged must schedule/coalesce work instead of synchronously reading snapshots.")

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
print("PASS: implied-selection reads are side-effect free, inspector refreshes are visible-only and debounced, and document selection attachment is deferred to the ApplicationIdle lifecycle reconcile boundary behind the execution-time active-document UI fence.")