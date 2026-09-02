#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"

text = SOURCE.read_text(encoding="utf-8")

for forbidden in (
    'SetLastError("socket: " + ex.Message)',
    'SetLastError("listener: " + ex.Message)',
    'SetLastError("request: " + ex.Message)',
):
    if forbidden in text:
        print(f"ERROR: raw transport exception detail reaches public LastError: {forbidden}")
        sys.exit(1)

if "SetLastError(" in text:
    print("ERROR: legacy free-form LastError setter remains in embedded MCP transport")
    sys.exit(1)

required = (
    "SetLastTransportError(TransportErrorKind.Socket, ex)",
    "SetLastTransportError(TransportErrorKind.Listener, ex)",
    "SetLastTransportError(TransportErrorKind.Request, ex)",
    "private enum TransportErrorKind",
    "private static void SetLastTransportError",
    'code = "listener:error"',
    'code = "request:error"',
    '"socket:" + socket.SocketErrorCode.ToString()',
    "lock (Sync) _lastError = code;",
)
for marker in required:
    if marker not in text:
        print(f"ERROR: missing MCP transport error-redaction contract marker: {marker}")
        sys.exit(1)

helper_start = text.find("private static void SetLastTransportError")
helper_end = text.find("private sealed class SessionState", helper_start)
if helper_start < 0 or helper_end < 0:
    print("ERROR: could not isolate MCP transport error-redaction helper")
    sys.exit(1)
helper = text[helper_start:helper_end]
for forbidden in ("exception.Message", "exception.ToString()"):
    if forbidden in helper:
        print(f"ERROR: transport error helper exposes raw exception detail: {forbidden}")
        sys.exit(1)

print("PASS: embedded MCP transport errors are classified before reaching public LastError")
