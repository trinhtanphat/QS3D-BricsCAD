#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / "src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs"
errors = []

if not path.is_file():
    errors.append("missing CurvedOpeningBooleanService.cs")
else:
    text = path.read_text(encoding="utf-8")
    required = (
        "using QS3D.Core.Persistence;",
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "foreach (var update in pending) CommitSemanticUpdate(project, update);",
        "if (pending.Count > 0) project.Touch();",
        "transaction.Commit();",
        "cadCommitted = true;",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
        "new AggregateException(operationError, restoreError)",
        "private static void CommitSemanticUpdate",
        'update.Host.Properties["PhysicalOpeningCutMode"] = "CurvedCenterlineFootprint";',
        "private static void TryRegen",
    )
    for needle in required:
        if needle not in text:
            errors.append("Curved opening boolean atomicity guard missing: " + needle)

    semantic = text.find("foreach (var update in pending) CommitSemanticUpdate(project, update);")
    touch = text.find("if (pending.Count > 0) project.Touch();", semantic)
    commit = text.find("transaction.Commit();", semantic)
    committed = text.find("cadCommitted = true;", commit)
    if min(semantic, touch, commit, committed) < 0 or not (semantic < touch < commit < committed):
        errors.append("Curved opening semantic/touch/CAD commit ordering is not rollback-safe")

    post_commit = text[committed:] if committed >= 0 else ""
    if "document.Editor.Regen();" in post_commit and "TryRegen(document);" not in post_commit:
        errors.append("Curved opening boolean still performs fatal Editor.Regen after native commit")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: curved physical opening metadata/audit/touch are applied before native commit with ProjectState rollback, and post-commit viewport regen is best-effort.")
