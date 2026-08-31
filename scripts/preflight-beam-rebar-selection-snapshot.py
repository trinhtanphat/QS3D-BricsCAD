#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/BeamRebarCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamRebarSolidBuilder.cs"
errors = []

for path in (COMMAND, BUILDER):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if not errors:
    command = COMMAND.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")

    required_command = [
        'CommandMethod("QS3DBEAMREBAR3D", CommandFlags.UsePickSet)',
        'var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);',
        'if (selectedIds.Length == 0)',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)',
        'ExistingProjectMutationContext.Require(document, "Beam Rebar 3D")',
        'expectedChangeVersion',
        'expectedTargetIds.SetEquals',
        'BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds)',
        'OperationFailure',
        'UiSyncWarning',
        'PaletteCoordinator.RefreshProject()',
        'document.Editor.Regen()',
    ]
    for token in required_command:
        if token not in command:
            errors.append("BeamRebarCommands.cs missing contract token: " + token)

    acquire = command.find('var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);')
    empty = command.find('if (selectedIds.Length == 0)', acquire)
    preview = command.find('ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)', empty)
    require = command.find('ExistingProjectMutationContext.Require(document, "Beam Rebar 3D")', preview)
    build = command.find('BeamRebarSolidBuilder.BuildSelected(document, project, selectedIds)', require)
    if min(acquire, empty, preview, require, build) < 0 or not (acquire < empty < preview < require < build):
        errors.append("Beam Rebar must enforce selection snapshot -> empty return -> read-only preview -> canonical project bind -> same-snapshot native build")

    if "ex.Message" in command or "exception.Message" in command:
        errors.append("BeamRebarCommands.cs must not expose raw host/native exception detail")

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
            errors.append("BeamRebarSolidBuilder.cs missing preserved safety token: " + token)

    for token in ('document.Editor.SelectImplied()', 'document.Editor.GetSelection()', 'document.Editor.SetImpliedSelection', 'PromptStatus'):
        if token in builder:
            errors.append("BeamRebarSolidBuilder.cs must consume admitted snapshot without editor re-selection: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Beam Rebar captures selection once, revalidates project/semantic targets, passes the exact cloned snapshot into native generation, preserves rollback/ownership/bounds, and redacts host exception detail from user-visible command/UI-sync failures.")
