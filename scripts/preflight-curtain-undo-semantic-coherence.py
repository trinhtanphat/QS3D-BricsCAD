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
namespace = {"__file__": str(HERE), "__name__": "__main__"}
exec(compile(source, str(HERE), "exec"), namespace)
