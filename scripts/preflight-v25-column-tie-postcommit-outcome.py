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

    required_builder = [
        "internal sealed class ColumnTieBuildResult",
        "public int Count { get; }",
        "public bool PostCommitCleanupWarning { get; }",
        "public ColumnTieBuildResult(int count, bool postCommitCleanupWarning)",
        "transaction.Commit();",
        "cadCommitted = true;",
        "catch (ObjectDisposedException operationError)",
        "if (cadCommitted)",
        "cleanupWarning = true;",
        "rollback.Restore(project);",
        "new AggregateException(operationError, restoreError)",
        "return new ColumnTieBuildResult(totalTies, cleanupWarning);",
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("ColumnTieSolidBuilder.cs missing committed-cleanup contract: " + token)

    commit = builder.find("transaction.Commit();")
    committed = builder.find("cadCommitted = true;", commit)
    disposed_catch = builder.find("catch (ObjectDisposedException operationError)", committed)
    cleanup_branch = builder.find("if (cadCommitted)", disposed_catch)
    rollback = builder.find("rollback.Restore(project);", disposed_catch)
    outcome = builder.find("return new ColumnTieBuildResult(totalTies, cleanupWarning);", disposed_catch)
    if min(commit, committed, disposed_catch, cleanup_branch, rollback, outcome) < 0 or not (commit < committed < disposed_catch < cleanup_branch < rollback < outcome):
        errors.append("Column Tie must distinguish post-commit disposal warning from pre-commit rollback before returning committed outcome")

    required_command = [
        "var result = ColumnTieSolidBuilder.BuildSelected(document, project, selectedIds);",
        "var count = result.Count;",
        "FinalizeUi(document, nativeDatabaseIdentity, message, result.PostCommitCleanupWarning);",
        "private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message, bool postCommitCleanupWarning)",
        "GetNativeDatabaseIdentity(document)",
        "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);",
        "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)",
    ]
    for token in required_command:
        if token not in command:
            errors.append("ColumnTieCommands.cs missing post-commit outcome/affinity contract: " + token)

    for raw_detail in (
        "ex.GetType().Name",
        "operationError.Message",
        "cleanupError.Message",
        "PostCommitCleanupWarning.Message",
        "postCommitCleanupWarning.Message",
    ):
        if raw_detail in command or raw_detail in builder:
            errors.append("Column Tie must not expose exception-derived cleanup detail: " + raw_detail)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Column Tie preserves rollback truth before native commit, classifies post-commit disposal as a committed warning outcome, and keeps generation-bound UI publication redacted.")
