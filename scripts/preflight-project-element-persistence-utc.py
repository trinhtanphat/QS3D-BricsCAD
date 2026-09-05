#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ProjectElement.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.find("internal void RestorePersistenceState(ElementDirtyFlags dirty, DateTime updatedUtc)")
if start < 0:
    fail("ProjectElement.RestorePersistenceState is missing")
end = text.find("internal void TouchPersistenceState()", start)
if end < 0:
    fail("ProjectElement persistence restore boundary is missing")
block = text[start:end]

required = (
    "(dirty & ~ElementDirtyFlags.All) != 0",
    "updatedUtc.Kind != DateTimeKind.Utc",
    "throw new ArgumentException",
    "persistence timestamp must be UTC",
    "Dirty = dirty;",
    "UpdatedUtc = updatedUtc;",
)
for marker in required:
    if marker not in block:
        fail(f"ProjectElement persistence restore is missing deterministic UTC marker: {marker}")

if "ToUniversalTime()" in block:
    fail("ProjectElement persistence restore must not perform host-timezone-dependent conversion")

kind_pos = block.index("updatedUtc.Kind != DateTimeKind.Utc")
dirty_assign_pos = block.index("Dirty = dirty;")
time_assign_pos = block.index("UpdatedUtc = updatedUtc;")
if not (kind_pos < dirty_assign_pos < time_assign_pos):
    fail("UTC admission must complete before ProjectElement persistence state mutation")

print("PASS: ProjectElement persistence restore rejects host-timezone-dependent timestamps before mutation")
