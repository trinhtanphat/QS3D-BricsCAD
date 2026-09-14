#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsTakeoffPackageRevision.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsTakeoffPackageRetainedSheetRevisionSmoke.cs"
for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit("Takeoff retained-sheet revision preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

for token in (
    "CompareRetainedSameRevision",
    "Package revision compare requires distinct package revision identifiers.",
    "Retained drawing revision changed source identity without a new drawing revision identifier.",
    "Retained drawing revision changed markup identity without a new drawing revision identifier.",
    "Retained drawing revision changed takeoff evidence without a new drawing revision identifier.",
    "RevisionMarkupChangeKind.Unchanged",
    "SameEvidence",
):
    if token not in source:
        raise SystemExit("Retained-sheet revision production contract missing: " + token)

for token in (
    "AcceptsRetainedUnchangedDrawingRevision();",
    "RejectsMutatedEvidenceUnderSameDrawingRevision();",
    "RejectsEqualPackageRevisionIdentity();",
    'Equal(1, retained.UnchangedCount, "retained unchanged markup count")',
    'Equal(1, result.ChangedSheetCount, "only revised sheet requires review")',
    'Near(1d, result.QuantityDelta, "package quantity delta excludes retained sheet")',
):
    if token not in smoke:
        raise SystemExit("Retained-sheet revision smoke contract missing: " + token)

print("PASS takeoff package retained same-revision sheet comparison contract")
