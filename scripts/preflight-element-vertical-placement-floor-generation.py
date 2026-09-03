#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Domain" / "ElementVerticalPlacementService.cs"


def fail(message: str) -> None:
    print(f"FAIL: {message}")
    raise SystemExit(1)


text = SOURCE.read_text(encoding="utf-8")

required = (
    "CaptureFloorGeneration(project)",
    "FindCapturedFloor(",
    "project.ChangeVersion",
    "project.Floors.Count",
    "StringComparer.OrdinalIgnoreCase",
)
for marker in required:
    if marker not in text:
        fail(f"vertical placement is missing floor-generation fence marker: {marker}")

if "ValidateFloorIdentityCollection(project);" in text:
    fail("vertical placement still validates the live floor catalog separately from level lookup")
if "FindFloor(project," in text:
    fail("vertical placement still performs live ProjectState floor lookup after validation")

print("PASS: vertical placement resolves levels from one fenced floor generation")
