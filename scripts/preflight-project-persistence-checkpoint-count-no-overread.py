#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectPersistenceCheckpoint.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.index("public static ProjectPersistenceCheckpoint Capture(")
end = text.index("public bool Matches(ProjectState project)", start)
block = text[start:end]

if "foreach (var rawId in elementIds)" in block:
    fail("persistence checkpoint still uses foreach and can over-read an underreported authoritative Count")

required = (
    "using var enumerator = elementIds.GetEnumerator();",
    "while (enumerator.MoveNext())",
    "expectedKnownCount.HasValue && observed >= expectedKnownCount.Value",
    "throw new InvalidOperationException(",
    "known element count",
    "var rawId = enumerator.Current;",
)
for marker in required:
    if marker not in block:
        fail(f"persistence checkpoint is missing fail-fast Count guard marker: {marker}")

guard = block.index("expectedKnownCount.HasValue && observed >= expectedKnownCount.Value")
current_read = block.index("var rawId = enumerator.Current;")
if guard > current_read:
    fail("authoritative checkpoint Count overrun must be rejected before enumerator.Current is read")

post_check = block.find("observed != expectedKnownCount.Value", current_read)
if post_check < 0:
    fail("persistence checkpoint must preserve the post-enumeration known-Count underrun check")

reread = block.find("RejectMalformedKnownCounts(elementIds)", post_check)
if reread < 0:
    fail("persistence checkpoint must re-read known Count after enumeration to detect generation drift")

print("PASS: persistence checkpoint fails closed before reading the first item beyond authoritative Count")
