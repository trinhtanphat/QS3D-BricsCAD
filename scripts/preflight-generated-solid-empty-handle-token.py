#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Cad/GeneratedSolidRuntimeHealthService.cs"
errors = []

if not SERVICE.is_file():
    errors.append("missing generated-solid runtime health service: " + str(SERVICE.relative_to(ROOT)))
else:
    text = SERVICE.read_text(encoding="utf-8")
    start = text.find("private static IReadOnlyList<ModelHealthIssue> InspectGeneratedSolidOwnership")
    end = text.find("private static void AddProviderSafely", start if start >= 0 else 0)
    if start < 0 or end < 0 or end <= start:
        errors.append("could not isolate InspectGeneratedSolidOwnership")
    else:
        ownership = text[start:end]
        old_skip = "if (!element.Properties.TryGetValue(HandleKey, out var rawHandle) || string.IsNullOrWhiteSpace(rawHandle)) continue;"
        if old_skip in ownership:
            errors.append("present empty GeneratedSolidHandle must not be silently treated as missing")

        required = (
            "if (!element.Properties.TryGetValue(HandleKey, out var rawHandle)) continue;",
            "var handle = (rawHandle ?? string.Empty).Trim();",
            '"GENERATED_SOLID_HANDLE_INVALID"',
            "if (!long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))",
        )
        for token in required:
            if token not in ownership:
                errors.append("generated-solid empty-token contract missing source token: " + token)

        if "string.IsNullOrWhiteSpace(rawHandle)" in ownership:
            errors.append("generated-solid ownership inspection must route present whitespace tokens through invalid-handle diagnostics")

print("QS3D generated-solid empty-handle-token preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: missing GeneratedSolidHandle remains absent while present null/empty/whitespace tokens reach GENERATED_SOLID_HANDLE_INVALID.")
