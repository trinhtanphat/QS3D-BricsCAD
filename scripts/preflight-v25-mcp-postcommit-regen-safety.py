#!/usr/bin/env python3
"""Fail-closed guard for issue #6530 MCP/application-context post-commit Regen safety."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
TARGET = ROOT / "src" / "QS3D.BricsCAD.V25" / "Cad" / "CadPostCommitUi.cs"
text = TARGET.read_text(encoding="utf-8")
errors = []

match = re.search(
    r"public static void TryRegen\(Document document, string operation\)\s*\{(?P<body>.*?)\n        \}",
    text,
    re.DOTALL,
)
if not match:
    errors.append("TryRegen: unable to isolate post-commit refresh helper")
else:
    body = match.group("body")
    guard = "if (Application.DocumentManager.IsApplicationContext) return;"
    regen = "document.Editor.Regen();"
    if guard not in body:
        errors.append("TryRegen: missing application-context guard before native Editor.Regen")
    if regen not in body:
        errors.append("TryRegen: expected synchronous command-context Editor.Regen is missing")
    if guard in body and regen in body and body.find(guard) > body.find(regen):
        errors.append("TryRegen: application-context guard must execute before Editor.Regen")

if errors:
    print("V25 MCP post-commit Regen safety preflight FAILED:")
    for error in errors:
        print(" -", error)
    sys.exit(1)

print("PASS: post-commit viewport Regen is suppressed in BricsCAD application context and retained for command context.")
print("NOTE: licensed MCP Single Footing crash verification remains LOCAL_ONLY.")
