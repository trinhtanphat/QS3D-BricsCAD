#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Cost/FrozenEstimateProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/FrozenEstimateProjectionTraversalCountSmoke.cs"
RUNBOOK = ROOT / "docs/FEATURE-RUNBOOKS/frozen-estimate-known-count-stability.md"

for path in (SOURCE, SMOKE, RUNBOOK):
    if not path.is_file():
        raise SystemExit("Frozen estimate Count-stability preflight missing file: " + str(path.relative_to(ROOT)))

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
runbook = RUNBOOK.read_text(encoding="utf-8")

required_source = (
    "if (hasKnownCount && index >= knownCount)",
    'throw new InvalidOperationException("Frozen estimate projection source Count does not match source traversal.");',
    "if (hasKnownCount && rows.Count != knownCount)",
    "RequireStableKnownCount(lines, knownCount);",
    "private static void RequireStableKnownCount(IEnumerable<EstimateLine> lines, int expectedCount)",
    "TryGetKnownCount(lines, out var observedCount)",
    'throw new InvalidOperationException("Frozen estimate projection source Count changed during enumeration.");',
    "if (index == MaxLines)",
    "reports an invalid negative known count",
    "reports conflicting known counts",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Frozen estimate Count-stability preflight missing source contract: " + ", ".join(missing))

overrun_guard = source.index("if (hasKnownCount && index >= knownCount)")
null_validation = source.index("if (line == null)")
duplicate_validation = source.index("if (!lineIds.Add(line.EstimateLineId))")
row_materialization = source.index("rows.Add(FrozenEstimateProjectionRow.From(line));")
if not overrun_guard < null_validation or not overrun_guard < duplicate_validation or not overrun_guard < row_materialization:
    raise SystemExit("Frozen estimate known-Count overrun must fail before unexpected-line validation/materialization.")

observed_mismatch = source.index("if (hasKnownCount && rows.Count != knownCount)")
stability_rebind = source.index("RequireStableKnownCount(lines, knownCount);")
sort_rows = source.index("rows.Sort(CompareRows);")
if not observed_mismatch < stability_rebind < sort_rows:
    raise SystemExit("Frozen estimate post-traversal Count stability must be checked before canonical sorting/result publication.")

required_smoke = (
    "ReportedCountGreaterThanTraversalFailsClosed();",
    "ReportedCountLessThanTraversalFailsBeforeUnexpectedLineValidation();",
    "CountChangesAfterExactTraversalFailsClosed();",
    "NegativeCountAfterExactTraversalFailsClosed();",
    "HonestCountedSourceRemainsAccepted();",
    "PureStreamingSourceRemainsAccepted();",
    "MoveNextCalls == 2",
    "CountReads == 2",
    "DriftingReadOnlyCollection<T>",
    "Frozen estimate projection source Count changed during enumeration.",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Frozen estimate Count-stability smoke is incomplete: " + ", ".join(missing_smoke))

for phrase in (
    "first unexpected line",
    "post-traversal",
    "10,000",
    "deterministic sorting",
    "commercial provenance",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Frozen estimate Count-stability runbook missing boundary: " + phrase)

print("PASS frozen estimate known-Count stability")
