#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "AutoRoomLifecycle.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.find("public static void MarkActive(ProjectElement room, string sourceSignature)")
if start < 0:
    fail("AutoRoomLifecycle.MarkActive is missing")
end = text.find("public static int SyncFamilyDefaults", start)
if end < 0:
    fail("AutoRoomLifecycle.MarkActive boundary is missing")
block = text[start:end]

required = (
    "room.SetProperty(BoundarySourceSignatureKey, normalizedSourceSignature);",
    "room.SetProperty(BoundaryStateKey, BoundaryStateActive);",
    'room.RemoveProperty("BoundaryStaleUtc");',
    'room.RemoveProperty("BoundaryStaleReason");',
)
for marker in required:
    if marker not in block:
        fail(f"Auto Room activation bypasses ProjectElement property lifecycle: {marker}")

for forbidden in (
    "room.Properties[BoundaryStateKey] =",
    "room.Properties[BoundarySourceSignatureKey] =",
    'room.Properties.Remove("BoundaryStaleUtc")',
    'room.Properties.Remove("BoundaryStaleReason")',
):
    if forbidden in block:
        fail(f"Auto Room activation still performs raw persisted-property mutation: {forbidden}")

normalize_pos = block.find("NormalizeSourceHandleText(sourceSignature)")
signature_pos = block.find("room.SetProperty(BoundarySourceSignatureKey, normalizedSourceSignature);")
state_pos = block.find("room.SetProperty(BoundaryStateKey, BoundaryStateActive);")
if min(normalize_pos, signature_pos, state_pos) < 0 or not (normalize_pos < signature_pos < state_pos):
    fail("Auto Room source signature must be normalized and lifecycle-admitted before activation-state publication")

print("PASS: Auto Room activation uses ProjectElement persisted-property lifecycle APIs with failure-atomic admission ordering")
