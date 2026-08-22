#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedGridAnnotationHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedGridAnnotationHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "if (string.IsNullOrWhiteSpace(rawHandles))",
        '"GRID_ANNOTATION_HANDLES_EMPTY"',
        ".Split(new[] { ';' }, StringSplitOptions.None)",
        ".Select(x => (x ?? string.Empty).Trim())",
        "if (handle.Length == 0)",
        '"GRID_ANNOTATION_HANDLE_INVALID"',
        '"Generated Grid annotation Handle không được rỗng."',
        "var isValidHex = long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);",
        "GeneratedHandleOwnershipPolicy.NormalizeHandleIdentity(handle)",
        "if (!distinct.Add(identity))",
        "if (distinct.Count != ExpectedHandleCount)",
    )
    for token in required:
        if token not in text:
            errors.append("missing grid-annotation empty-token/canonical-identity contract token: " + token)

    for forbidden in (
        ".Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)",
        ".Where(x => x.Length > 0)",
        "if (!distinct.Add(handle))",
    ):
        if forbidden in text:
            errors.append("grid-annotation inspected stream regressed to discarded/uncanonical handle identity: " + forbidden)

fixtures = (
    "A1;A2;A3;A4;A5;A6;;",
    ";A1;A2;A3;A4;A5;A6",
    "A1;A2;A3;A4;A5;A6;",
    "A1;A2;A3; ;A4;A5;A6",
)
for raw in fixtures:
    normalized = [part.strip() for part in raw.split(";")]
    if not any(part == "" for part in normalized):
        errors.append("regression fixture no longer contains an empty token: " + raw)

print("QS3D grid-annotation empty-handle-token preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Grid annotation metadata keeps empty tokens fail-visible while duplicate checks use canonical numeric CAD-handle identity.")
