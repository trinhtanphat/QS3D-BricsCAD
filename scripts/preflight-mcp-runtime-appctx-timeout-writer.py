#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
runtime = (ROOT / "src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs").read_text(encoding="utf-8")
coordinator = (ROOT / "src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs").read_text(encoding="utf-8")

required = [
    "private const int CadWorkQueued = 0;",
    "private const int CadWorkRunning = 1;",
    "private const int CadWorkCancelledBeforeStart = 2;",
    "private sealed class CadStartedTimeoutException : TimeoutException",
    "private static string InvokeCad(Func<string> action)",
    "private static void ExecuteCadWork(object state)",
]
for token in required:
    if token not in runtime:
        raise SystemExit(f"missing runtime application-context writer contract: {token}")

mutation_start = runtime.index("private static string Mutation(string body, string tool, Func<string> action)")
mutation_end = runtime.index("private static bool IsDurabilitySaveTool", mutation_start)
mutation_body = runtime[mutation_start:mutation_end]
for token in [
    "catch (CadStartedTimeoutException timeout)",
    "McpCadMutationCoordinator.DetachMutationForDeferredCompletion(writerScope)",
    "timeout.TransferWriterScope(deferredWriterScope)",
    "writerScope = null",
]:
    if token not in mutation_body:
        raise SystemExit(f"mutation timeout must transfer writer ownership before caller unwind: {token}")

invoke_start = runtime.index("private static string InvokeCad(Func<string> action)")
invoke_end = runtime.index("private static void ExecuteCadWork(object state)", invoke_start)
invoke_body = runtime[invoke_start:invoke_end]

dispatch = "Application.DocumentManager.ExecuteInApplicationContext(ExecuteCadWork, item);"
initial_wait = "if (!item.Done.Wait(CadDispatchTimeoutMilliseconds))"
cancel = "Interlocked.CompareExchange(ref item.DispatchState, CadWorkCancelledBeforeStart, CadWorkQueued)"
detach = "item.DetachAfterStartedTimeout()"
started_timeout = "throw new CadStartedTimeoutException(item);"

for token in (dispatch, initial_wait, cancel, detach, started_timeout):
    if token not in invoke_body:
        raise SystemExit(f"runtime CAD dispatch must retain bounded cancel/handoff contract: {token}")
if "item.Done.Wait();" in invoke_body:
    raise SystemExit("started CAD callback must not extend the request with an unbounded completion wait")
if invoke_body.index(initial_wait) > invoke_body.index(cancel):
    raise SystemExit("cancel-before-start may only run after the bounded initial wait expires")
if invoke_body.index(cancel) > invoke_body.index(detach):
    raise SystemExit("started-work handoff may only occur after cancel-before-start loses the race")
if invoke_body.index(detach) > invoke_body.index(started_timeout):
    raise SystemExit("completion ownership must be detached before the bounded timeout returns")
if "item.Abandoned" in runtime or "public int Abandoned" in runtime:
    raise SystemExit("completion ownership must not restore the racy abandoned-handle handoff")

work_start = runtime.index("private sealed class CadWorkItem")
work_end = runtime.index("private static string InvokeCadMutation", work_start)
work_body = runtime[work_start:work_end]
for token in [
    "DetachAfterStartedTimeout",
    "AttachWriterScope",
    "Complete",
    "DisposeCallerCompletionIfOwned",
]:
    if token not in work_body:
        raise SystemExit(f"CAD work item must own race-safe detached completion: {token}")

execute_start = runtime.index("private static void ExecuteCadWork(object state)")
execute_end = runtime.index("private static string DescribeEntity", execute_start)
execute_body = runtime[execute_start:execute_end]
claim = "Interlocked.CompareExchange(ref item.DispatchState, CadWorkRunning, CadWorkQueued)"
if claim not in execute_body or execute_body.index(claim) > execute_body.index("item.Action()"):
    raise SystemExit("CAD callback must atomically claim execution before side-effecting work")
if "item.Complete();" not in execute_body:
    raise SystemExit("CAD callback terminal path must publish through CadWorkItem.Complete")

for token in [
    "internal static IDisposable DetachMutationForDeferredCompletion(IDisposable mutationScope)",
    "DetachForDeferredCompletion",
    "private sealed class DeferredMutationRelease : IDisposable",
]:
    if token not in coordinator:
        raise SystemExit(f"mutation coordinator must support deferred terminal writer release: {token}")

print("PASS: MCP started CAD timeout is response-bounded while writer ownership remains quarantined to terminal completion")
