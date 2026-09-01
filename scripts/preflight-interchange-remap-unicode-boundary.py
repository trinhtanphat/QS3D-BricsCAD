#!/usr/bin/env python3
from pathlib import Path
import sys

root = Path(__file__).resolve().parents[1]
planner = root / "src/QS3D.Core/Export/ProjectInterchangeRemapPlanner.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/ProjectInterchangeRemapUnicodeBoundarySmoke.cs"
errors = []

planner_text = planner.read_text(encoding="utf-8") if planner.exists() else ""
smoke_text = smoke.read_text(encoding="utf-8") if smoke.exists() else ""

required_planner = [
    "HasWellFormedUtf16(source)",
    "HasWellFormedUtf16(suffix)",
    "char.IsHighSurrogate(source[keep - 1])",
    "char.IsLowSurrogate(source[keep])",
    "keep--;",
    "Remap identity/name contains malformed UTF-16."
]
for token in required_planner:
    if token not in planner_text:
        errors.append(f"planner missing Unicode boundary contract token: {token}")

required_smoke = [
    "ZoneIdBoundaryPreservesSupplementaryScalar",
    "FamilyNameBoundaryPreservesSupplementaryScalar",
    "BmpBoundaryRemainsStable",
    "MalformedUtf16FailsClosed",
    "ModuleInitializer"
]
for token in required_smoke:
    if token not in smoke_text:
        errors.append(f"smoke missing deterministic Unicode boundary coverage token: {token}")

if "Substring(0, keep).TrimEnd() + suffix" not in planner_text:
    errors.append("planner no longer preserves bounded append publication shape")

if errors:
    print("Interchange remap Unicode boundary preflight FAILED:")
    for error in errors:
        print(f" - {error}")
    sys.exit(1)

print("PASS interchange remap Unicode scalar boundary contract")
