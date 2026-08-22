#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSemanticTagHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedSemanticTagHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private static HashSet<string> ParseHandles",
        ".Split(new[] { ';' }, StringSplitOptions.None)",
        "if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))",
        '"SEMANTIC_TAG_HANDLE_INVALID"',
        '"SEMANTIC_TAG_HANDLE_DUPLICATE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing semantic-tag empty-token contract token: " + token)

    forbidden = ".Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)"
    if forbidden in text:
        errors.append("semantic-tag ParseHandles still removes empty tokens before validation")

for raw in ("AA;;BB", ";AA", "AA;", "AA; ;BB"):
    normalized = [part.strip() for part in raw.split(";")]
    if not any(part == "" for part in normalized):
        errors.append("regression fixture no longer contains an empty token: " + raw)

print("QS3D semantic-tag empty-handle-token preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: semantic-tag health preserves delimiter-empty tokens so malformed generated handle lists fail visible.")
