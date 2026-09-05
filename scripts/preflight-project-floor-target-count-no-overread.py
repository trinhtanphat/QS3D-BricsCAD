#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectFloorService.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.index("private static IReadOnlyList<ProjectElement> ResolveOwnedElements(")
end = text.index("private static int? SnapshotKnownTargetCount", start)
block = text[start:end]

legacy = "if (knownTargetCount.HasValue && observed > knownTargetCount.Value)\n                        continue;"
if legacy in block:
    fail("floor mutation target traversal still continues after authoritative Count is exceeded")

required = (
    "knownTargetCount.HasValue && observed > knownTargetCount.Value",
    "throw new InvalidOperationException(",
    "known count",
)
for marker in required:
    if marker not in block:
        fail(f"floor mutation target traversal is missing fail-fast Count guard marker: {marker}")

count_guard = block.index("knownTargetCount.HasValue && observed > knownTargetCount.Value")
current_read = block.index("var element = enumerator.Current;")
if count_guard > current_read:
    fail("authoritative Count overrun must be rejected before enumerator.Current is read")

print("PASS: floor mutation targets fail closed on the first item beyond authoritative Count")
