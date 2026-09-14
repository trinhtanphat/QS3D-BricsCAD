#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageSheetRevisionPolicy.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageSheetRevisionPolicySmoke.cs"
for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit("Takeoff package sheet-revision preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for token in (
    "AutodeskTakeoffPackagePerSheetRevisionCoordinator",
    "string.Equals(item.Revision, sourceSheet.Revision, StringComparison.OrdinalIgnoreCase)",
    'package.Revision + "\\u001fSTALE-SHEET-REVISION"',
    "result.Sources.Select(source => RestoreSourceRevision(source, sourceRevisions))",
    "inner.Build(",
):
    if token not in source:
        raise SystemExit("Per-sheet revision production contract missing: " + token)

for token in (
    "BuildsPackageAcrossIndependentSheetRevisions();",
    "RejectsEvidenceFromWrongSheetRevision();",
    'Equal("R3", result.Sources.Single(x => x.Id == "A101").Revision',
    'Equal("R7", result.Sources.Single(x => x.Id == "A102").Revision',
    'Near(5d, result.Inventory[0].MeasuredQuantity, "cross-sheet measured quantity")',
    'result.Issues.Any(x => x.Code == "PKG.STALE_EVIDENCE_REVISION")',
):
    if token not in smoke:
        raise SystemExit("Per-sheet revision smoke contract missing: " + token)

print("PASS takeoff package per-sheet revision policy contract")
