#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RightPanel.xaml.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        'AliasContains("hiện visible on", token)',
        'AliasContains("ẩn hidden off", token)',
        'AliasContains("khóa locked lock", token)',
        'AliasContains("mở unlocked unlock", token)',
        "aliases.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)",
        "string.Equals(alias, token, StringComparison.CurrentCultureIgnoreCase)",
    )
    for token in required:
        if token not in text:
            errors.append("layer status filter missing exact-alias token: " + token)
    if "aliases.IndexOf(token" in text:
        errors.append("layer status aliases must not use substring matching because lock/locked overlap unlocked")

print("QS3D layer status alias filter preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: visible/hidden and locked/unlocked aliases use exact token matching without overlapping status substrings.")
