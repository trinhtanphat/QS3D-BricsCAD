#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
HEALTH = ROOT / "src/QS3D.Core/Diagnostics/GridNamingHealthService.cs"
COMPREHENSIVE = ROOT / "src/QS3D.Core/Diagnostics/ComprehensiveModelHealthService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridNamingHealthSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/GRID-WORKFLOW.md"
errors = []

for path in (HEALTH, COMPREHENSIVE, SMOKE, REG, DOC):
    if not path.is_file():
        errors.append("missing Grid naming health contract file: " + str(path.relative_to(ROOT)))

if HEALTH.is_file():
    text = HEALTH.read_text(encoding="utf-8")
    for token in (
        '"GRID_LABEL_DUPLICATE"',
        '"GRID_LABEL_EMPTY"',
        '"GRID_LABEL_TOO_LONG"',
        '"GRID_SEQUENCE_INVALID"',
        '"GRID_SEQUENCE_WITHOUT_LABEL"',
        "StringComparer.OrdinalIgnoreCase",
        "NumberStyles.None",
    ):
        if token not in text:
            errors.append("GridNamingHealthService.cs missing integrity token: " + token)

if COMPREHENSIVE.is_file() and "new GridNamingHealthService().Inspect(project)" not in COMPREHENSIVE.read_text(encoding="utf-8"):
    errors.append("ComprehensiveModelHealthService does not include Grid naming health")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "HealthyGeneratedLabelsRemainClean",
        "DuplicateLabelsAreErrorsOnBothOwners",
        "InvalidSequenceAndEmptyLabelAreReported",
        "ComprehensiveHealthIncludesGridNamingIssues",
    ):
        if token not in text:
            errors.append("GridNamingHealthSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "GridNamingHealthSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Grid naming health smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "GridNamingHealthService",
        "GRID_LABEL_DUPLICATE",
        "Health does not invent labels or mutate the model",
        "Core health intentionally does not pretend",
    ):
        if token not in text:
            errors.append("GRID-WORKFLOW.md missing naming health/runtime boundary: " + token)

print("QS3D Grid naming health preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: comprehensive Core health detects duplicate/malformed Grid naming state without inventing labels or claiming native Grid annotation runtime certification.")
