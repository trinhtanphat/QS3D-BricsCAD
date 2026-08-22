#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Diagnostics/GeneratedSlabMeshHealthService.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing GeneratedSlabMeshHealthService source")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "foreach (var item in raw.Split(new[] { ';' }, StringSplitOptions.None))",
        "var handle = (item ?? string.Empty).Trim();",
        "if (handle.Length == 0 || !long.TryParse(handle, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))",
        '"INVALID_SLAB_MESH_GENERATED_HANDLE"',
    )
    for token in required:
        if token not in text:
            errors.append("missing slab-mesh empty-token contract token: " + token)

    forbidden = "foreach (var item in raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))"
    if forbidden in text:
        errors.append("slab-mesh validation still removes empty handle tokens before validation")

# Pin the malformed metadata shapes that motivated the source contract.  These are
# intentionally valid around the empty token so a matching count cannot hide it.
for raw in ("AA;;BB", ";AA", "AA;", "AA; ;BB"):
    normalized = [part.strip() for part in raw.split(";")]
    if not any(part == "" for part in normalized):
        errors.append("regression fixture no longer contains an empty token: " + raw)

print("QS3D slab-mesh empty-handle-token preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: slab-mesh health preserves delimiter-empty tokens so malformed generated handle lists fail visible.")
