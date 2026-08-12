#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedCurtainPanelHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedCurtainPanelHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "foreach (var token in (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.None))",
        "var handleText = token ?? string.Empty;",
        "var handle = handleText.Trim();",
        "if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))",
        '"INVALID_CURTAIN_PANEL_GENERATED_HANDLE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing curtain-panel empty-token contract token: " + token)

    forbidden = "foreach (var token in (raw ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))"
    if forbidden in text:
        errors.append("curtain-panel validation still removes empty handle tokens before validation")

for raw in ("AA;;BB", ";AA", "AA;", "AA; ;BB"):
    normalized = [part.strip() for part in raw.split(";")]
    if not any(part == "" for part in normalized):
        errors.append("regression fixture no longer contains an empty token: " + raw)

print("QS3D curtain-panel empty-handle-token preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: curtain-panel health preserves delimiter-empty tokens so malformed generated handle lists fail visible.")
