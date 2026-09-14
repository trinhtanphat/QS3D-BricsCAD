#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SlabMeshCommands.cs"
BUILDER = ROOT / "src/QS3D.BricsCAD.V25/Cad/SlabMeshSolidBuilder.cs"
errors = []

for path in (COMMAND, BUILDER):
    if not path.is_file():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if not errors:
    command = COMMAND.read_text(encoding="utf-8")
    builder = BUILDER.read_text(encoding="utf-8")

    required_builder = [
        "internal sealed class SlabMeshBuildResult",
        "public int Elements { get; }",
        "public int Bars { get; }",
        "public bool PostCommitCleanupWarning { get; }",
        "public SlabMeshBuildResult(int elements, int bars, bool postCommitCleanupWarning)",
        "transaction.Commit();",
        "cadCommitted = true;",
        "if (cadCommitted)",
        "cleanupWarning = true;",
        "return new SlabMeshBuildResult(pending.Count, pending.Sum(x => x.Handles.Count), cleanupWarning);",
        "rollback.Restore(project);",
        "new AggregateException(operationError, restoreError)",
    ]
    for token in required_builder:
        if token not in builder:
            errors.append("SlabMeshSolidBuilder.cs missing committed-cleanup contract: " + token)

    commit = builder.find("transaction.Commit();")
    committed = builder.find("cadCommitted = true;", commit)
    catch = builder.find("catch (Exception operationError)", committed)
    cleanup_branch = builder.find("if (cadCommitted)", catch)
    rollback = builder.find("rollback.Restore(project);", catch)
    outcome = builder.find("return new SlabMeshBuildResult(pending.Count, pending.Sum(x => x.Handles.Count), cleanupWarning);", catch)
    if min(commit, committed, catch, cleanup_branch, rollback, outcome) < 0 or not (commit < committed < catch < cleanup_branch < rollback < outcome):
        errors.append("Slab Mesh must distinguish post-commit cleanup warning from pre-commit rollback before returning committed outcome")

    required_command = [
        "var nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
        "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
        "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity);",
        "var result = SlabMeshSolidBuilder.BuildSelected(document, project);",
        "FinalizeUi(document, nativeDatabaseIdentity, message, result.PostCommitCleanupWarning);",
        "private static IntPtr GetNativeDatabaseIdentity(Document document)",
        "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
        "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
        "document.Database.UnmanagedObject == nativeDatabaseIdentity",
        "document.Editor.WriteMessage(\"\\nQS3D \" + visibleMessage);",
        "TryWriteMessage(document, nativeDatabaseIdentity, \"\\nQS3D \" + visibleMessage + \" UI sync warning.\");",
    ]
    for token in required_command:
        if token not in command:
            errors.append("SlabMeshCommands.cs missing post-commit affinity/outcome contract: " + token)

    for raw_detail in ("PostCommitCleanupWarning.Message", "postCommitCleanupWarning.Message", "operationError.Message"):
        if raw_detail in command or raw_detail in builder:
            errors.append("Slab Mesh must not expose raw cleanup exception detail: " + raw_detail)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Slab Mesh preserves rollback truth before native commit, returns a committed cleanup-warning outcome after native commit, and fences post-commit UI publication to the invocation document/native database generation.")
