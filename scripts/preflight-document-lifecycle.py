#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

lifecycle = ROOT / "src/QS3D.BricsCAD.V25/DocumentLifecycleCoordinator.cs"
selection = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs"
project = ROOT / "src/QS3D.BricsCAD.V25/ProjectContextCoordinator.cs"
entry = ROOT / "src/QS3D.BricsCAD.V25/PluginEntry.cs"

if not lifecycle.is_file():
    errors.append("missing DocumentLifecycleCoordinator.cs")
else:
    text = lifecycle.read_text(encoding="utf-8")
    for token in (
        "docs.DocumentCreated += OnDocumentCreated;",
        "docs.DocumentCreated -= OnDocumentCreated;",
        "docs.DocumentActivated += OnDocumentActivated;",
        "docs.DocumentActivated -= OnDocumentActivated;",
        "docs.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "docs.DocumentDestroyed += OnDocumentDestroyed;",
        "docs.DocumentDestroyed -= OnDocumentDestroyed;",
        "DispatcherPriority.ApplicationIdle",
        "ScheduleReconcile(docs.MdiActiveDocument, false);",
        "SelectionSyncCoordinator.Attach(document);",
    ):
        if token not in text:
            errors.append("document lifecycle missing event/staged reconcile contract: " + token)

    start = text.find("public static void Start()")
    stop = text.find("public static void Stop()", start + 1)
    start_body = text[start:stop] if start >= 0 and stop > start else ""
    subscribe = start_body.find("docs.DocumentCreated += OnDocumentCreated;")
    started = start_body.find("_started = true;", subscribe)
    schedule = start_body.find("ScheduleReconcile(docs.MdiActiveDocument, false);", started)
    rollback = start_body.find("catch", schedule)
    if min(subscribe, started, schedule, rollback) < 0 or not subscribe < started < schedule < rollback:
        errors.append("Start must subscribe critical handlers, claim started ownership, then defer project/selection/UI reconciliation before rollback handling")

    critical_helper = text.find("private static void AttachCriticalServices")
    if critical_helper >= 0:
        schedule_helper = text.find("private static void ScheduleReconcile", critical_helper)
        body = text[critical_helper:schedule_helper] if schedule_helper > critical_helper else ""
        for token in (
            "AttachProjectPersistence(document);",
            "SourceReconcileUndoCoordinator.Attach(document);",
            "CurtainWallUndoCoordinator.Attach(document);",
        ):
            if token not in body:
                errors.append("critical lifecycle attachment helper missing: " + token)
        if "AttachCriticalServices(docs.MdiActiveDocument);" not in start_body:
            errors.append("Start must attach critical persistence/Undo services before claiming started ownership")
    else:
        for token in (
            "AttachProjectPersistence(docs.MdiActiveDocument);",
            "SourceReconcileUndoCoordinator.Attach(docs.MdiActiveDocument);",
            "CurtainWallUndoCoordinator.Attach(docs.MdiActiveDocument);",
        ):
            if token not in start_body:
                errors.append("Start missing direct critical lifecycle attachment: " + token)

    for token in (
        "try { docs.DocumentCreated -= OnDocumentCreated; } catch { }",
        "try { docs.DocumentActivated -= OnDocumentActivated; } catch { }",
        "try { docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; } catch { }",
        "try { docs.DocumentDestroyed -= OnDocumentDestroyed; } catch { }",
        "StopPendingLifecycleWork();",
        "foreach (var document in SaveCompleteHandlers.Keys.ToArray()) DetachProjectPersistence(document);",
        "SourceReconcileUndoCoordinator.Stop();",
        "CurtainWallUndoCoordinator.Stop();",
        "SelectionSyncCoordinator.Stop();",
        "_started = false;",
        "throw;",
    ):
        if token not in start_body:
            errors.append("DocumentLifecycleCoordinator.Start missing rollback contract: " + token)

    destroy_start = text.find("private static void OnDocumentToBeDestroyed")
    destroyed_start = text.find("private static void OnDocumentDestroyed", destroy_start + 1)
    cleanup = text[destroy_start:destroyed_start] if destroy_start >= 0 and destroyed_start > destroy_start else ""
    for token in (
        "var document = e.Document;",
        "CancelPendingReconcile(document);",
        "DetachProjectPersistence(document);",
        "SourceReconcileUndoCoordinator.Detach(document);",
        "CurtainWallUndoCoordinator.Detach(document);",
        "SelectionSyncCoordinator.Detach(document);",
        "ProjectContextCoordinator.Forget(document);",
    ):
        if token not in cleanup:
            errors.append("DocumentToBeDestroyed missing exact Document cleanup contract: " + token)
    for forbidden in ("e.FileName", "DetachByName", "ForgetByName"):
        if forbidden in cleanup:
            errors.append("document destruction cleanup must not depend on filename identity: " + forbidden)

    reconcile_start = text.find("private static void ReconcileDocument")
    persistence_start = text.find("private static void AttachProjectPersistence", reconcile_start)
    reconcile = text[reconcile_start:persistence_start] if reconcile_start >= 0 and persistence_start > reconcile_start else ""
    active = reconcile.find("var refreshActiveUi = refreshUi && IsActiveDocument(document);")
    attach = reconcile.find("SelectionSyncCoordinator.Attach(document);", active)
    ensure = reconcile.find("EnsureProject(document, refreshActiveUi);", attach)
    refresh = reconcile.find("if (refreshActiveUi) SelectionSyncCoordinator.Refresh(document);", ensure)
    if min(active, attach, ensure, refresh) < 0 or not active < attach < ensure < refresh:
        errors.append("deferred reconcile must fence active UI at execution time, attach selection, then load/refresh project UI")

if not selection.is_file():
    errors.append("missing SelectionSyncCoordinator.cs")
else:
    text = selection.read_text(encoding="utf-8")
    for token in ("public static void Detach(Document? document)", "Attached.Remove(document);"):
        if token not in text:
            errors.append("selection sync missing exact Document detach contract: " + token)

if not project.is_file():
    errors.append("missing ProjectContextCoordinator.cs")
else:
    text = project.read_text(encoding="utf-8")
    for token in ("public static void Forget(Document document)", "Projects.Remove(document);", "UnsavedProjectKeys.Remove(document);"):
        if token not in text:
            errors.append("project context missing exact Document cleanup contract: " + token)

if not entry.is_file():
    errors.append("missing PluginEntry.cs")
else:
    text = entry.read_text(encoding="utf-8")
    if "DocumentLifecycleCoordinator.Start();" not in text or "TryCleanup(DocumentLifecycleCoordinator.Stop);" not in text:
        errors.append("PluginEntry must start document lifecycle coordination and include its stop operation in contained host teardown")

print("QS3D document lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: lifecycle startup owns critical native hooks before NETLOAD returns, defers project/selection/UI work to ApplicationIdle, fences modeless UI to the execution-time active document, rolls back partial startup, and tears down exact-document ownership synchronously through contained host teardown.")