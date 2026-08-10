#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "straight/non-bulged": {
        "path": "src/QS3D.BricsCAD.V25/Cad/OpeningBooleanService.cs",
        "audit": '"geometry.opening.boolean",',
        "extra": (),
    },
    "curved/bulged": {
        "path": "src/QS3D.BricsCAD.V25/Cad/CurvedOpeningBooleanService.cs",
        "audit": 'AuditTrail.ForProject(project).Record("geometry.opening.boolean.curved"',
        "extra": ('update.Host.Properties["PhysicalOpeningCutMode"] = "CurvedCenterlineFootprint";',),
    },
}

for label, contract in contracts.items():
    path = ROOT / contract["path"]
    if not path.is_file():
        errors.append(label + ": missing physical opening boolean service")
        continue

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
        'update.Host.Properties["PhysicalOpeningCutSolidHandle"]',
        'update.Host.Properties["PhysicalOpeningCutFingerprint"]',
        'update.Host.Properties["PhysicalOpeningCutCount"]',
        contract["audit"],
        "private static void TryRegen",
    ) + tuple(contract["extra"])
    for needle in required:
        if needle not in text:
            errors.append(label + " opening boolean atomicity guard missing: " + needle)

    semantic = text.find("foreach (var update in pending) CommitSemanticUpdate(project, update);")
    touch = text.find("if (pending.Count > 0) project.Touch();", semantic)
    commit = text.find("transaction.Commit();", semantic)
    committed = text.find("cadCommitted = true;", commit)
    restore = text.find("rollback.Restore(project)", committed)
    if min(semantic, touch, commit, committed, restore) < 0 or not (semantic < touch < commit < committed < restore):
        errors.append(label + " opening boolean semantic/touch/CAD commit/rollback ordering is not rollback-safe")

    post_commit = text[committed:] if committed >= 0 else ""
    if "document.Editor.Regen();" in post_commit and "TryRegen(document);" not in post_commit:
        errors.append(label + " opening boolean still performs fatal Editor.Regen after native commit")
    if semantic >= 0 and "CommitSemanticUpdate(project, update)" in text[commit + len("transaction.Commit();"):]:
        errors.append(label + " opening boolean still advances physical-cut semantic metadata after CAD commit")

print("QS3D physical opening boolean atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: straight/non-bulged and curved/bulged physical opening cuts advance PhysicalOpeningCut metadata/audit/touch while native boolean transactions remain rollback-capable, restore deep project state on pre-commit failure, and keep post-commit viewport regen non-fatal.")
