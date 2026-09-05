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
    "ValidateCapturedReferenceGeneration(",
    "capturedZones",
    "capturedFloors",
    "capturedFamilies",
    "capturedElements",
    "new ProjectStateSnapshot(",
    "detached,",
)
for marker in required:
    if marker not in block:
        fail(f"snapshot capture is missing generation-fence marker: {marker}")

copy_pos = block.index("var detached = CreateDetachedCopy(project);")
fence_pos = block.index("ValidateCapturedReferenceGeneration(")
publish_pos = block.index("new ProjectStateSnapshot(")
if not (copy_pos < fence_pos < publish_pos):
    fail("snapshot generation fence must run after detached-copy materialization and before snapshot publication")

helper_start = text.find("private static void ValidateCapturedReferenceGeneration(")
if helper_start < 0:
    fail("snapshot capture is missing the captured-reference generation validator")
helper_end = text.find("private static", helper_start + len("private static void ValidateCapturedReferenceGeneration("))
helper = text[helper_start:helper_end if helper_end >= 0 else len(text)]

helper_markers = (
    "ReferenceEquals(",
    "capturedZones",
    "capturedFloors",
    "capturedFamilies",
    "capturedElements",
    "project.Zones.Count",
    "project.Floors.Count",
    "project.Families.Count",
    "project.Elements.Count",
    "InvalidOperationException",
)
for marker in helper_markers:
    if marker not in helper:
        fail(f"snapshot generation validator is missing fail-closed identity marker: {marker}")

print("PASS: project snapshot capture fences exact collection generations before publication")
