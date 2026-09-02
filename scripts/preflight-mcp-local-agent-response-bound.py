#!/usr/bin/env python3
"""Fail closed unless the local MCP client bounds HTTP response allocation and decoding."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpLocalAgentClient.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP local-agent response bound preflight failed closed: {message}")
    raise SystemExit(1)


source = SOURCE.read_text(encoding="utf-8")

for needle in [
    "private const int MaxResponseBytes =",
    "new UTF8Encoding(false, true)",
    "response.ContentLength > MaxResponseBytes",
    "ReadBoundedResponseBody(stream, response.ContentLength)",
    "private static string ReadBoundedResponseBody(Stream stream, long advertisedLength)",
    "totalBytes > MaxResponseBytes - read",
    "StrictUtf8.GetString",
    "Local MCP response exceeds the allowed size.",
]:
    if needle not in source:
        fail(f"missing response-size/UTF-8 safety contract: {needle}")

send_start = source.find("private static LocalHttpResult Send(")
normalize_start = source.find("private static string NormalizeBody(", send_start)
if send_start < 0 or normalize_start < 0:
    fail("Send/NormalizeBody boundary is missing")
send = source[send_start:normalize_start]

if "ReadToEnd()" in send:
    fail("Send still performs an unbounded response ReadToEnd")
if "new StreamReader" in send:
    fail("Send still delegates response admission to an unbounded text reader")

helper_start = source.find("private static string ReadBoundedResponseBody(")
helper_end = source.find("private static string NormalizeBody(", helper_start)
if helper_start < 0 or helper_end < 0:
    fail("bounded response reader helper is missing")
helper = source[helper_start:helper_end]

for needle in ["stream.Read(", "MemoryStream", "MaxResponseBytes", "StrictUtf8.GetString"]:
    if needle not in helper:
        fail(f"bounded response reader missing: {needle}")

# Both advertised and streaming bounds are required. Content-Length alone is bypassable
# by chunked/unknown-length responses; streaming-only checks allocate before rejecting
# obviously oversized fixed-length responses.
if source.count("Local MCP response exceeds the allowed size.") < 2:
    fail("oversize fixed-length and streaming responses are not both fail-closed")

print("MCP local-agent response bound preflight passed.")
sys.exit(0)
