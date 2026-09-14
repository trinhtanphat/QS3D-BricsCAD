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

    required_builder = [
        "internal sealed class ColumnRebarBuildOutcome",
        "public int Count { get; }",
        "public bool PostCommitCleanupWarning { get; }",
        "public static ColumnRebarBuildOutcome BuildSelected(Document document, ProjectState project, ObjectId[] selectedIds)",
        "if (selectedIds.Length == 0) return new ColumnRebarBuildOutcome(0, false);",
        "transaction.Commit();",
        "cadCommitted = true;",
        "if (cadCommitted)",
        "cleanupWarning = true;",
        "return new ColumnRebarBuildOutcome(totalBars, cleanupWarning);",
        "rollback.Restore(project);",
        "new AggregateException(operationError, restoreError)",
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("ColumnRebarSolidBuilder.cs missing committed-cleanup contract: " + token)

    commit = builder.find("transaction.Commit();")
    committed = builder.find("cadCommitted = true;", commit)
    catch = builder.find("catch (Exception operationError)", committed)
    cleanup_branch = builder.find("if (cadCommitted)", catch)
    rollback = builder.find("rollback.Restore(project);", catch)
    outcome = builder.find("return new ColumnRebarBuildOutcome(totalBars, cleanupWarning);", catch)
    if min(commit, committed, catch, cleanup_branch, rollback, outcome) < 0 or not (commit < committed < catch < cleanup_branch < rollback < outcome):
        errors.append("Column Rebar must distinguish post-commit cleanup warning from pre-commit rollback before returning committed outcome")

    required_command = [
        "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
        "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);",
        "var outcome = ColumnRebarSolidBuilder.BuildSelected(document, project, selectedIds);",
        "var count = outcome.Count;",
        "FinalizeUi(document, nativeDatabaseIdentity, message, outcome.PostCommitCleanupWarning);",
        "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
        "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
        "document.Database.UnmanagedObject == nativeDatabaseIdentity",
    ]
    for token in required_command:
        if token not in command:
            errors.append("RebarGeometryCommands.cs missing post-commit affinity/outcome contract: " + token)

    for raw_detail in ("PostCommitCleanupWarning.Message", "postCommitCleanupWarning.Message", "operationError.Message"):
        if raw_detail in command or raw_detail in builder:
            errors.append("Column Rebar must not expose raw cleanup exception detail: " + raw_detail)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Column Rebar preserves rollback truth before native commit, returns a committed cleanup-warning outcome after native commit, and fences post-commit UI publication to the invocation document/native database generation.")
