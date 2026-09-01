#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpDirectDiagnosticsThemeRuntime.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP diagnostics stream-epoch preflight failed: {message}", file=sys.stderr)
    raise SystemExit(1)


runtime = RUNTIME.read_text(encoding="utf-8")

requirements = {
    "privacy-safe generation identity": 'Guid.NewGuid().ToString("N")',
    "stream epoch state": "StreamEpoch",
    "epoch-bound cursor input": 'afterStreamEpoch',
    "cursor epoch validation": "RequireCursorEpoch",
    "explicit stale cursor reset": "cursorReset",
    "batch epoch output": 'streamEpoch',
    "cursor object": 'cursor',
}
for label, token in requirements.items():
    if token not in runtime:
        fail(f"direct diagnostics runtime is missing {label}: {token}")

if 'afterSequence > 0' not in runtime or 'afterStreamEpoch' not in runtime:
    fail("nonzero numeric cursors must be fail-closed unless bound to an epoch")
if "StringComparison.OrdinalIgnoreCase" not in runtime:
    fail("cursor epoch comparison must be case-insensitive for canonical hex input")
if "MaxWaitMilliseconds = 7000" not in runtime:
    fail("bounded diagnostics wait contract must remain below the edge deadline at 7000 ms")
if "MaxScannedEventsPerFile = 50000" not in runtime:
    fail("existing bounded canonical diagnostics scan contract must remain")
if "McpCadAgentRuntime.AuditFilePath" not in runtime or 'yield return path + ".1";' not in runtime:
    fail("diagnostics must stay limited to canonical current/rotated audit files")
if 'latestSequence' not in runtime:
    fail("existing latestSequence compatibility field must remain")

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
