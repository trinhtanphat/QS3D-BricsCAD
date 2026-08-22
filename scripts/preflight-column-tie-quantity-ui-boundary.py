#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ColumnTieQuantityCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing " + str(SOURCE.relative_to(ROOT)))
else:
    text = SOURCE.read_text(encoding="utf-8")
    snapshot = "var snapshot = ProjectStateSnapshot.Capture(project);"
    touch = "project.Touch();"
    restore = "snapshot.Restore(project);"
    finalize = "FinalizeUi(document, message);"
    helper = "private static void FinalizeUi(Document document, string message)"
    refresh = "PaletteCoordinator.RefreshProject();"
    warning = "Cảnh báo UI sau Tie QTY commit"

    for token in (snapshot, touch, restore, finalize, helper, refresh, warning):
        if token not in text:
            errors.append("QS3DREBARTIEQTY missing post-commit boundary token: " + token)

    snapshot_pos = text.find(snapshot)
    touch_pos = text.find(touch)
    restore_pos = text.find(restore)
    finalize_pos = text.find(finalize)
    helper_pos = text.find(helper)
    if min(snapshot_pos, touch_pos, restore_pos, finalize_pos, helper_pos) >= 0:
        if not snapshot_pos < touch_pos < restore_pos < finalize_pos < helper_pos:
            errors.append("QS3DREBARTIEQTY must snapshot before mutation, retain catch/restore, and invoke FinalizeUi only after the semantic try/catch completes")
        post_commit_boundary = text[restore_pos + len(restore):finalize_pos]
        if refresh in post_commit_boundary or "PaletteCoordinator.SetStatus" in post_commit_boundary or "Editor.WriteMessage" in post_commit_boundary:
            errors.append("QS3DREBARTIEQTY must not perform fallible UI work directly between semantic rollback boundary and FinalizeUi")

    helper_body = text[helper_pos:] if helper_pos >= 0 else ""
    if helper_pos >= 0 and ("catch (System.Exception ex)" not in helper_body or refresh not in helper_body):
        errors.append("QS3DREBARTIEQTY FinalizeUi must contain and absorb post-commit UI failures")

if errors:
    print("QS3D column tie quantity UI-boundary preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DREBARTIEQTY preserves semantic rollback for calculation failures while isolating post-commit Palette/editor UI failures.")
