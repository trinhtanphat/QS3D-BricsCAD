#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/QuantityBltPresetCommands.cs"
CATALOG = ROOT / "src/QS3D.BricsCAD.V25/Services/StartCenterCommandCatalog.cs"
DOCS = ROOT / "docs/COMMANDS.md"
errors = []

if not COMMAND.is_file():
    errors.append("missing QuantityBltPresetCommands.cs")
else:
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DSETUPBLT", CommandFlags.Modal)]',
        "window.LoadBltPresetOnOpen();",
    ):
        if token not in text:
            errors.append("BLT preset command missing staging token: " + token)

if not CATALOG.is_file():
    errors.append("missing StartCenterCommandCatalog.cs")
else:
    text = CATALOG.read_text(encoding="utf-8")
    if text.count('"QS3DSETUPBLT"') != 1:
        errors.append("Start Center must register QS3DSETUPBLT exactly once.")
    for token in (
        '"Preset BLT3D tính toán"',
        '"Khối lượng"',
        "chỉ lưu khi bấm Lưu Cài Đặt",
    ):
        if token not in text:
            errors.append("Start Center BLT preset entry missing discoverability token: " + token)

if not DOCS.is_file():
    errors.append("missing docs/COMMANDS.md")
else:
    text = DOCS.read_text(encoding="utf-8")
    if text.count("`QS3DSETUPBLT`") != 1:
        errors.append("docs/COMMANDS.md must document QS3DSETUPBLT exactly once.")
    for token in (
        "staged as a reviewable draft",
        "native quantity defaults remain unchanged",
    ):
        if token not in text:
            errors.append("QS3DSETUPBLT documentation missing safety token: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DSETUPBLT remains a staged BLT3D preset command and is discoverable exactly once in Start Center and the canonical command reference without changing native defaults.")
