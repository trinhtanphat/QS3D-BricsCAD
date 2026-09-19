from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
command = (ROOT / "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs").read_text(encoding="utf-8")
builder = (ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs").read_text(encoding="utf-8")

required_command = [
    'private const string MutationOutcomeIndeterminate',
    'BeamStirrupBuildResult result;',
    'result = BeamStirrupSolidBuilder.BuildSelected',
    'Report(document, nativeDatabaseIdentity, MutationOutcomeIndeterminate);',
    'if (result.PostCommitCleanupWarning)',
    'FinalizeUi(document, nativeDatabaseIdentity, MutationOutcomeIndeterminate, postCommitCleanupWarning: false);',
]
for needle in required_command:
    if needle not in command:
        raise SystemExit(f"missing command fail-safe contract: {needle}")

if command.index('RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);') > command.index('result = BeamStirrupSolidBuilder.BuildSelected'):
    raise SystemExit("document-generation fence must precede native mutation")

required_builder = [
    'var rollback = ProjectStateSnapshot.Capture(project);',
    'transaction.Commit();',
    'cadCommitted = true;',
    'catch (ObjectDisposedException operationError)',
    'rollback.Restore(project);',
]
for needle in required_builder:
    if needle not in builder:
        raise SystemExit(f"missing builder rollback/commit invariant: {needle}")

print("PASS: V25 beam stirrup command treats builder/cleanup outcomes as non-retryable indeterminate while preserving generation and rollback guards")
