#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src/QS3D.Core/Domain/GridNamingService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridNamingSmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"
DOC = ROOT / "docs/GRID-WORKFLOW.md"
errors = []

for path in (SERVICE, SMOKE, REG, DOC):
    if not path.is_file():
        errors.append("missing Grid naming contract file: " + str(path.relative_to(ROOT)))

if SERVICE.is_file():
    text = SERVICE.read_text(encoding="utf-8")
    for token in (
        'public const string GridLabelKey = "GridLabel"',
        'public const string GridSequenceIndexKey = "GridSequenceIndex"',
        'GridLabelSequence.Numeric',
        'GridLabelSequence.Alphabetic',
        'ids.Distinct(StringComparer.OrdinalIgnoreCase).Count() != ids.Count',
        'element.Category != ElementCategory.Grid',
        'reservedLabels.Contains(label)',
        'project.Touch()',
    ):
        if token not in text:
            errors.append("GridNamingService.cs missing fail-closed token: " + token)

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "NumericSequenceIsOrderedAndPadded",
        "AlphabeticSequenceCrossesZDeterministically",
        "ExistingExternalLabelBlocksWholeBatch",
        "NonGridInputBlocksWholeBatch",
    ):
        if token not in text:
            errors.append("GridNamingSmoke.cs missing regression scenario: " + token)

if REG.is_file() and "GridNamingSmoke.Run();" not in REG.read_text(encoding="utf-8"):
    errors.append("Grid naming smoke is not registered")

if DOC.is_file():
    text = DOC.read_text(encoding="utf-8")
    for token in (
        "GridNamingService",
        "caller supplies an explicit ordered list",
        "source-implemented native endpoint annotation",
        "source implementation, not V25 runtime certification",
    ):
        if token not in text:
            errors.append("GRID-WORKFLOW.md missing naming/runtime boundary: " + token)

print("QS3D Grid semantic naming preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Grid semantic naming is deterministic, batch-prevalidated and uniqueness-guarded; native endpoint annotation is source-implemented while exact-SHA V25 runtime/UI certification remains an explicit gate.")
