#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
coord = (ROOT / "src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs").read_text(encoding="utf-8")
runtime = (ROOT / "src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs").read_text(encoding="utf-8")

reset_start = coord.index("internal static void Reset()")
reset_end = coord.index("private static NativeCommandReservation ArmNativeCommandInCadContext", reset_start)
reset_body = coord[reset_start:reset_end]

required_reset = [
    "McpCadAgentRuntime.AutomationStopped",
    "if (!preservePending && _pending != null)",
    "if (!preservePending) _pending = null;",
    "_lease = null;",
    "CurrentOperationId.Value = null;",
]
for token in required_reset:
    if token not in reset_body:
        raise SystemExit(f"missing emergency native-barrier reset contract: {token}")

if "DetachPendingLocked(_pending)" not in reset_body:
    raise SystemExit("full server-start reset must still detach stale pending handlers")

stop_start = runtime.index("public static void StopAutomation()")
stop_end = runtime.index("public static string Call", stop_start)
stop_body = runtime[stop_start:stop_end]
if "_automationStopped = true;" not in stop_body or "McpCadMutationCoordinator.Reset();" not in stop_body:
    raise SystemExit("StopAutomation must mark stopped before invoking coordinator reset policy")

start_start = runtime.index("public static void ResetForServerStart()")
start_end = runtime.index("public static void StopAutomation()", start_start)
start_body = runtime[start_start:start_end]
if "_automationStopped = false;" not in start_body or "McpCadMutationCoordinator.Reset();" not in start_body:
    raise SystemExit("server start must mark running before invoking full coordinator reset policy")

resume_start = runtime.index("private static string ResumeAgent")
resume_end = runtime.index("private static string CancelCurrentCommand", resume_start)
resume_body = runtime[resume_start:resume_end]
if "McpCadMutationCoordinator.Reset" in resume_body:
    raise SystemExit("ResumeAgent must never clear the preserved pending native-command barrier")

print("PASS: MCP emergency stop preserves committed native-command writer barrier")
