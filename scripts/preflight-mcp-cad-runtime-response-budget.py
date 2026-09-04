#!/usr/bin/env python3
"""Focused source guard for core MCP CAD dispatch response budget."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
DOC = ROOT / "docs" / "FEATURE-RUNBOOKS" / "mcp-cad-runtime-response-budget.md"


def main() -> int:
    errors: list[str] = []
    if not SRC.is_file():
        print(f"ERROR: missing {SRC.relative_to(ROOT)}")
        return 1

    source = SRC.read_text(encoding="utf-8")
    match = re.search(r"private const int CadDispatchTimeoutMilliseconds\s*=\s*(\d+)\s*;", source)
    if not match:
        errors.append("missing CadDispatchTimeoutMilliseconds constant")
    elif int(match.group(1)) != 8000:
        errors.append(f"core CAD dispatch timeout must reserve response budget at 8000 ms, found {match.group(1)}")

    invoke_start = source.find("private static string InvokeCad(Func<string> action)")
    execute_start = source.find("private static void ExecuteCadWork", invoke_start)
    invoke = source[invoke_start:execute_start] if invoke_start >= 0 and execute_start > invoke_start else ""
    for token in (
        "item.Done.Wait(CadDispatchTimeoutMilliseconds)",
        "Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued)",
        "queued work was cancelled before start",
        "item.DetachAfterStartedTimeout()",
        "throw new CadStartedTimeoutException(item);",
        "item.DisposeCallerCompletionIfOwned();",
    ):
        if token not in invoke:
            errors.append(f"InvokeCad must preserve bounded fail-closed semantics: {token}")

    if "item.Done.Wait();" in invoke:
        errors.append("already-started CAD work must not extend the MCP response with an unbounded completion wait")
    if "item.Abandoned" in invoke or "Interlocked.Exchange(ref item.Abandoned" in invoke:
        errors.append("InvokeCad must not restore the racy abandoned completion-handle handoff")
    if "Thread.Sleep(" in invoke:
        errors.append("InvokeCad timeout path must not extend the response deadline with Thread.Sleep")

    mutation_start = source.find("private static string Mutation(string body, string tool, Func<string> action)")
    mutation_end = source.find("private static bool IsDurabilitySaveTool", mutation_start)
    mutation = source[mutation_start:mutation_end] if mutation_start >= 0 and mutation_end > mutation_start else ""
    for token in (
        "catch (CadStartedTimeoutException timeout)",
        "McpCadMutationCoordinator.DetachMutationForDeferredCompletion(writerScope)",
        "timeout.TransferWriterScope(deferredWriterScope)",
        "writerScope = null",
    ):
        if token not in mutation:
            errors.append(f"started timeout must retain writer ownership through deferred completion: {token}")

    if not DOC.is_file():
        errors.append(f"missing runbook: {DOC.relative_to(ROOT)}")
    else:
        doc = DOC.read_text(encoding="utf-8")
        for token in (
            "Lane-Key: `issue-5164`",
            "15 seconds",
            "8 seconds",
            "cad_active_document",
            "cad_view_state",
            "cad_sysvar",
            "raw 502",
            "completion-uncertain",
            "LOCAL_ONLY",
        ):
            if token not in doc:
                errors.append(f"runbook missing contract token: {token}")

    if errors:
        print("FAIL: MCP CAD runtime response-budget guard")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: MCP CAD runtime response-budget guard")
    return 0


if __name__ == "__main__":
    sys.exit(main())
