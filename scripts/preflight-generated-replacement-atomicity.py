#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

contracts = {
    "LINE wall": {
        "path": "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs",
        "start": "public static int BuildSelectedLineWalls(\n            Document document,\n            ProjectState project,\n            ElementCategory category,\n            bool allowPostCommitUi = true)",
        "end": "private static SourceBatchKind ValidateSourceBatch",
    },
    "POLYLINE wall": {
        "path": "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs",
        "start": "public static int BuildSelected(\n            Document document,\n            ProjectState project,\n            ElementCategory category,\n            bool allowPostCommitUi = true)",
        "end": "private static void CommitWallPierPathSnapshot",
    },
    "WallPier profile": {
        "path": "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs",
        "start": "public static int BuildSelectedLinePiers(Document document, ProjectState project)",
        "end": "private static void ClearPathProfileSnapshot",
    },
    "structural": {
        "path": "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs",
        "start": "public static int BuildSelected(Document document, ProjectState project, ElementCategory category)",
        "end": "private static bool UsesLine",
    },
}

commit_token = "transaction.Commit();"
committed_token = "cadCommitted = true;"

for label, contract in contracts.items():
    path = ROOT / contract["path"]
    if not path.is_file():
        errors.append(label + ": missing builder " + contract["path"])
        continue

    text = path.read_text(encoding="utf-8")
    if "using QS3D.Core.Persistence;" not in text:
        errors.append(label + ": missing explicit ProjectStateSnapshot namespace")

    start = text.find(contract["start"])
    end = text.find(contract["end"], start + 1) if start >= 0 else -1
    if start < 0 or end < 0 or end <= start:
        errors.append(label + ": cannot isolate canonical build method for atomicity review")
        continue
    body = text[start:end]

    required = (
        "ProjectStateSnapshot.Capture(project)",
        "var cadCommitted = false;",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.MarkGenerated",
        "GeneratedGeometryService.CommitReplacement",
        commit_token,
        committed_token,
        "catch (Exception operationError)",
        "if (!cadCommitted)",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
    )
    for token in required:
        if token not in body:
            errors.append(label + ": missing atomic replacement contract: " + token)

    semantic_index = body.find("GeneratedGeometryService.CommitReplacement")
    cad_commit_index = body.find(commit_token)
    committed_index = body.find(committed_token, cad_commit_index + 1)
    restore_index = body.find("rollback.Restore(project)")
    if min(semantic_index, cad_commit_index, committed_index, restore_index) < 0:
        continue
    if not semantic_index < cad_commit_index < committed_index < restore_index:
        errors.append(label + ": semantic replacement must occur before CAD commit; the durable-commit flag must follow commit; project restore belongs only to the pre-commit failure path")

    if body.count("GeneratedGeometryService.CommitReplacement") != 1:
        errors.append(label + ": expected exactly one semantic replacement phase inside the canonical build method")
    if body.count(commit_token) != 1 or body.count(committed_token) != 1:
        errors.append(label + ": expected exactly one CAD commit and one durable-commit flag inside the canonical build method")

    # Guard specifically against the original split-brain pattern. Searching only this
    # build-method slice prevents a later helper transaction from being mistaken for the
    # relevant CAD commit if someone moves CommitReplacement below the real commit again.
    after_commit = body[cad_commit_index + len(commit_token):]
    if "GeneratedGeometryService.CommitReplacement" in after_commit:
        errors.append(label + ": semantic generated ownership is still mutated after CAD commit")

    if label == "structural":
        stage_after = body.find("undoTransition.StageAfter(project, afterSnapshot);")
        confirm = body.find("undoTransition?.ConfirmCommitted();", cad_commit_index + 1)
        if min(stage_after, confirm) < 0 or not semantic_index < stage_after < cad_commit_index < confirm < committed_index:
            errors.append(
                "structural: semantic Undo after-snapshot must be staged before native commit and its in-session history published only after the CAD commit, before the durable-commit lifecycle continues"
            )

snapshot = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"
if not snapshot.is_file():
    errors.append("missing ProjectStateSnapshot rollback primitive")
else:
    text = snapshot.read_text(encoding="utf-8")
    for token in (
        "public static ProjectStateSnapshot Capture(ProjectState project)",
        "public void Restore(ProjectState project)",
        "target.Elements.Clear()",
        "target.AuditEvents.Clear()",
        "targetMetadata.ReplacePersistenceState(source.Metadata)",
        "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion)",
    ):
        if token not in text:
            errors.append("ProjectStateSnapshot missing deep rollback contract: " + token)

print("QS3D generated replacement atomicity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: canonical LINE/POLYLINE wall, WallPier-profile and structural build methods commit generated semantic ownership while the CAD transaction is still rollback-capable; structural native-Undo history is staged before and published after that commit; helper transactions cannot mask ordering regressions, and pre-commit failures restore a deep project snapshot.")
