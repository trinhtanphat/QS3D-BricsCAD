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
    "RequireEquivalentDetachedState(detached, project);",
    "ValidateCapturedReferenceGeneration(",
    "new ProjectStateSnapshot(",
)
for marker in required:
    if marker not in block:
        fail(f"snapshot capture is missing nested-stability marker: {marker}")

copy_pos = block.index("var detached = CreateDetachedCopy(project);")
first_equivalence = block.index("RequireEquivalentDetachedState(detached, project);")
reference_fence = block.index("ValidateCapturedReferenceGeneration(")
second_equivalence = block.index("RequireEquivalentDetachedState(detached, project);", first_equivalence + 1)
publish = block.index("new ProjectStateSnapshot(")
if not (copy_pos < first_equivalence < reference_fence < second_equivalence < publish):
    fail("snapshot nested-state verification must fence the exact-reference check on both sides before publication")
if block.count("CreateDetachedCopy(project)") != 1:
    fail("snapshot nested-state stability must not allocate a second full detached project")

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
    "SourceHandles",
    "DependsOn",
    "Properties",
    "Quantities",
    "AuditEvents",
    "RequireEquivalentMap(",
    "RequireEquivalentDoubleMap(",
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

double_map_start = text.find("private static void RequireEquivalentDoubleMap(")
if double_map_start < 0:
    fail("snapshot nested-state validation is missing quantity-map equivalence")
double_map_end = text.find("private static", double_map_start + len("private static void RequireEquivalentDoubleMap("))
double_map_helper = text[double_map_start:double_map_end if double_map_end >= 0 else len(text)]
for marker in ("Count", "TryGetValue", "Equals", "InvalidOperationException"):
    if marker not in double_map_helper:
        fail(f"snapshot quantity-map equivalence is missing marker: {marker}")

sequence_signature = "private static void RequireEquivalentSequence<T>("
sequence_start = text.find(sequence_signature)
if sequence_start < 0:
    fail("snapshot nested-state validation is missing ordered-sequence equivalence")
sequence_end = text.find("private static", sequence_start + len(sequence_signature))
sequence_helper = text[sequence_start:sequence_end if sequence_end >= 0 else len(text)]
for marker in ("Count", "for", "InvalidOperationException"):
    if marker not in sequence_helper:
        fail(f"snapshot sequence equivalence is missing marker: {marker}")

print("PASS: project snapshot capture rejects mixed-time nested persisted state before publication without a second full clone")
