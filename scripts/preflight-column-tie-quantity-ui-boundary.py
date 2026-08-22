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
    command_start = text.find('[CommandMethod("QS3DREBARTIEQTY", CommandFlags.UsePickSet)]')
    helper = "private static void FinalizeUi(Document document, string message)"
    helper_pos = text.find(helper, command_start + 1) if command_start >= 0 else -1
    command = text[command_start:helper_pos] if command_start >= 0 and helper_pos > command_start else ""

    snapshot = "var snapshot = ProjectStateSnapshot.Capture(project);"
    audit = 'AuditTrail.ForProject(project).Record("quantity.rebar.column.tie", element.Id,'
    restore = "snapshot.Restore(project);"
    finalize = "FinalizeUi(document, message);"
    refresh = "PaletteCoordinator.RefreshProject();"
    warning = "Cảnh báo UI sau Tie QTY commit"

    if not command:
        errors.append("cannot isolate QS3DREBARTIEQTY from FinalizeUi")
    else:
        for token in (snapshot, audit, restore, finalize):
            if token not in command:
                errors.append("QS3DREBARTIEQTY missing semantic/UI boundary token: " + token)

        snapshot_pos = command.find(snapshot)
        audit_pos = command.find(audit)
        restore_pos = command.find(restore)
        finalize_pos = command.find(finalize)
        if min(snapshot_pos, audit_pos, restore_pos, finalize_pos) >= 0:
            if not snapshot_pos < audit_pos < restore_pos < finalize_pos:
                errors.append("QS3DREBARTIEQTY must snapshot before audit-owned mutation, retain catch/restore, and invoke FinalizeUi only after the semantic try/catch completes")
            post_commit_boundary = command[restore_pos + len(restore):finalize_pos]
            if refresh in post_commit_boundary or "PaletteCoordinator.SetStatus" in post_commit_boundary or "Editor.WriteMessage" in post_commit_boundary:
                errors.append("QS3DREBARTIEQTY must not perform fallible UI work directly between semantic rollback boundary and FinalizeUi")

        if "project.Touch();" in command:
            errors.append("QS3DREBARTIEQTY revision must remain AuditTrail-owned without a redundant project.Touch()")
        if command.count(audit) != 1:
            errors.append("QS3DREBARTIEQTY must retain exactly one per-loop quantity.rebar.column.tie AuditTrail call site")

    for token in (helper, refresh, warning):
        if token not in text:
            errors.append("QS3DREBARTIEQTY missing post-commit UI isolation token: " + token)

    helper_body = text[helper_pos:] if helper_pos >= 0 else ""
    if helper_pos >= 0 and ("catch (System.Exception ex)" not in helper_body or refresh not in helper_body):
        errors.append("QS3DREBARTIEQTY FinalizeUi must contain and absorb post-commit UI failures")

if errors:
    print("QS3D column tie quantity UI-boundary preflight")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: QS3DREBARTIEQTY keeps audit-owned semantic revisions and rollback for calculation failures while isolating post-commit Palette/editor UI failures.")
