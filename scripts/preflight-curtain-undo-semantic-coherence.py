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

old_begin_token = '    "CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore)",\n'
new_begin_token = '    "CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore, undoAdmission)",\n'
if old_begin_token not in source:
    raise SystemExit("Curtain Undo base guard BeginTransition token drifted")
source = source.replace(old_begin_token, new_begin_token, 1)

old_begin_find = 'begin = build.find("CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore)")'
new_begin_find = 'begin = build.find("CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore, undoAdmission)")'
if old_begin_find not in source:
    raise SystemExit("Curtain Undo base guard BeginTransition finder drifted")
source = source.replace(old_begin_find, new_begin_find, 1)

old_regen_find = 'regen = build.find("RegenerateDirty(project)")'
new_regen_find = old_regen_find + '\nadmission = build.find("var undoAdmission = CurtainWallUndoCoordinator.OwnerStateSnapshot.Capture(project, undoBefore.OwnerIds);")'
if old_regen_find not in source:
    raise SystemExit("Curtain Undo base guard regeneration finder drifted")
source = source.replace(old_regen_find, new_regen_find, 1)

old_min = 'if min(capture, begin, regen, line_host, line_frame, line_panel, stage, commit, post, refresh) < 0:'
new_min = 'if min(capture, regen, admission, begin, line_host, line_frame, line_panel, stage, commit, post, refresh) < 0:'
if old_min not in source:
    raise SystemExit("Curtain Undo base guard ordering minimum drifted")
source = source.replace(old_min, new_min, 1)

old_order = 'elif not (capture < begin < regen < line_host < line_frame < line_panel < stage < commit < post < refresh):'
new_order = 'elif not (capture < regen < admission < begin < line_host < line_frame < line_panel < stage < commit < post < refresh):'
if old_order not in source:
    raise SystemExit("Curtain Undo base guard ordering contract drifted")
source = source.replace(old_order, new_order, 1)
old_order_message = 'Curtain Undo must capture/register before mutation, stage after all builders, commit marker with CAD, then finalize the exact post-fingerprint state'
new_order_message = 'Curtain Undo must capture exact command-entry owner state before regeneration, capture current admission after regeneration, then register before native mutation and preserve symmetric full-state Undo/Redo'
if old_order_message not in source:
    raise SystemExit("Curtain Undo base guard ordering message drifted")
source = source.replace(old_order_message, new_order_message, 1)

extra = r'''
for token in (
    "public TransitionRestoreGuard PrepareTransitionRestore(ProjectState project)",
    "public void RestoreTransition(ProjectState project, TransitionRestoreGuard guard)",
):
    if token not in checkpoint:
        errors.append("Core persistence transition contract missing: " + token)

for token in (
    "ProjectElementStateSnapshot.Capture(project, element.Id)",
    "FullState.Restore(project)",
    "FullState.Matches(project)",
    "HasSameOwnerGenerationSet",
    "OwnerStateSnapshot admission)",
    "before.HasSameOwnerGenerationSet(admission)",
    "if (!admission.Matches(project))",
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
