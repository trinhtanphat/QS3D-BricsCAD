#!/usr/bin/env python3
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

required = [
    "src/QS3D.Core/Rebar/ColumnTieLayoutPlanner.cs",
    "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs",
    "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs",
    "src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs",
    "src/QS3D.BricsCAD.V25/ColumnTieCommands.cs",
    "src/QS3D.BricsCAD.V25/ColumnTieHealthCommands.cs",
    "tests/QS3D.Core.SmokeTests/ColumnTieLayoutSmoke.cs",
    "tests/QS3D.Core.SmokeTests/ColumnTieSmokeRegistration.cs",
    "tests/QS3D.Core.SmokeTests/GeneratedTieHealthSmoke.cs",
    "tests/QS3D.Core.SmokeTests/GeneratedTieHealthRegistration.cs",
]
for relative in required:
    if not (ROOT / relative).is_file():
        errors.append("missing column-tie file: " + relative)

checks = {
    "src/QS3D.Core/Rebar/ColumnTieLayoutPlanner.cs": [
        "SpacingMm", "BottomClearanceM", "TopClearanceM", "MaxTies", "actualSpacing > requestedSpacingM",
        "ClosedPath", "PathPerimeterM", "Cover + tie radius leaves no usable tie envelope"
    ],
    "src/QS3D.Core/Diagnostics/GeneratedTieRebarHealthService.cs": [
        "GeneratedTieRebarHandles", "TIE_REBAR_GENERATED_OWNERSHIP_CONFLICT", "TIE_REBAR_GENERATED_SOLID_MISSING",
        "TIE_REBAR_GENERATED_COUNT_MISMATCH", "GeneratedTieRebarActualSpacingM"
    ],
    "src/QS3D.BricsCAD.V25/Cad/GeneratedTieRebarOwnershipGuard.cs": [
        "GeneratedRebarHandles", "GeneratedShapeRebarHandles", "GeneratedTieRebarHandles", "EnsureTieOwned", "Refusing destructive erase"
    ],
    "src/QS3D.BricsCAD.V25/Cad/ColumnTieSolidBuilder.cs": [
        "ColumnTieLayoutPlanner.Plan", "GeneratedTieRebarHandles", "BooleanOperationType.BoolUnite", "GeneratedTieRebarOwnershipGuard.Build(project)",
        "ownership.EnsureTieOwned", "MaxTiesPerElement", "MaxTiesPerBatch", "GeneratedTieRebarActualSpacingM"
    ],
    "tests/QS3D.Core.SmokeTests/ColumnTieLayoutSmoke.cs": [
        "SpacingIsMaximumNotMinimum", "SingleTieWhenUsableRangeCollapses", "RejectsImpossibleCoverAndBadSpacing", "1.448d"
    ],
    "tests/QS3D.Core.SmokeTests/GeneratedTieHealthSmoke.cs": [
        "MissingTieSolidIsReported", "OwnershipConflictIsReported", "CountAndSourceConflictsAreReported"
    ],
}
for relative, needles in checks.items():
    path = ROOT / relative
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(relative + " missing guard/token: " + needle)

commands = []
for path in (ROOT / "src/QS3D.BricsCAD.V25").rglob("*.cs"):
    text = path.read_text(encoding="utf-8")
    commands += re.findall(r'CommandMethod\("([A-Za-z0-9_]+)"', text)
for command in ("QS3DREBARTIES3D", "QS3DREBARTIEHEALTH"):
    if command not in commands:
        errors.append("missing column-tie command: " + command)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: rectangular column tie planning, bounded native geometry, ownership guards, health and deterministic smoke registration are present.")
