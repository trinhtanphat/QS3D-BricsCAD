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
    required = (
        "docs.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed;",
        "docs.DocumentDestroyed += OnDocumentDestroyed;",
        "docs.DocumentDestroyed -= OnDocumentDestroyed;",
    )
    for token in required:
        if token not in text:
            errors.append("document lifecycle missing event contract: " + token)

    start = text.find("public static void Start()")
    stop = text.find("public static void Stop()", start + 1)
    start_body = text[start:stop] if start >= 0 and stop > start else ""
    start_tokens = (
        "try",
        "docs.DocumentCreated += OnDocumentCreated;",
        "docs.DocumentActivated += OnDocumentActivated;",
        "docs.DocumentToBeDestroyed += OnDocumentToBeDestroyed;",
        "docs.DocumentDestroyed += OnDocumentDestroyed;",
        "AttachProjectPersistence(docs.MdiActiveDocument);",
        "SelectionSyncCoordinator.Attach(docs.MdiActiveDocument);",
        "_started = true;",
        "catch",
        "try { docs.DocumentCreated -= OnDocumentCreated; } catch { }",
        "try { docs.DocumentActivated -= OnDocumentActivated; } catch { }",
        "try { docs.DocumentToBeDestroyed -= OnDocumentToBeDestroyed; } catch { }",
        "try { docs.DocumentDestroyed -= OnDocumentDestroyed; } catch { }",
        "foreach (var document in SaveCompleteHandlers.Keys.ToArray()) DetachProjectPersistence(document);",
        "SelectionSyncCoordinator.Stop();",
        "_started = false;",
        "throw;",
    )
    positions = []
    cursor = 0
    for token in start_tokens:
        position = start_body.find(token, cursor)
        if position < 0:
            errors.append("DocumentLifecycleCoordinator.Start missing atomic startup/rollback contract: " + token)
            positions = []
            break
        positions.append(position)
        cursor = position + len(token)
    if positions and positions != sorted(positions):
        errors.append("DocumentLifecycleCoordinator.Start startup/rollback ordering is not monotonic")

    if "docs.DocumentCreated += OnDocumentCreated;\n            docs.DocumentActivated += OnDocumentActivated;" in start_body and "try\n            {" not in start_body:
        errors.append("document manager subscriptions must be inside the Start rollback boundary")

    destroy_start = text.find("private static void OnDocumentToBeDestroyed")
    destroyed_start = text.find("private static void OnDocumentDestroyed", destroy_start + 1)
    if destroy_start < 0 or destroyed_start <= destroy_start:
        errors.append("document lifecycle cannot isolate exact-Document cleanup stage")
    else:
        cleanup = text[destroy_start:destroyed_start]
        for token in (
            "var document = e.Document;",
            "DetachProjectPersistence(document);",
            "SelectionSyncCoordinator.Detach(document);",
            "ProjectContextCoordinator.Forget(document);",
        ):
            if token not in cleanup:
                errors.append("DocumentToBeDestroyed missing exact Document cleanup contract: " + token)
        for forbidden in ("e.FileName", "DetachByName", "ForgetByName"):
            if forbidden in cleanup:
                errors.append("document destruction cleanup must not depend on filename identity: " + forbidden)

    # DocumentDestroyed is intentionally retained only for no-document UI reset / active-DWG rebind.
    # Exact project/selection ownership cleanup must already have happened in DocumentToBeDestroyed.
    destroyed_end = text.find("private static void AttachProjectPersistence", destroyed_start + 1)
    destroyed = text[destroyed_start:destroyed_end] if destroyed_start >= 0 and destroyed_end > destroyed_start else ""
    for forbidden in ("ProjectContextCoordinator.Forget(", "SelectionSyncCoordinator.Detach(", "e.FileName", "DetachByName", "ForgetByName"):
        if forbidden in destroyed:
            errors.append("DocumentDestroyed must be post-destroy UI/rebind only, not ownership cleanup: " + forbidden)

if not selection.is_file():
    errors.append("missing SelectionSyncCoordinator.cs")
else:
    text = selection.read_text(encoding="utf-8")
    for token in ("public static void Detach(Document? document)", "Attached.Remove(document);"):
        if token not in text:
            errors.append("selection sync missing exact Document detach contract: " + token)

    attach = text.find("public static void Attach(Document? document)")
    detach = text.find("public static void Detach(Document? document)", attach + 1)
    attach_body = text[attach:detach] if attach >= 0 and detach > attach else ""
    attach_tokens = (
        "if (document == null || Attached.Contains(document)) return;",
        "var subscribed = false;",
        "try",
        "document.ImpliedSelectionChanged += OnImpliedSelectionChanged;",
        "subscribed = true;",
        "if (!Attached.Add(document))",
        "document.ImpliedSelectionChanged -= OnImpliedSelectionChanged;",
        "Refresh(document);",
        "catch",
        "if (subscribed)",
        "try { document.ImpliedSelectionChanged -= OnImpliedSelectionChanged; }",
        "RemovePending(document);",
        "Refreshing.Remove(document);",
        "Attached.Remove(document);",
        "throw;",
    )
    cursor = 0
    for token in attach_tokens:
        position = attach_body.find(token, cursor)
        if position < 0:
            errors.append("SelectionSyncCoordinator.Attach missing retryable attachment contract: " + token)
            break
        cursor = position + len(token)

    if "if (document == null || !Attached.Add(document)) return;" in text:
        errors.append("selection sync must not claim Attached ownership before native event subscription succeeds")

    subscribe = attach_body.find("document.ImpliedSelectionChanged += OnImpliedSelectionChanged;")
    claim = attach_body.find("Attached.Add(document)", subscribe + 1)
    if subscribe < 0 or claim < 0 or subscribe >= claim:
        errors.append("selection sync must subscribe first and claim Attached ownership only afterward")

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
    if "DocumentLifecycleCoordinator.Start();" not in text or "DocumentLifecycleCoordinator.Stop();" not in text:
        errors.append("PluginEntry must start and stop document lifecycle coordination")

print("QS3D document lifecycle preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: document lifecycle startup rolls back partial manager/persistence/selection subscriptions, selection attachment remains retryable after native subscription failure, exact-Document destruction cleanup remains authoritative, and DocumentDestroyed stays post-destroy UI/rebind only.")
