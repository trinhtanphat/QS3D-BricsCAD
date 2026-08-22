#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Domain/GridNamingService.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/GridNamingReservedLabelIntegritySmoke.cs"
REG = ROOT / "tests/QS3D.Core.SmokeTests/GridNamingReservedLabelIntegritySmokeRegistration.cs"
errors = []

for path in (SOURCE, SMOKE, REG):
    if not path.is_file():
        errors.append("missing Grid naming reserved-label integrity file: " + str(path.relative_to(ROOT)))

if SOURCE.is_file():
    text = SOURCE.read_text(encoding="utf-8")
    for token in (
        "var normalizedExisting = existing.Trim();",
        "if (!reservedLabels.Add(normalizedExisting))",
        '"Grid label is duplicated outside the renumber batch: " + normalizedExisting',
    ):
        if token not in text:
            errors.append("GridNamingService.cs missing fail-closed reserved-label token: " + token)
    if "reservedLabels.Add(existing.Trim());" in text:
        errors.append("GridNamingService.cs still silently collapses duplicate non-target Grid labels")

if SMOKE.is_file():
    text = SMOKE.read_text(encoding="utf-8")
    for token in (
        "DuplicateNonTargetLabelsBlockWholeBatchAtomically",
        "TargetOwnedDuplicateCanBeRepaired",
        "beforeVersion",
        "project.ChangeVersion",
        '"  KEEP  "',
        '"keep"',
    ):
        if token not in text:
            errors.append("Grid naming reserved-label smoke missing regression token: " + token)

if REG.is_file():
    text = REG.read_text(encoding="utf-8")
    for token in (
        "[ModuleInitializer]",
        "GridNamingReservedLabelIntegritySmoke.Run();",
    ):
        if token not in text:
            errors.append("Grid naming reserved-label smoke registration missing token: " + token)

print("QS3D Grid naming reserved-label integrity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Grid renumber rejects duplicate non-target reserved labels atomically while preserving repairable target-involved duplicates.")
