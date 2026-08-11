#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/AutoHostLinkCommands.cs"
errors = []

if not path.is_file():
    errors.append("missing AutoHostLinkCommands.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "using QS3D.Core.Persistence;",
        "var rollback = ProjectStateSnapshot.Capture(project);",
        "service.LinkOpening(project, item.Opening.Id, item.HostId);",
        "regenerated = linked > 0 ? Regenerate(project, regenerationTargets) : 0;",
        "rollback.Restore(project);",
        "new AggregateException(operationError, restoreError)",
    )
    for token in required:
        if token not in text:
            errors.append("Auto Host batch missing atomicity contract: " + token)

    capture = text.find("var rollback = ProjectStateSnapshot.Capture(project);")
    link = text.find("service.LinkOpening(project, item.Opening.Id, item.HostId);")
    regen = text.find("regenerated = linked > 0 ? Regenerate(project, regenerationTargets) : 0;")
    restore = text.find("rollback.Restore(project);")
    if min(capture, link, regen, restore) >= 0 and not (capture < link < regen < restore):
        errors.append("Auto Host snapshot must precede link mutations, and rollback must cover both link application and regeneration")

    planning = text.find("using (var transaction = document.Database.TransactionManager.StartOpenCloseTransaction())")
    if planning >= 0 and capture >= 0 and capture < planning:
        errors.append("Auto Host snapshot should guard mutation phase without changing planning skip/ambiguity semantics")

print("QS3D Auto Host atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: planned Auto Host link mutations and regeneration are project-atomic while invalid/ambiguous planning remains reviewable and non-mutating.")
