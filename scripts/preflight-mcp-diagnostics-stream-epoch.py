#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HUB = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDiagnosticHub.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDirectDiagnosticsThemeRuntime.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP diagnostics stream-epoch preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


hub = HUB.read_text(encoding="utf-8")
runtime = RUNTIME.read_text(encoding="utf-8")

hub_requirements = {
    "stream epoch state": "_streamEpoch",
    "fresh generation identity": 'Guid.NewGuid().ToString("N")',
    "epoch read surface": "StreamEpoch",
    "epoch on persisted events": 'streamEpoch',
}
for label, token in hub_requirements.items():
    if token not in hub:
        fail(f"hub is missing {label}: {token}")

runtime_requirements = {
    "epoch-bound cursor input": 'afterStreamEpoch',
    "current epoch binding": "McpDiagnosticHub.StreamEpoch",
    "cursor epoch validation": "RequireCursorEpoch",
    "explicit stale cursor reset": "cursorReset",
    "batch epoch output": "streamEpoch",
    "event epoch parser": "StreamEpochRegex",
}
for label, token in runtime_requirements.items():
    if token not in runtime:
        fail(f"direct diagnostics runtime is missing {label}: {token}")

if 'afterSequence > 0' not in runtime or 'afterStreamEpoch' not in runtime:
    fail("nonzero numeric cursors must be fail-closed unless bound to an epoch")
if "MaxWaitMilliseconds = 15000" not in runtime:
    fail("existing bounded diagnostics wait contract must remain 15 seconds")
if "MaxScannedEventsPerFile = 50000" not in runtime:
    fail("existing bounded canonical diagnostics scan contract must remain")
if "McpCadAgentRuntime.AuditFilePath" not in runtime or 'yield return path + ".1";' not in runtime:
    fail("diagnostics must stay limited to canonical current/rotated audit files")

for forbidden in (
    "Directory.GetFiles(",
    "Directory.EnumerateFiles(",
    "Process.Start(",
    "cmd.exe",
    "powershell",
):
    if forbidden.lower() in runtime.lower():
        fail(f"runtime contains forbidden broad/local execution surface: {forbidden}")

print("MCP diagnostics stream-epoch preflight passed.")
