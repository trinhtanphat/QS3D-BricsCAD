#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
coord = (ROOT / "src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs").read_text(encoding="utf-8")

required = [
    "private const int CadWorkQueued = 0;",
    "private const int CadWorkRunning = 1;",
    "private const int CadWorkCancelledBeforeStart = 2;",
    "internal bool TryBegin()",
    "internal bool CancelBeforeStart()",
    "if (!work.TryBegin())",
    "if (work.CancelBeforeStart())",
]
for token in required:
    if token not in coord:
        raise SystemExit(f"missing application-context timeout barrier contract: {token}")

invoke_start = coord.index("private static T InvokeInCadContext")
invoke_end = coord.index("private static void ExecuteCadContextWork", invoke_start)
invoke_body = coord[invoke_start:invoke_end]

wait = "if (!work.Done.Wait(CadDispatchTimeoutMilliseconds))"
cancel = "if (work.CancelBeforeStart())"
settle = "work.Done.Wait();"
dispatch = "Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadContextWork<T>, work);"
disposal_try = "try\n            {"
if wait not in invoke_body:
    raise SystemExit("application-context dispatch must keep the bounded initial wait")
if cancel not in invoke_body:
    raise SystemExit("timed-out dispatch must atomically cancel work that has not started")
if settle not in invoke_body:
    raise SystemExit("timeout racing with already-started callback must fail closed until callback settles")
if dispatch not in invoke_body or disposal_try not in invoke_body:
    raise SystemExit("application-context dispatch must remain under the work-handle disposal boundary")
if invoke_body.index(dispatch) < invoke_body.index(disposal_try):
    raise SystemExit("synchronous ExecuteInApplicationContext failure must still dispose the work completion handle")
if invoke_body.index(wait) > invoke_body.index(cancel):
    raise SystemExit("cancel-before-start must only be attempted after the bounded wait expires")
if invoke_body.index(cancel) > invoke_body.index(settle):
    raise SystemExit("started callback must settle only after cancellation loses the atomic race")

execute_start = coord.index("private static void ExecuteCadContextWork")
execute_end = coord.index("private static void OnCommandWillStart", execute_start)
execute_body = coord[execute_start:execute_end]
if execute_body.index("if (!work.TryBegin())") > execute_body.index("work.Action()"):
    raise SystemExit("late callback must claim execution before running the side-effecting CAD action")

print("PASS: MCP application-context timeout cannot ghost-arm native writer state")
