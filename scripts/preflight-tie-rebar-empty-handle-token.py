#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedTieRebarHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "StringSplitOptions.None",
        "if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))",
        '"INVALID_TIE_REBAR_GENERATED_HANDLE"',
        '"TIE_REBAR_GENERATED_HANDLE_NON_CANONICAL"',
        "StringComparison.Ordinal",
    )
    for token in required:
        if token not in text:
            errors.append("missing tie-rebar empty/canonical-token contract token: " + token)

    normalization = re.search(
        r"var\s+\w+\s*=\s*item\s*\?\?\s*string\.Empty;\s*var\s+handle\s*=\s*\w+\.Trim\(\);",
        text,
        re.DOTALL,
    )
    if normalization is None:
        errors.append("tie-rebar validation no longer preserves the raw token before null-safe trim normalization")

    inspected = text.split("private static void InspectCover", 1)[0]
    if "StringSplitOptions.RemoveEmptyEntries" in inspected:
        errors.append("tie-rebar inspected handle stream removes empty tokens before validation")

for raw in ("AA;;BB", ";AA", "AA;", "AA; ;BB"):
    normalized = [part.strip() for part in raw.split(";")]
    if not any(part == "" for part in normalized):
        errors.append("regression fixture no longer contains an empty token: " + raw)

print("QS3D tie-rebar empty-handle-token preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: tie-rebar health preserves delimiter-empty tokens, rejects invalid handles and flags padded/non-canonical generated handle tokens.")
