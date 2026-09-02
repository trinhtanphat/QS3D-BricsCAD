#!/usr/bin/env python3
"""Fail closed unless MCP tool structuredContent validates complete JSON containers."""

from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVER = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpEmbeddedServerV2.cs"
JSON = ROOT / "src" / "QS3D.BricsCAD.V25" / "McpTopLevelJson.cs"


def fail(message: str) -> None:
    print(f"ERROR: MCP tool-result structured JSON preflight failed closed: {message}")
    raise SystemExit(1)


server = SERVER.read_text(encoding="utf-8")
json_source = JSON.read_text(encoding="utf-8")

required_parser_contract = [
    "internal static bool IsCompleteJsonValue(string json)",
    "TrySkipValue(source, ref index, out error)",
    "index == source.Length",
]
for needle in required_parser_contract:
    if needle not in json_source:
        fail(f"complete-value JSON validator contract missing: {needle}")

if "LooksLikeJsonValue(raw) && McpTopLevelJson.IsCompleteJsonValue(raw)" not in server:
    fail("ToolSuccess must validate the complete JSON value before raw structuredContent splice")

if '"\\\"data\\\":" + raw' not in server:
    fail("structuredContent JSON-container projection contract drifted")

# Keep compatibility explicit: scalar/plain output remains escaped text rather than being
# reclassified as structured JSON merely because the parser can validate a scalar.
if "private static bool LooksLikeJsonValue(string value)" not in server:
    fail("container-only compatibility discriminator is missing")

print("MCP tool-result structured JSON preflight passed.")
sys.exit(0)
