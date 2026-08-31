#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CORE = ROOT / "src/QS3D.Core/Mep/MepRecognition.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepRecognitionSmoke.cs"
errors = []

for path in (CORE, SMOKE):
    if not path.is_file():
        errors.append("missing MEP recognition text-integrity file: " + str(path.relative_to(ROOT)))

if CORE.is_file():
    core = CORE.read_text(encoding="utf-8")
    for token in (
        "char.IsControl(character)",
        "char.IsHighSurrogate(character)",
        "!char.IsLowSurrogate(trimmed[i + 1])",
        "char.IsLowSurrogate(character)",
        '"Recognition text must contain well-formed UTF-16."',
    ):
        if token not in core:
            errors.append("Core MEP recognition source missing UTF-16 admission contract token: " + token)

    high = core.find("if (char.IsHighSurrogate(character))")
    paired = core.find("!char.IsLowSurrogate(trimmed[i + 1])", high)
    advance = core.find("i++;", paired)
    low = core.find("if (char.IsLowSurrogate(character))", advance)
    if min(high, paired, advance, low) < 0 or not (high < paired < advance < low):
        errors.append("recognition text validation must accept only paired high+low surrogates and reject stray lows")

if SMOKE.is_file():
    smoke = SMOKE.read_text(encoding="utf-8")
    for token in (
        "RecognitionTextIntegrity();",
        '"bad-id-\\ud800"',
        '"Structure\\udc00"',
        '"DUCT\\ud800X"',
        "char.ConvertFromUtf32(0x1F6A7)",
        '"supplementary rule id preservation"',
        '"supplementary recognition status"',
        '"well-formed UTF-16"',
    ):
        if token not in smoke:
            errors.append("MEP recognition smoke missing UTF-16 regression token: " + token)

print("QS3D MEP recognition text-integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: MEP recognition rule identity/category/token admission rejects malformed UTF-16 while preserving valid supplementary text.")
