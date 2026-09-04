#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
runtime = (ROOT / "src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs").read_text(encoding="utf-8")

required = [
    "private const int CadWorkQueued = 0;",
    "private const int CadWorkRunning = 1;",
    "private const int CadWorkCancelledBeforeStart = 2;",
    "private static string InvokeCad(Func<string> action)",
    "private static void ExecuteCadWork(object state)",
]
for token in required:
    if token not in runtime:
        raise SystemExit(f"missing runtime application-context writer contract: {token}")

invoke_start = runtime.index("private static string InvokeCad(Func<string> action)")
invoke_end = runtime.index("private static void ExecuteCadWork(object state)", invoke_start)
invoke_body = runtime[invoke_start:invoke_end]

dispatch = "Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadWork, item);"
initial_wait = "if (!item.Done.Wait(CadDispatchTimeoutMilliseconds))"
cancel = "Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued)"
settle = "item.Done.Wait();"
disposal_try = "try\n            {"
dispose = "item.Done.Dispose();"

if dispatch not in invoke_body or initial_wait not in invoke_body or cancel not in invoke_body:
    raise SystemExit("runtime CAD dispatch must retain bounded initial wait plus atomic cancel-before-start")
if settle not in invoke_body:
    raise SystemExit("runtime timeout racing with an already-started CAD callback must retain writer ownership until that callback settles")
if disposal_try not in invoke_body or invoke_body.index(dispatch) < invoke_body.index(disposal_try):
    raise SystemExit("synchronous ExecuteInApplicationContext failure must remain inside the completion-handle disposal boundary")
if dispose not in invoke_body or invoke_body.index(dispose) < invoke_body.index(dispatch):
    raise SystemExit("completion handle must be disposed by the invoking caller after dispatch/wait processing")
if invoke_body.index(initial_wait) > invoke_body.index(cancel):
    raise SystemExit("cancel-before-start may only run after the bounded initial wait expires")
if invoke_body.index(cancel) > invoke_body.index(settle):
    raise SystemExit("already-started callback may settle only after cancel-before-start loses the atomic race")
if "Timed out after CAD work started" in invoke_body:
    raise SystemExit("runtime must not return/release the writer merely because an already-started CAD callback crossed the initial timeout")
if "item.Abandoned" in runtime or "public int Abandoned" in runtime:
    raise SystemExit("completion ownership must not use an abandoned handoff that can race callback completion")

execute_start = runtime.index("private static void ExecuteCadWork(object state)")
execute_end = runtime.index("private static string DescribeEntity", execute_start)
execute_body = runtime[execute_start:execute_end]
claim = "Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued)"
if execute_body.index(claim) > execute_body.index("item.Action()"):
    raise SystemExit("CAD callback must atomically claim execution before side-effecting work")
if "catch (ObjectDisposedException)" not in execute_body:
    raise SystemExit("cancel-before-start callback delivery must tolerate caller-owned completion-handle disposal")
if execute_body.index(claim) > execute_body.index("item.Done.Set()"):
    raise SystemExit("callback completion may only be signalled after execution-state admission")

print("PASS: MCP runtime application-context timeout retains writer ownership and safe completion-handle cleanup")
