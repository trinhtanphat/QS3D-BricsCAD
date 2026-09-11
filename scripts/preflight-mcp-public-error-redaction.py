#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
FILES = [
    ROOT / "src/QS3D.BricsCAD.V25/McpEmbeddedServer.cs",
    ROOT / "src/QS3D.BricsCAD.V25/McpEmbeddedServerV2.cs",
]
errors = []
for path in FILES:
    text = path.read_text(encoding="utf-8-sig")
    name = path.name
    if "SanitizePublicError" not in text:
        errors.append(f"{name}: missing bounded public-error sanitizer")
    for method in ("ToolError", "JsonRpcError"):
        block = re.search(rf"private static string {method}\([^{{]+\)\s*\{{(?P<body>.*?)\n\s*\}}", text, re.S)
        if not block:
            errors.append(f"{name}: missing {method} implementation")
            continue
        body = block.group("body")
        if "SanitizePublicError" not in body:
            errors.append(f"{name}: {method} serializes unsanitized public message")
    if "MaxPublicErrorCharacters" not in text:
        errors.append(f"{name}: missing public-error output bound")
    if "Authorization" not in text or "REDACTED" not in text:
        errors.append(f"{name}: missing bearer/secret redaction contract")
    if "JsonEscape(ex.Message)" in text:
        errors.append(f"{name}: direct exception message still crosses a public JSON boundary")
    if "JsonEscape(SanitizePublicError(ex.Message))" not in text:
        errors.append(f"{name}: HTTP/public exception boundary is not routed through sanitizer")
    if "char.IsControl(ch)" not in text:
        errors.append(f"{name}: missing control-character stripping")
    if "[PATH]" not in text:
        errors.append(f"{name}: missing local-path redaction")

if errors:
    print("MCP public error redaction preflight FAILED")
    for error in errors:
        print(" - " + error)
    sys.exit(1)
print("MCP public error redaction preflight PASS")
