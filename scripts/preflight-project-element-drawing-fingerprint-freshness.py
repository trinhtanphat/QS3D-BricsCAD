#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectElement.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.index("public string DrawingFingerprint")
end = text.index("public IList<string> SourceHandles", start)
block = text[start:end]

required = (
    "var next = NormalizeDrawingFingerprint(value);",
    "string.Equals(_drawingFingerprint, next, StringComparison.Ordinal)",
    "_drawingFingerprint = next;",
    "MarkDirtyCore(ElementDirtyFlags.Relations, true);",
)
for marker in required:
    if marker not in block:
        fail(f"DrawingFingerprint setter is missing persistence-freshness marker: {marker}")

normalize = block.index("var next = NormalizeDrawingFingerprint(value);")
no_op = block.index("string.Equals(_drawingFingerprint, next, StringComparison.Ordinal)")
assign = block.index("_drawingFingerprint = next;")
mark = block.index("MarkDirtyCore(ElementDirtyFlags.Relations, true);")
if not (normalize < no_op < assign < mark):
    fail("DrawingFingerprint must validate/canonicalize, no-op identical values, assign, then mark relation state dirty")

if "set => _drawingFingerprint" in block:
    fail("DrawingFingerprint still uses the normalize-only expression setter")

print("PASS: drawing fingerprint changes advance element persistence freshness while canonical no-ops stay stable")
