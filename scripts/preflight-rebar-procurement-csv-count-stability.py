from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Export" / "RebarProcurementCsvExporter.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "RebarProcurementCsvCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "ReadKnownCount(rows)",
    "ICollection<RebarProcurementSummary>",
    "IReadOnlyCollection<RebarProcurementSummary>",
    "ICollection nonGenericCollection",
    "ValidateKnownCount(rows, admittedCount);",
    "using (var enumerator = rows.GetEnumerator())",
    "var moved = enumerator.MoveNext();",
    "rowCount >= admittedCount.Value",
    "rowCount != admittedCount.Value",
    "conflicting row Count evidence",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing rebar procurement CSV Count-stability source guard token: {token}")

if "foreach (var row in rows)" in source:
    raise SystemExit("rebar procurement CSV must not regress to foreach because Current could be read before Count-integrity admission")

start = source.index("public static string ToCsv(")
end = source.index("private static int? ReadKnownCount", start)
method = source[start:end]
rebound = "ValidateKnownCount(rows, admittedCount);"
move = method.index("var moved = enumerator.MoveNext();")
post_move = method.index(rebound, move)
bound = method.index("if (rowCount >= MaxRowCount)", post_move)
overrun = method.index("if (admittedCount.HasValue && rowCount >= admittedCount.Value)", bound)
current = method.index("var row = enumerator.Current;", overrun)
post_current = method.index(rebound, current)
row_count = method.index("rowCount++;", post_current)
null_check = method.index("if (row == null)", row_count)
append = method.index("sb.Append(Q(row.AlgorithmId))", null_check)

if not move < post_move < bound < overrun < current < post_current < row_count < null_check < append:
    raise SystemExit(
        "rebar procurement CSV counted traversal must order MoveNext -> Count rebound -> hard bound -> known overrun -> Current -> Count rebound -> row acceptance/publication"
    )

required_smoke = [
    "GrowthRejectsBeforeUnexpectedCurrentRead",
    "ShrinkRejectsBeforeSecondCurrentRead",
    "CurrentDriftWinsBeforeNullRowValidation",
    "new RebarProcurementSummary[] { null! }",
    "UnderYieldRejectsAgainstAdmittedCount",
    "ConflictingInterfacesRejectBeforeEnumeration",
    "OversizedKnownCountRejectsBeforeEnumeration",
    "StableKnownCountPreservesOutput",
    "PureStreamingSourceRemainsSupported",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing rebar procurement CSV Count-stability smoke token: {token}")

print("PASS rebar procurement CSV known-Count stability source guard")
