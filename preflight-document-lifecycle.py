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
        "SelectionSyncCoordinator.Detach(document);",
        "ProjectContextCoordinator.Forget(document);",
    )
    for token in required:
        if token not in text:
            errors.append("document lifecycle missing exact cleanup contract: " + token)
    if "DocumentDestroyed +=" in text or "DetachByName(e.FileName)" in text or "ForgetByName(e.FileName)" in text:
        errors.append("document destruction cleanup must not depend on filename identity")

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

print("PASS: document lifecycle cleanup is keyed by exact Document identity, including unsaved project keys and selection subscriptions.")
