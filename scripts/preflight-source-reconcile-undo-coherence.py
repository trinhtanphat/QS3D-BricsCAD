#!/usr/bin/env python3
from pathlib import Path

HERE = Path(__file__).resolve()
BASE = HERE.parent / "_guard_bases" / "source-reconcile-undo-coherence.py"
source = BASE.read_text(encoding="utf-8")

old = '''for token in (
    "SourceReconcileUndoCoordinator.Attach(docs.MdiActiveDocument)",
    "SourceReconcileUndoCoordinator.Attach(e.Document)",
    "SourceReconcileUndoCoordinator.Detach(document)",
    "SourceReconcileUndoCoordinator.Stop()",
):
    if token not in lifecycle:
        errors.append("Document lifecycle missing Undo coordination: " + token)
'''
new = '''for token in (
    "AttachCriticalServices(docs.MdiActiveDocument)",
    "AttachCriticalServices(e.Document)",
    "SourceReconcileUndoCoordinator.Attach(document)",
    "SourceReconcileUndoCoordinator.Detach(document)",
    "SourceReconcileUndoCoordinator.Stop()",
    "ScheduleReconcile(e.Document, false)",
    "ScheduleReconcile(e.Document, true)",
):
    if token not in lifecycle:
        errors.append("Document lifecycle missing staged Undo coordination: " + token)
if lifecycle.count("AttachCriticalServices(e.Document)") < 2:
    errors.append("Source Reconcile Undo critical attachment must cover both DocumentCreated and DocumentActivated")
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard lifecycle block drifted")
source = source.replace(old, new, 1)
namespace = {"__file__": str(HERE), "__name__": "__main__"}
exec(compile(source, str(HERE), "exec"), namespace)
