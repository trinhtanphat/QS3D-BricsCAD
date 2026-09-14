#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/FoundationMeshCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/FoundationMeshSolidBuilder.cs"
errors = []

for path in (COMMAND, BUILDER):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if not errors:
    command = COMMAND.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")

    required_command = [
        'CommandMethod("QS3DFOUNDATIONREBAR3D", CommandFlags.UsePickSet)',
        'var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);',
        'var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);',
        'RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);',
        'FoundationMeshSolidBuilder.BuildSelected(document, project, selectedIds)',
        'FinalizeUi(document, nativeDatabaseIdentity, message, result.PostCommitCleanupWarning);',
        'private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)',
        'ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)',
        'document.Database.UnmanagedObject == nativeDatabaseIdentity',
    ]
    for token in required_command:
        if token not in command:
            errors.append("FoundationMeshCommands.cs missing lifecycle contract: " + token)

    acquire = command.find('var selectedIds = CadSelectionGuard.AcquireCurrentSelection(document);')
    require = command.find('ExistingProjectMutationContext.Require(document, "Foundation Rebar 3D")', acquire)
    fence = command.find('RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);', require)
    build = command.find('FoundationMeshSolidBuilder.BuildSelected(document, project, selectedIds)', fence)
    if min(acquire, require, fence, build) < 0 or not (acquire < require < fence < build):
        errors.append("Foundation Mesh must preserve selection snapshot -> project bind -> native-generation fence -> same-snapshot geometry handoff")

    for raw_detail in ('ex.Message', 'ex.GetType().Name', 'exception.Message'):
        if raw_detail in command:
            errors.append("Foundation Mesh public UI must redact host exception-derived detail: " + raw_detail)

    required_builder = [
        'public bool PostCommitCleanupWarning { get; set; }',
        'BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)',
        'if (selectedIds == null) throw new ArgumentNullException(nameof(selectedIds));',
        'var ids = (ObjectId[])selectedIds.Clone();',
        'ProjectStateSnapshot.Capture(project)',
        'transaction.Commit();',
        'cadCommitted = true;',
        'catch (ObjectDisposedException operationError)',
        'if (cadCommitted)',
        'cleanupWarning = true;',
        'rollback.Restore(project);',
        'new AggregateException(operationError, restoreError)',
        'PostCommitCleanupWarning = cleanupWarning',
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("FoundationMeshSolidBuilder.cs missing atomic committed-outcome contract: " + token)

    for forbidden in ('document.Editor.SelectImplied()', 'document.Editor.GetSelection()', 'document.Editor.SetImpliedSelection('):
        if forbidden in builder:
            errors.append("FoundationMeshSolidBuilder.cs must consume admitted ObjectId snapshot without re-reading/mutating selection: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Foundation Mesh uses one admitted selection snapshot, exact document/native-generation affinity, rollback-owned precommit failure, committed post-cleanup outcome, and redacted generation-safe UI publication.")
