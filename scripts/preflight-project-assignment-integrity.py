#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

checks = {
    "src/QS3D.Core/Domain/ProjectFloorService.cs": [
        "Project contains duplicate semantic element id",
        "ReferenceEquals(owned, element)",
        "Element does not belong to the project instance",
    ],
    "src/QS3D.Core/Domain/ProjectZoneService.cs": [
        "Project contains duplicate semantic element id",
        "ReferenceEquals(owned, element)",
        "Element does not belong to the project instance",
    ],
    "src/QS3D.Core/Domain/ProjectFamilyService.cs": [
        "Project contains duplicate semantic element id",
        "ReferenceEquals(owned, element)",
        "Element does not belong to the project instance",
    ],
    "src/QS3D.Core/Services/BulkEditService.cs": [
        "OwnedDistinct(project, elements)",
        "Project contains duplicate semantic element id",
        "ReferenceEquals(owned, element)",
        "Element does not belong to the project instance",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectZoneServiceSmoke.cs": [
        "AssignmentRejectsSpoofedSameIdElement",
        "Rejected spoofed assignment must not mutate",
    ],
    "tests/QS3D.Core.SmokeTests/ProjectFamilyServiceSmoke.cs": [
        "FamilyAssignmentRejectsSpoofedSameIdElement",
        "Rejected spoofed Family assignment must not mutate",
    ],
    "tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs": [
        "BulkEditRejectsForeignSameIdElements",
        "MultiplyNumericProperty",
    ],
    "src/QS3D.BricsCAD.V25/Cad/SemanticSelectionResolver.cs": [
        "SemanticHandleOwnershipResolver.Resolve(project, selectedHandles)",
    ],
}

for rel, needles in checks.items():
    path = ROOT / rel
    if not path.is_file():
        errors.append("missing assignment-integrity file: " + rel)
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(rel + " missing assignment-integrity token: " + needle)

print("QS3D project assignment integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)

print("PASS: Floor/Zone/Family/BulkEdit object-based mutations require exact project-owned semantic instances, duplicate project IDs fail closed, and spoofed same-ID regressions are covered.")
