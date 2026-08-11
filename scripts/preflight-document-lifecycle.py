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

print("PASS: ownership cleanup is keyed by exact Document identity in DocumentToBeDestroyed; DocumentDestroyed remains a separate post-destroy UI/active-DWG rebind stage.")
