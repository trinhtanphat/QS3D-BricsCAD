#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

source_path = ROOT / "src/QS3D.Core/Model/EntitySnapshot.cs"
smoke_path = ROOT / "tests/QS3D.Core.SmokeTests/LogicRegressionSmoke.cs"

if not source_path.is_file():
    errors.append("missing EntitySnapshot source")
else:
    source = source_path.read_text(encoding="utf-8")
    required = (
        '"Entity type must not contain leading or trailing whitespace."',
        "if (!string.Equals(value, canonical, StringComparison.Ordinal))",
        "throw new ArgumentException(boundaryWhitespaceMessage, parameterName);",
        "if (char.IsControl(value[index]))",
    )
    for needle in required:
        if needle not in source:
            errors.append("EntitySnapshot missing canonical EntityType contract: " + needle)

if not smoke_path.is_file():
    errors.append("missing LogicRegressionSmoke")
else:
    smoke = smoke_path.read_text(encoding="utf-8")
    required_smoke = (
        "EntitySnapshotRejectsNonCanonicalEntityType();",
        'new EntitySnapshot("A1", " Line", "A-BEAM")',
        'new EntitySnapshot("A2", "Line ", "A-BEAM")',
        'new EntitySnapshot("A3", "\\tLine", "A-BEAM")',
        'Equal("Line", canonical.EntityType);',
    )
    for needle in required_smoke:
        if needle not in smoke:
            errors.append("LogicRegressionSmoke missing EntityType regression: " + needle)

print("EntitySnapshot EntityType canonical identity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: EntitySnapshot rejects padded/control EntityType identity before recognition while canonical EntityType remains unchanged.")
