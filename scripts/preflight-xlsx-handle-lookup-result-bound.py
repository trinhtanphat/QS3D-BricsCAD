#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/XlsxHandleReader.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/XlsxHandleLookupResultBoundSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/xlsx-handle-lookup-result-bound.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Xlsx lookup bound preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "MaximumIdentityValues = 16384",
    "MaterializeIdentityValues(handles, nameof(handles))",
    "MaterializeIdentityValues(elementIds, nameof(elementIds))",
    "observed > MaximumIdentityValues",
    "identity values",
    "StringComparer.OrdinalIgnoreCase",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Xlsx lookup bounded materialization source contract missing: " + repr(missing))

legacy = "handles.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList().AsReadOnly()"
if legacy in source:
    raise SystemExit("Xlsx lookup must not use unbounded LINQ materialization for handles.")

required_smoke = (
    "HandlesRejectFirstOverBoundObservation",
    "ElementIdsRejectFirstOverBoundObservation",
    "StableInputsPreserveCanonicalizationAndDeduplication",
    "MaximumIdentityValues + 1",
    "CurrentReads",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Xlsx lookup bounded materialization smoke contract missing: " + repr(missing_smoke))

print("PASS Xlsx handle lookup result bounded identity materialization guard")
