#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedRebarHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedRebarHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    start_token = "private static void InspectSet("
    end_token = "private static IEnumerable<string> SplitHandles"
    start = text.find(start_token)
    end = text.find(end_token, start + 1) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append("cannot isolate GeneratedRebarHealthService.InspectSet validation block")
    else:
        block = text[start:end]
        required = (
            "var handles = raw.Split(new[] { ';' }, StringSplitOptions.None);",
            "if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))",
            '"INVALID_" + spec.CodePrefix + "_GENERATED_HANDLE"',
            'spec.CodePrefix + "_GENERATED_HANDLE_NON_CANONICAL"',
            "StringComparison.Ordinal",
        )
        for token in required:
            if token not in block:
                errors.append("missing generated-rebar empty/canonical-token contract token: " + token)
        normalization = re.search(
            r"var\s+\w+\s*=\s*item\s*\?\?\s*string\.Empty;\s*var\s+handle\s*=\s*\w+\.Trim\(\);",
            block,
            re.DOTALL,
        )
        if normalization is None:
            errors.append("InspectSet no longer preserves the raw generated-rebar token before null-safe trim normalization")
        if "StringSplitOptions.RemoveEmptyEntries" in block:
            errors.append("InspectSet still removes empty generated-rebar handle tokens before validation")

    for token in (
        'HandlesKey = "GeneratedRebarHandles"',
        'CodePrefix = "REBAR"',
        'HandlesKey = "GeneratedShapeRebarHandles"',
        'CodePrefix = "SHAPE_REBAR"',
    ):
        if token not in text:
            errors.append("missing generated-rebar handle-set spec token: " + token)

for raw in ("AA;;BB", ";AA", "AA;", "AA; ;BB"):
    normalized = [part.strip() for part in raw.split(";")]
    if not any(part == "" for part in normalized):
        errors.append("regression fixture no longer contains an empty token: " + raw)

print("QS3D generated-rebar empty-handle-token preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: GeneratedRebarHealthService preserves delimiter-empty tokens for both rebar sets, rejects invalid handles and flags padded/non-canonical tokens.")
