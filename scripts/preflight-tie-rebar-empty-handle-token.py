#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedTieRebarHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "raw.Split(new[] { ';' }, StringSplitOptions.None)",
        ".Select(x => (x ?? string.Empty).Trim()).ToArray()",
        "if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))",
        '"INVALID_TIE_REBAR_GENERATED_HANDLE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing tie-rebar empty-token contract token: " + token)

    inspected_forbidden = "var handles = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();"
    if inspected_forbidden in text:
        errors.append("tie-rebar inspection still removes empty handle tokens before validation")

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

print("PASS: tie-rebar health preserves delimiter-empty tokens so malformed generated handle lists fail visible.")
