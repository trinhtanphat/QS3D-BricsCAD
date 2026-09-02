#!/usr/bin/env python3
"""Fail closed unless MCP tool structuredContent validates complete JSON containers."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP tool-result structured JSON preflight failed closed: {message}")
    raise SystemExit(1)


server = SERVER.read_text(encoding="utf-8")

success_start = server.find("private static string ToolSuccess(string jsonValue)")
success_end = server.find("private static bool LooksLikeJsonValue(string value)", success_start)
if success_start < 0 or success_end < 0:
    fail("tool-success response builder is missing")
tool_success = server[success_start:success_end]

start = server.find("private static bool LooksLikeJsonValue(string value)")
end = server.find("private static bool LooksLikeJsonObject(string value)", start)
if start < 0 or end < 0:
    fail("container compatibility validator is missing")
validator = server[start:end]

# Delimiter checks retain the historical contract: only objects/arrays are projected as
# structured JSON; scalar/plain output continues through the escaped text fallback.
for needle in ["first == '{'", "first == '['", "last == '}'", "last == ']'"]:
    if needle not in validator:
        fail(f"container-only compatibility check missing: {needle}")

# A delimiter heuristic alone is unsafe. Reuse the hardened MCP JSON scanner through a
# synthetic top-level property so malformed/trailing/trailing-comma containers fail closed.
if "McpTopLevelJson.TryFindPropertyValue" not in validator:
    fail("container candidate is not validated by the hardened complete JSON scanner")
if '"value"' not in validator or "found" not in validator or "error" not in validator:
    fail("complete-value scanner result is not checked fail-closed")
if "string.Equals(raw, trimmed, StringComparison.Ordinal)" not in validator:
    fail("validated JSON token must equal the complete trimmed candidate")

# Scope the wire-contract checks to ToolSuccess instead of matching across the entire
# source file. The C# string literal starts before structuredContent, so the stable source
# token is the escaped JSON field sequence itself, not an invented quote boundary at data.
if '\\"structuredContent\\":{\\"data\\":" + raw' not in tool_success:
    fail("structuredContent JSON-container projection contract drifted")
if "if (!LooksLikeJsonValue(raw))" not in tool_success:
    fail("tool-success path no longer validates structured JSON before projection")
if '\\"value\\":\\"' not in tool_success or "JsonEscape(raw)" not in tool_success:
    fail("invalid/non-container tool output no longer falls back to escaped JSON text")

print("MCP tool-result structured JSON preflight passed.")
sys.exit(0)
