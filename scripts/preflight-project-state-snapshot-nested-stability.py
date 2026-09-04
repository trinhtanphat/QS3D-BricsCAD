#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectStateSnapshot.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")
start = text.index("public static ProjectStateSnapshot Capture(ProjectState project)")
end = text.index("public static ProjectState CreateDetachedCopy(ProjectState project)", start)
block = text[start:end]

required = (
    "var detached = CreateDetachedCopy(project);",
    "var verification = CreateDetachedCopy(project);",
    "RequireEquivalentDetachedState(detached, verification);",
    "ValidateCapturedReferenceGeneration(",
    "new ProjectStateSnapshot(",
)
for marker in required:
    if marker not in block:
        fail(f"snapshot capture is missing nested-stability marker: {marker}")

first_copy = block.index("var detached = CreateDetachedCopy(project);")
second_copy = block.index("var verification = CreateDetachedCopy(project);")
equivalence = block.index("RequireEquivalentDetachedState(detached, verification);")
reference_fence = block.index("ValidateCapturedReferenceGeneration(")
publish = block.index("new ProjectStateSnapshot(")
if not (first_copy < second_copy < equivalence < reference_fence < publish):
    fail("snapshot nested-state verification must compare two detached materializations before generation validation and publication")

helper_start = text.find("private static void RequireEquivalentDetachedState(")
if helper_start < 0:
    fail("snapshot capture is missing the detached-state equivalence validator")
helper_end = text.find("private static", helper_start + len("private static void RequireEquivalentDetachedState("))
helper = text[helper_start:helper_end if helper_end >= 0 else len(text)]

helper_markers = (
    "SchemaVersion",
    "ProjectId",
    "Name",
    "DrawingPath",
    "DrawingFingerprint",
    "ActiveZoneId",
    "ActiveFloorId",
    "UpdatedUtc",
    "ChangeVersion",
    "Metadata",
    "Zones",
    "Floors",
    "Families",
    "QuantityRules",
    "Elements",
    "AuditEvents",
    "RequireEquivalentMap(",
    "RequireEquivalentSequence(",
    "InvalidOperationException",
)
for marker in helper_markers:
    if marker not in helper:
        fail(f"snapshot nested-state equivalence validator is missing persisted-state marker: {marker}")

map_start = text.find("private static void RequireEquivalentMap(")
if map_start < 0:
    fail("snapshot nested-state validation is missing map key/value equivalence")
map_end = text.find("private static", map_start + len("private static void RequireEquivalentMap("))
map_helper = text[map_start:map_end if map_end >= 0 else len(text)]
for marker in ("Count", "TryGetValue", "StringComparison.Ordinal", "InvalidOperationException"):
    if marker not in map_helper:
        fail(f"snapshot map equivalence is missing marker: {marker}")

sequence_start = text.find("private static void RequireEquivalentSequence(")
if sequence_start < 0:
    fail("snapshot nested-state validation is missing ordered-sequence equivalence")
sequence_end = text.find("private static", sequence_start + len("private static void RequireEquivalentSequence("))
sequence_helper = text[sequence_start:sequence_end if sequence_end >= 0 else len(text)]
for marker in ("Count", "for", "InvalidOperationException"):
    if marker not in sequence_helper:
        fail(f"snapshot sequence equivalence is missing marker: {marker}")

print("PASS: project snapshot capture rejects mixed-time nested persisted state before publication")
