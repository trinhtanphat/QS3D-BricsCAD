#!/usr/bin/env python3
"""Fail closed unless V25 reference acquisition avoids recursive reuse of ExtractDir."""

from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SCRIPT = ROOT / "scripts" / "acquire-v25-compile-references.ps1"
text = SCRIPT.read_text(encoding="utf-8")

failures = []

if re.search(r"Remove-Item\s+-LiteralPath\s+\$extract\s+-Recurse", text, re.I):
    failures.append("ExtractDir must never be recursively deleted by pathname after a prior reparse sample")

absent_guard = re.search(
    r"if\s*\(Test-Path\s+-LiteralPath\s+\$extract\)\s*\{[^}]*throw[^}]*\}",
    text,
    re.I | re.S,
)
if not absent_guard:
    failures.append("existing ExtractDir must fail closed instead of being recursively reused")

create = re.search(r"New-Item\s+-ItemType\s+Directory\s+-Path\s+\$extract(?P<tail>[^\r\n]*)", text, re.I)
if not create:
    failures.append("ExtractDir must be created as a fresh directory")
elif "-Force" in create.group("tail"):
    failures.append("fresh ExtractDir creation must not use -Force because a raced-in path must fail")

if "ExtractDir unexpectedly already exists" not in text:
    failures.append("fail-closed existing/raced extraction root diagnostic is required")

if failures:
    for failure in failures:
        print(f"ERROR: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("PASS V25 reference extraction cleanup generation safety")
