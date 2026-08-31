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
    "while (enumerator.MoveNext())",
    "if (hasKnownCount && index >= knownCount)",
    "if (index >= MaxLines)",
    "var line = enumerator.Current;",
    'throw new InvalidOperationException("Frozen estimate projection source Count does not match source traversal.");',
    "if (hasKnownCount && rows.Count != knownCount)",
    "RequireStableKnownCount(lines, knownCount);",
    "private static void RequireStableKnownCount(IEnumerable<EstimateLine> lines, int expectedCount)",
    "TryGetKnownCount(lines, out var observedCount)",
    'throw new InvalidOperationException("Frozen estimate projection source Count changed during enumeration.");',
    "reports an invalid negative known count",
    "reports conflicting known counts",
)
missing = [token for token in required_source if token not in source]
if missing:
    raise SystemExit("Frozen estimate Count-stability preflight missing source contract: " + ", ".join(missing))

traversal = source.index("while (enumerator.MoveNext())")
overrun_guard = source.index("if (hasKnownCount && index >= knownCount)", traversal)
ceiling_guard = source.index("if (index >= MaxLines)", traversal)
current_read = source.index("var line = enumerator.Current;", traversal)
null_validation = source.index("if (line == null)", current_read)
duplicate_validation = source.index("if (!lineIds.Add(line.EstimateLineId))", null_validation)
row_materialization = source.index("rows.Add(FrozenEstimateProjectionRow.From(line));", duplicate_validation)
if not traversal < overrun_guard < ceiling_guard < current_read < null_validation < duplicate_validation < row_materialization:
    raise SystemExit(
        "Frozen estimate traversal must reject known-Count/streaming overflow before Current and preserve validation before row materialization."
    )

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
