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
    "while (true)",
    "var movedNext = enumerator.MoveNext();",
    "expectedKnownCount.HasValue && observed >= expectedKnownCount.Value",
    "throw new InvalidOperationException(",
    "known element count",
    "var rawId = enumerator.Current;",
    "RequireStableKnownCount(elementIds, expectedKnownCount.Value);",
)
for marker in required:
    if marker not in block:
        fail(f"persistence checkpoint is missing fail-fast Count guard marker: {marker}")

move = block.index("var movedNext = enumerator.MoveNext();")
guard = block.index("expectedKnownCount.HasValue && observed >= expectedKnownCount.Value", move)
current_read = block.index("var rawId = enumerator.Current;", guard)
if not (move < guard < current_read):
    fail("authoritative checkpoint Count overrun must be rejected after MoveNext and before enumerator.Current is read")

between_move_and_current = block[move:current_read]
if "RequireStableKnownCount(elementIds, expectedKnownCount.Value);" not in between_move_and_current:
    fail("checkpoint Count must be re-admitted after MoveNext before Current is read")

post_check = block.find("observed != expectedKnownCount.Value", current_read)
if post_check < 0:
    fail("persistence checkpoint must preserve the post-enumeration known-Count underrun check")

helper_start = text.find("private static void RequireStableKnownCount(", end)
if helper_start < 0:
    fail("persistence checkpoint must preserve a stable known-Count helper")
helper_end = text.find("private static int? RejectMalformedKnownCounts", helper_start)
if helper_end < 0:
    fail("persistence checkpoint known-Count helper boundary is missing")
helper = text[helper_start:helper_end]
if "RejectMalformedKnownCounts(elementIds)" not in helper:
    fail("stable known-Count helper must re-read authoritative Count evidence")

print("PASS: persistence checkpoint fails closed before Current and re-admits Count across traversal")
