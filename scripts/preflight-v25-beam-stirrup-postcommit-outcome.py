#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/BeamStirrupCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/BeamStirrupSolidBuilder.cs"
errors = []

for path in (COMMAND, BUILDER):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if not errors:
    command = COMMAND.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")

    required_builder = [
        "internal sealed class BeamStirrupBuildResult",
        "public int Elements { get; }",
        "public int Stirrups { get; }",
        "public bool PostCommitCleanupWarning { get; }",
        "public BeamStirrupBuildResult(int elements, int stirrups, bool postCommitCleanupWarning)",
        "transaction.Commit();",
        "cadCommitted = true;",
        "if (cadCommitted)",
        "cleanupWarning = true;",
        "rollback.Restore(project);",
        "new AggregateException(operationError, restoreError)",
        "return new BeamStirrupBuildResult(pending.Count, count, cleanupWarning);",
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("BeamStirrupSolidBuilder.cs missing committed-cleanup contract: " + token)

    commit = builder.find("transaction.Commit();")
    committed = builder.find("cadCommitted = true;", commit)
    catch = builder.find("catch (Exception operationError)", committed)
    cleanup_branch = builder.find("if (cadCommitted)", catch)
    rollback = builder.find("rollback.Restore(project);", catch)
    outcome = builder.find("return new BeamStirrupBuildResult(pending.Count, count, cleanupWarning);", catch)
    if min(commit, committed, catch, cleanup_branch, rollback, outcome) < 0 or not (commit < committed < catch < cleanup_branch < rollback < outcome):
        errors.append("Beam Stirrup must distinguish post-commit cleanup warning from pre-commit rollback before returning committed outcome")

    required_command = [
        "var result = BeamStirrupSolidBuilder.BuildSelected(document, project, selectedIds, expectedTargetIds);",
        "FinalizeUi(document, nativeDatabaseIdentity, message, result.PostCommitCleanupWarning);",
        "private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message, bool postCommitCleanupWarning)",
        "GetNativeDatabaseIdentity(document)",
        "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);",
        "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)",
    ]
    for token in required_command:
        if token not in command:
            errors.append("BeamStirrupCommands.cs missing post-commit outcome/affinity contract: " + token)

    for raw_detail in (
        "PostCommitCleanupWarning.Message",
        "postCommitCleanupWarning.Message",
        "operationError.Message",
        "cleanupError.Message",
    ):
        if raw_detail in command or raw_detail in builder:
            errors.append("Beam Stirrup must not expose raw cleanup exception detail: " + raw_detail)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Beam Stirrup preserves rollback truth before native commit, returns a committed cleanup-warning outcome after native commit, and propagates that outcome through the existing generation-bound UI finalization path.")
