#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
coord = (ROOT / "src/QS3D.BricsCAD.V25/McpCadMutationCoordinator.cs").read_text(encoding="utf-8")
runtime = (ROOT / "src/QS3D.BricsCAD.V25/McpCadAgentRuntime.cs").read_text(encoding="utf-8")

required_coord = [
    "internal static void ResetForServerStart()",
    "internal static void EmergencyStopPreservePending()",
    "if (_pending != null) DetachPendingLocked(_pending);",
    "CleanupExpiredLeaseLocked(DateTime.UtcNow);",
]
for token in required_coord:
    if token not in coord:
        raise SystemExit(f"missing emergency native-barrier contract: {token}")

if "McpCadMutationCoordinator.Reset();" in runtime:
    raise SystemExit("StopAutomation/server-start must not share the old unconditional coordinator Reset() path")
if "McpCadMutationCoordinator.ResetForServerStart();" not in runtime:
    raise SystemExit("server start must use the explicit full-reset path")
if "McpCadMutationCoordinator.EmergencyStopPreservePending();" not in runtime:
    raise SystemExit("emergency stop must preserve committed pending native-command ownership")

stop_start = runtime.index("public static void StopAutomation()")
stop_end = runtime.index("public static string Call", stop_start)
stop_body = runtime[stop_start:stop_end]
if "EmergencyStopPreservePending" not in stop_body:
    raise SystemExit("StopAutomation must preserve the pending native-command barrier")

resume_start = runtime.index("private static string ResumeAgent")
resume_end = runtime.index("private static string CancelCurrentCommand", resume_start)
resume_body = runtime[resume_start:resume_end]
if "McpCadMutationCoordinator" in resume_body and "Reset" in resume_body:
    raise SystemExit("ResumeAgent must never clear the pending native-command barrier")

print("PASS: MCP emergency stop preserves committed native-command writer barrier")
