#!/usr/bin/env python3
"""Fail closed if MCP emergency-control paths expose caught host/native exception text."""
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpCadAgentRuntime.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    '"cadContextError":\"redacted\"',
    '"Automation stopped, but ESC delivery failed."',
    '"CAD command cancellation failed after both bounded delivery paths."',
]
for token in required:
    if token not in text:
        print(f"ERROR: missing stable emergency-control error contract: {token}")
        sys.exit(1)

forbidden = [
    'Escape(ex.Message)',
    '"Automation stopped, but ESC delivery failed: " + ex.Message',
]
for token in forbidden:
    if token in text:
        print(f"ERROR: MCP emergency-control path exposes caught exception detail: {token}")
        sys.exit(1)

# Keep the guard narrow: emergency stop and cancel must retain both CAD-context and
# foreground fallback paths rather than "fixing" disclosure by deleting recovery.
for token in [
    'private static string EmergencyStop()',
    'private static string CancelCurrentCommand()',
    'TrySendEscapeFallback()',
    'delivery\\\":\\\"foreground-fallback',
]:
    if token not in text:
        print(f"ERROR: emergency-control recovery contract disappeared: {token}")
        sys.exit(1)

print("MCP CAD emergency-control error redaction preflight passed.")
