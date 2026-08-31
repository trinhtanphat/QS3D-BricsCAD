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
        'var selectedIds = CadSelectionGuard.ReadImpliedSelection(document);',
        'if (selectedIds.Length == 0)',
        'ExistingProjectMutationContext.Require(document, "Column Tie 3D")',
        'ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds)',
        'SelectionGuidance',
        'OperationFailure',
        'UiSyncWarning',
        'PaletteCoordinator.RefreshProject()',
        'document.Editor.Regen()',
    ]
    for token in required_command:
        if token not in command:
            errors.append("ColumnTieCommands.cs missing contract token: " + token)

    acquire = command.find('var selectedIds = CadSelectionGuard.ReadImpliedSelection(document);')
    empty = command.find('if (selectedIds.Length == 0)', acquire)
    require = command.find('ExistingProjectMutationContext.Require(document, "Column Tie 3D")', empty)
    build = command.find('ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds)', require)
    if min(acquire, empty, require, build) < 0 or not (acquire < empty < require < build):
        errors.append("Column Tie must enforce PICKFIRST snapshot -> empty return -> canonical project bind -> same-snapshot native build")

    for token in (".SelectImplied()", "ex.Message", "exception.Message"):
        if token in command:
            errors.append("ColumnTieCommands.cs must not expose/re-read native selection detail: " + token)

    required_builder = [
        'BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)',
        'if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));',
        'if (selectedIds.Length == 0) return 0;',
        'var ids = (ObjectId[])selectedIds.Clone();',
        'ProjectStateSnapshot.Capture(project)',
        'using (document.LockDocument())',
        'GeneratedTieRebarOwnershipGuard.Build(project)',
        'MaxTiesPerElement',
        'MaxTiesPerBatch',
        'transaction.Commit()',
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("ColumnTieSolidBuilder.cs missing preserved safety token: " + token)

    for token in ('document.Editor.SelectImplied()', 'CadSelectionGuard.ReadImpliedSelection'):
        if token in builder:
            errors.append("ColumnTieSolidBuilder.cs must consume the admitted snapshot without re-reading selection: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Column Tie captures PICKFIRST once before project binding, passes the exact cloned snapshot into native generation, preserves rollback/ownership/bounds, and redacts host exception detail from user-visible command/UI-sync failures.")
