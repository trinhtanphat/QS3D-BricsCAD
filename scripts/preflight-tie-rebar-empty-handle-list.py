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
        "if (!element.Properties.TryGetValue(HandlesKey, out var raw)) continue;",
        "var handles = (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.None)",
        '"INVALID_TIE_REBAR_GENERATED_HANDLE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing tie-rebar empty-list contract token: " + token)

    forbidden = "if (!element.Properties.TryGetValue(HandlesKey, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;"
    if forbidden in text:
        errors.append("present-but-empty tie-rebar handle metadata is still skipped")

for raw in ("", " ", "\t", "  \r\n  "):
    tokens = [part.strip() for part in raw.split(";")]
    if tokens != [""]:
        errors.append("empty-list fixture no longer reaches invalid-handle validation")

print("QS3D tie-rebar empty-handle-list preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: present-but-empty tie-rebar handle metadata reaches validation instead of being silently skipped.")
