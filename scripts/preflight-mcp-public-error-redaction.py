#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
HELPER = ROOT / "src/QS3D.BricsCAD.V25/McpPublicTextSanitizer.cs"
FILES = [
    ROOT / "src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs",
    ROOT / "src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs",
]
errors = []

if not HELPER.exists():
    errors.append("missing shared McpPublicTextSanitizer")
else:
    helper = HELPER.read_text(encoding="utf-8-sig")
    for token in (
        "MaxPublicTextCharacters = 512",
        "Authorization",
        "[REDACTED]",
        "[PATH]",
        "char.IsControl(ch)",
    ):
        if token not in helper:
            errors.append("shared sanitizer missing contract token: " + token)

for path in FILES:
    text = path.read_text(encoding="utf-8-sig")
    name = path.name
    block = re.search(
        r"private static string SanitizePublicError\(string message\)\s*\{(?P<body>.*?)\n\s*\}",
        text,
        re.S,
    )
    if not block or "McpPublicTextSanitizer.Sanitize" not in block.group("body"):
        errors.append(f"{name}: public error wrapper is not delegated to shared sanitizer")
    for method in ("ToolError", "JsonRpcError"):
        method_block = re.search(
            rf"private static string {method}\([^{{]+\)\s*\{{(?P<body>.*?)\n\s*\}}",
            text,
            re.S,
        )
        if not method_block:
            errors.append(f"{name}: missing {method} implementation")
            continue
        if "SanitizePublicError" not in method_block.group("body"):
            errors.append(f"{name}: {method} serializes unsanitized public message")
    if "MaxPublicErrorCharacters" not in text:
        errors.append(f"{name}: missing compatibility public-error bound constant")
    if "JsonEscape(ex.Message)" in text:
        errors.append(f"{name}: direct exception message still crosses a public JSON boundary")
    if "JsonEscape(SanitizePublicError(ex.Message))" not in text:
        errors.append(f"{name}: HTTP/public exception boundary is not routed through sanitizer")
if errors:
    print("MCP public error redaction preflight FAILED")
    for error in errors:
        print(" - " + error)
    sys.exit(1)
print("MCP public error redaction preflight PASS")
