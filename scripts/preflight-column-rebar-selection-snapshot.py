#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/RebarGeometryCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/ColumnRebarSolidBuilder.cs"
errors = []

for path in (COMMAND, BUILDER):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if not errors:
    command = COMMAND.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")

    required_command = [
        'CommandMethod("QS3DREBAR3D", CommandFlags.UsePickSet)',
        'var selectedIds = CadSelectionGuard.ReadImpliedSelection(document);',
        'if (selectedIds.Length == 0)',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)',
        'ExistingProjectMutationContext.Require(document, "Rebar 3D")',
        'expectedChangeVersion',
        'expectedTargetIds.SetEquals',
        'ColumnRebarSolidBuilder.BuildSelected(document, project, selectedIds)',
        'SelectionGuidance',
        'OperationFailure',
        'UiSyncWarning',
        'PaletteCoordinator.RefreshProject()',
        'document.Editor.Regen()',
    ]
    for token in required_command:
        if token not in command:
            errors.append("RebarGeometryCommands.cs missing contract token: " + token)

    acquire = command.find('var selectedIds = CadSelectionGuard.ReadImpliedSelection(document);')
    empty = command.find('if (selectedIds.Length == 0)', acquire)
    preview = command.find('ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)', empty)
    require = command.find('ExistingProjectMutationContext.Require(document, "Rebar 3D")', preview)
    build = command.find('ColumnRebarSolidBuilder.BuildSelected(document, project, selectedIds)', require)
    if min(acquire, empty, preview, require, build) < 0 or not (acquire < empty < preview < require < build):
        errors.append("Column Rebar must enforce PICKFIRST snapshot -> empty return -> read-only preview -> canonical project bind -> same-snapshot native build")

    for token in (".SelectImplied()", "ex.Message", "exception.Message"):
        if token in command:
            errors.append("RebarGeometryCommands.cs must not expose/re-read native selection detail: " + token)

    required_builder = [
        'BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)',
        'if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));',
        'if (selectedIds.Length == 0) return 0;',
        'var ids = (ObjectId[])selectedIds.Clone();',
        'ProjectStateSnapshot.Capture(project)',
        'using (document.LockDocument())',
        'GeneratedRebarOwnershipGuard.Build(project)',
        'MaxBarsPerElement',
        'MaxBarsPerBatch',
        'GeneratedRebarNativeOwnershipService.RequireMatchingOwnership',
        'CommitSemanticUpdate(project, update)',
        'transaction.Commit()',
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("ColumnRebarSolidBuilder.cs missing preserved safety token: " + token)

    for token in ('document.Editor.SelectImplied()', 'CadSelectionGuard.ReadImpliedSelection', 'PromptStatus'):
        if token in builder:
            errors.append("ColumnRebarSolidBuilder.cs must consume the admitted snapshot without re-reading selection: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Column Rebar captures PICKFIRST once, revalidates project/semantic targets, passes the exact cloned snapshot into native generation, preserves rollback/ownership/bounds, and redacts host exception detail from user-visible command/UI-sync failures.")
