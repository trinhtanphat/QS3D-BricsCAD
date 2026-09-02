#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
coord = (ROOT / "src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs").read_text(encoding="utf-8")
runtime = (ROOT / "src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs").read_text(encoding="utf-8")

required_coord = [
    "reservation.BeginDispatch();",
    "if (_pending != null && !_pending.Dispatching)",
    "public bool Dispatching { get; set; }",
    "internal void BeginDispatch()",
    "_pending.Dispatching = true;",
]
for token in required_coord:
    if token not in coord:
        raise SystemExit(f"missing emergency native-barrier contract: {token}")

reset_start = coord.index("internal static void Reset()")
reset_end = coord.index("private static NativeCommandReservation ArmNativeCommandInCadContext", reset_start)
reset_body = coord[reset_start:reset_end]
if "DetachPendingLocked(_pending)" not in reset_body or "_pending = null;" not in reset_body:
    raise SystemExit("reset must still clean pre-dispatch reservations")
if "McpCadAgentRuntime.AutomationStopped" in reset_body:
    raise SystemExit("coordinator reset must use native-command lifecycle state, not runtime stop-state coupling")

queue_start = coord.index("internal static void QueueNativeCommand")
queue_end = coord.index("internal static string StatusJson", queue_start)
queue_body = coord[queue_start:queue_end]
if queue_body.index("reservation.BeginDispatch();") > queue_body.index("enqueue();"):
    raise SystemExit("native-command barrier must become durable before enqueue")

resume_start = runtime.index("private static string ResumeAgent")
resume_end = runtime.index("private static string CancelCurrentCommand", resume_start)
resume_body = runtime[resume_start:resume_end]
if "McpCadMutationCoordinator.Reset" in resume_body:
    raise SystemExit("ResumeAgent must never clear the pending native-command barrier")

print("PASS: MCP emergency stop preserves dispatching native-command writer barrier")
