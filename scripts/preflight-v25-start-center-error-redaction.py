#!/usr/bin/env python3
"""Guard Start Center user-facing failures against raw exception-detail leakage."""
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterWindow.cs"
text = SOURCE.read_text(encoding="utf-8")

failures = []
if re.search(r"\b(?:ex|exception)\.Message\b", text, re.IGNORECASE):
    failures.append("Start Center source exposes raw Exception.Message")
if re.search(r"\b(?:ex|exception)\.ToString\s*\(\s*\)", text, re.IGNORECASE):
    failures.append("Start Center source exposes raw exception ToString() detail")

required = (
    'ShowSafeFailure("Không thể làm mới Khởi đầu.',
    'ShowSafeFailure("Không thể mở dự án gần đây.',
    'ShowSafeFailure("Không thể hoàn tất thao tác.',
)
for marker in required:
    if marker not in text:
        failures.append(f"missing stable failure boundary: {marker}")

if "private void ShowSafeFailure(string message)" not in text:
    failures.append("missing centralized Start Center failure presenter")

if failures:
    for failure in failures:
        print(f"ERROR: {failure}", file=sys.stderr)
    raise SystemExit(1)

print("Start Center error-redaction preflight passed")
