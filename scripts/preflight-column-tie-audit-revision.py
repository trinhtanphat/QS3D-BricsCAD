#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ColumnTieQuantityCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ColumnTieQuantityCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start = text.find('[CommandMethod("QS3DREBARTIEQTY", CommandFlags.UsePickSet)]')
    end = text.find("private static void FinalizeUi", start)
    if start < 0 or end <= start:
        errors.append("cannot isolate QS3DREBARTIEQTY")
    else:
        command = text[start:end]
        required = (
            "document.Editor.SelectImplied()",
            "document.Editor.GetSelection()",
            "if (selected.Count == 0) return;",
            "ExistingProjectMutationContext.TryGet(document, out var project)",
            "if (targets.Count == 0)",
            "ProjectStateSnapshot.Capture(project)",
            "ColumnTieProjectQuantityService.Calculate(element, project.FindFamily(element.FamilyId))",
            'element.Quantities["TieRebarCount"] = quantity.Count;',
            'element.Quantities["TieRebarCutLengthM"] = quantity.CuttingLengthPerTieM;',
            'element.Quantities["TieRebarTotalLengthM"] = quantity.TotalLengthM;',
            'element.Quantities["TieRebarKgPerM"] = quantity.KgPerMeter;',
            'element.Quantities["TieRebarWeightKg"] = quantity.TotalWeightKg;',
            'AuditTrail.ForProject(project).Record("quantity.rebar.column.tie", element.Id,',
            "snapshot.Restore(project);",
        )
        for token in required:
            if token not in command:
                errors.append("Tie QTY missing audit-owned revision token: " + token)

        selection_at = command.find("document.Editor.SelectImplied()")
        project_at = command.find("ExistingProjectMutationContext.TryGet(document, out var project)")
        target_at = command.find("if (targets.Count == 0)")
        snapshot_at = command.find("ProjectStateSnapshot.Capture(project)")
        audit_at = command.find('AuditTrail.ForProject(project).Record("quantity.rebar.column.tie", element.Id,')
        restore_at = command.find("snapshot.Restore(project);")
        if min(selection_at, project_at, target_at, snapshot_at, audit_at, restore_at) >= 0:
            if not selection_at < project_at < target_at < snapshot_at < audit_at < restore_at:
                errors.append("Tie QTY lifecycle/rollback ordering drifted")

        if "project.Touch();" in command:
            errors.append("Tie QTY must not add a standalone project.Touch after per-target AuditTrail records")
        if command.count('AuditTrail.ForProject(project).Record("quantity.rebar.column.tie", element.Id,') != 1:
            errors.append("Tie QTY source must retain exactly one per-loop quantity.rebar.column.tie AuditTrail call site")

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Column Tie QTY keeps per-target audit-owned revisions, quantity writes and rollback without a redundant batch-tail ProjectState.Touch.")
