#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

builders = (
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallFrameSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/Cad/CurtainWallPathFrameSolidBuilder.cs",
)

for relative in builders:
    path = ROOT / relative
    if not path.is_file():
        errors.append("missing curtain frame builder: " + relative)
        continue
    text = path.read_text(encoding="utf-8")
    required = (
        "ProjectStateSnapshot.Capture(project)",
        "ApplyPendingSemanticState(project, pending)",
        "transaction.Commit();",
        "projectRollback.Restore(project)",
        "AuditTrail.ForProject(project).Record",
        "if (pending.Count > 0) project.Touch();",
    )
    for token in required:
        if token not in text:
            errors.append(relative + " missing replacement-journal token: " + token)

    semantic_apply = text.find("ApplyPendingSemanticState(project, pending)")
    cad_commit = text.find("transaction.Commit();", semantic_apply)
    restore = text.find("projectRollback.Restore(project)")
    result = text.find("return new CurtainFrameBuildResult", restore)
    if min(semantic_apply, cad_commit, restore, result) < 0:
        errors.append(relative + " has incomplete replacement-journal ordering")
    elif not (semantic_apply < cad_commit < restore < result):
        errors.append(relative + " must apply semantic ownership before CAD commit and restore snapshot only on failed operation")

    helper = text.split("private static void ApplyPendingSemanticState", 1)
    if len(helper) != 2:
        errors.append(relative + " must isolate pending semantic ownership in one helper")
    else:
        helper_body = helper[1].split("private static ", 1)[0]
        if "GeneratedCurtainFrameHandles" not in helper_body and "HandlesKey" not in helper_body:
            errors.append(relative + " semantic journal must write GeneratedCurtainFrameHandles")
        if "ClearGeneratedCurtainFrameStale" not in helper_body:
            errors.append(relative + " semantic journal must clear curtain frame stale state before CAD commit")

    if "document.Editor.Regen();" in text:
        errors.append(relative + " must not perform post-commit UI regen inside the native/semantic replacement builder")

if errors:
    print("QS3D Curtain frame replacement journal preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: LINE/path Curtain frame builders apply semantic ownership while the native transaction is still rollback-capable, restore the project snapshot on failed CAD commit, and leave post-commit UI synchronization to command surfaces.")
