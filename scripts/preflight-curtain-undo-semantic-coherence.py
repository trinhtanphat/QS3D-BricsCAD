#!/usr/bin/env python3
from pathlib import Path

HERE = Path(__file__).resolve()
BASE = HERE.parent / "_guard_bases" / "curtain-undo-semantic-coherence.py"
source = BASE.read_text(encoding="utf-8")

old = '''for token in (
    "CurtainWallUndoCoordinator.Attach(docs.MdiActiveDocument);",
    "CurtainWallUndoCoordinator.Stop();",
    "CurtainWallUndoCoordinator.Attach(e.Document);",
    "CurtainWallUndoCoordinator.Detach(document);",
    "CurtainWallUndoCoordinator.Attach(active);",
):
    if token not in lifecycle:
        errors.append("document lifecycle missing Curtain Undo affinity hook: " + token)

if lifecycle.count("CurtainWallUndoCoordinator.Attach(e.Document);") < 2:
    errors.append("Curtain Undo must attach on both DocumentCreated and DocumentActivated")
'''
new = '''for token in (
    "AttachCriticalServices(docs.MdiActiveDocument);",
    "AttachCriticalServices(e.Document);",
    "AttachCriticalServices(active);",
    "CurtainWallUndoCoordinator.Attach(document);",
    "CurtainWallUndoCoordinator.Stop();",
    "CurtainWallUndoCoordinator.Detach(document);",
    "ScheduleReconcile(e.Document, false);",
    "ScheduleReconcile(e.Document, true);",
):
    if token not in lifecycle:
        errors.append("document lifecycle missing staged Curtain Undo affinity hook: " + token)

if lifecycle.count("AttachCriticalServices(e.Document);") < 2:
    errors.append("Curtain Undo critical attachment must cover both DocumentCreated and DocumentActivated")
'''
if old not in source:
    raise SystemExit("Curtain Undo base guard lifecycle block drifted")
source = source.replace(old, new, 1)

for obsolete in (
    '    "_persistence.Restore(project);",\n',
    '    "target.Restore(project);",\n',
    '    "restoreRollback.Restore(project);",\n',
):
    if obsolete not in source:
        raise SystemExit("Curtain Undo base guard restore token drifted: " + obsolete.strip())
    source = source.replace(obsolete, "", 1)

old_sync = 'if "ReadRevision(document)" not in sync_body or "target.Restore(project);" not in sync_body or "if (refreshAfterRestore) RefreshAfterRestore(document);" not in sync_body:'
new_sync = 'if "ReadRevision(document)" not in sync_body or "target.Restore(project, transitionGuard);" not in sync_body or "if (refreshAfterRestore) RefreshAfterRestore(document);" not in sync_body:'
if old_sync not in source:
    raise SystemExit("Curtain Undo base guard sync-body contract drifted")
source = source.replace(old_sync, new_sync, 1)

extra = r'''
for token in (
    "public TransitionRestoreGuard PrepareTransitionRestore(ProjectState project)",
    "public void RestoreTransition(ProjectState project, TransitionRestoreGuard guard)",
):
    if token not in checkpoint:
        errors.append("Core persistence transition contract missing: " + token)

for token in (
    "public ProjectPersistenceCheckpoint.TransitionRestoreGuard PrepareTransitionRestore(ProjectState project)",
    "return _persistence.PrepareTransitionRestore(project);",
    "public void Restore(ProjectState project, ProjectPersistenceCheckpoint.TransitionRestoreGuard transitionGuard)",
    "_persistence.RestoreTransition(project, transitionGuard);",
    "var transitionGuard = currentExpected.PrepareTransitionRestore(project);",
    "target.Restore(project, transitionGuard);",
    "restoreRollback.Restore(project, transitionGuard);",
):
    if token not in coord:
        errors.append("Curtain Undo transition restore contract missing: " + token)
'''
if "\nif errors:\n" not in source:
    raise SystemExit("Curtain Undo base guard error boundary drifted")
source = source.replace("\nif errors:\n", "\n" + extra + "\nif errors:\n", 1)

namespace = {"__file__": str(HERE), "__name__": "__main__"}
exec(compile(source, str(HERE), "exec"), namespace)
