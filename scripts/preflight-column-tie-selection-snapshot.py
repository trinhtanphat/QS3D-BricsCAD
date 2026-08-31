#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/ColumnTieCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs"
errors = []

for path in (COMMAND, BUILDER):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if not errors:
    command = COMMAND.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")

    required_command = [
        'CommandMethod("QS3DREBARTIES3D", CommandFlags.UsePickSet)',
        'ExistingProjectMutationContext.Require(document, "Column Tie 3D")',
        'ColumnTieSolidBuilder.BuildSelected(document, project)',
        'count == 0',
        'SelectionGuidance',
        'OperationFailure',
        'UiSyncWarning',
        'PaletteCoordinator.RefreshProject()',
        'document.Editor.Regen()',
    ]
    for token in required_command:
        if token not in command:
            errors.append("ColumnTieCommands.cs missing contract token: " + token)

    forbidden_command = [
        "CadSelectionGuard.ReadImpliedSelection",
        ".SelectImplied()",
        "ex.Message",
        "exception.Message",
    ]
    for token in forbidden_command:
        if token in command:
            errors.append("ColumnTieCommands.cs must not expose/re-read selection detail: " + token)

    required_builder = [
        "document.Editor.SelectImplied()",
        "selection.Value.GetObjectIds()",
        "if (ids.Length == 0) return 0;",
        "ProjectStateSnapshot.Capture(project)",
        "using (document.LockDocument())",
        "GeneratedTieRebarOwnershipGuard.Build(project)",
        "MaxTiesPerElement",
        "MaxTiesPerBatch",
        "transaction.Commit()",
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("ColumnTieSolidBuilder.cs missing preserved safety token: " + token)

    if builder.count("document.Editor.SelectImplied()") != 1:
        errors.append("ColumnTieSolidBuilder.cs must own exactly one implied-selection acquisition")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Column Tie command delegates one authoritative implied-selection read to the native builder, preserves rollback/ownership/bounds, and redacts host exception detail from user-visible command/UI-sync failures.")
