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
    "if (hasKnownCount)\n                        RequireStableKnownCount(lines, knownCount);",
    "if (hasKnownCount && index >= knownCount)",
    "if (index >= MaxLines)",
    "var line = enumerator.Current;",
    "rows.Add(FrozenEstimateProjectionRow.From(line));",
    "index++;",
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
pre_current_rebind = source.index("RequireStableKnownCount(lines, knownCount);", traversal)
overrun_guard = source.index("if (hasKnownCount && index >= knownCount)", traversal)
ceiling_guard = source.index("if (index >= MaxLines)", traversal)
current_read = source.index("var line = enumerator.Current;", traversal)
null_validation = source.index("if (line == null)", current_read)
duplicate_validation = source.index("if (!lineIds.Add(line.EstimateLineId))", null_validation)
row_materialization = source.index("rows.Add(FrozenEstimateProjectionRow.From(line));", duplicate_validation)
index_increment = source.index("index++;", row_materialization)
post_row_rebind = source.index("RequireStableKnownCount(lines, knownCount);", index_increment)
if not traversal < pre_current_rebind < overrun_guard < ceiling_guard < current_read < null_validation < duplicate_validation < row_materialization < index_increment < post_row_rebind:
    raise SystemExit(
        "Frozen estimate traversal must rebind Count before Current, preserve bounds/validation ordering, and rebind after row materialization."
    )

observed_mismatch = source.index("if (hasKnownCount && rows.Count != knownCount)")
final_stability_rebind = source.index("RequireStableKnownCount(lines, knownCount);", observed_mismatch)
sort_rows = source.index("rows.Sort(CompareRows);")
if not observed_mismatch < final_stability_rebind < sort_rows:
    raise SystemExit("Frozen estimate final Count stability must be checked before canonical sorting/result publication.")

required_smoke = (
    "ReportedCountGreaterThanTraversalFailsClosed();",
    "ReportedCountLessThanTraversalFailsBeforeUnexpectedLineValidation();",
    "CountChangesAfterExactTraversalFailsClosed();",
    "NegativeCountAfterExactTraversalFailsClosed();",
    "TransientCountDriftBeforeCurrentFailsClosed();",
    "TransientCountDriftAfterMaterializationFailsClosed();",
    "HonestCountedSourceRemainsAccepted();",
    "PureStreamingSourceRemainsAccepted();",
    "SequencedCountCollection<T>",
    "CurrentReads == 0",
    "CountReads == 4",
    "[ModuleInitializer]",
    "Frozen estimate projection source Count changed during enumeration.",
)
missing_smoke = [token for token in required_smoke if token not in smoke]
if missing_smoke:
    raise SystemExit("Frozen estimate Count-stability smoke is incomplete: " + ", ".join(missing_smoke))

for phrase in (
    "first unexpected line",
    "mid-traversal",
    "post-row",
    "post-traversal",
    "10,000",
    "deterministic sorting",
    "commercial provenance",
    "no licensed BricsCAD runtime",
):
    if phrase not in runbook:
        raise SystemExit("Frozen estimate Count-stability runbook missing boundary: " + phrase)

print("PASS frozen estimate known-Count stability")