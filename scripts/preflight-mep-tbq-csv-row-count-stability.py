#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepTbqProjection.cs"
HISTORICAL_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqCsvRowCountStabilitySmoke.cs"
MULTI_COUNT_SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqCsvMultiCountSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
historical_smoke = HISTORICAL_SMOKE.read_text(encoding="utf-8")
multi_count_smoke = MULTI_COUNT_SMOKE.read_text(encoding="utf-8")

required_source = [
    "var rowCountContract = CaptureCsvRowCountContract(rows);",
    "var admittedRowCount = rowCountContract.ReadOnlyCount;",
    "CaptureCsvRowCountContract(IReadOnlyList<MepTbqReportRow> rows)",
    "if (rows is ICollection<MepTbqReportRow> genericCollection)",
    "if (rows is ICollection nonGenericCollection)",
    "MEP/TBQ CSV row source reports conflicting Count channels.",
    "RequireStableCsvRowCounts(rows, rowCountContract);",
    "var row = rows[i];",
    "if (row == null)",
    "MEP/TBQ CSV row Count changed during serialization.",
    "MEP/TBQ CSV row Count must not be negative.",
    "rows.Count != expected.ReadOnlyCount",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"MEP/TBQ CSV multi-Count stability guard missing source token: {token}")

method_start = source.index("public string SerializeCsv(IReadOnlyList<MepTbqReportRow> rows)")
method_end = source.index("public static bool IsOwnedItem", method_start)
method = source[method_start:method_end]

if "i < rows.Count" in method:
    raise SystemExit("MEP/TBQ CSV Count stability guard forbids live Count as the serialization loop bound")

ordered = [
    "var rowCountContract = CaptureCsvRowCountContract(rows);",
    "var admittedRowCount = rowCountContract.ReadOnlyCount;",
    "for (var i = 0; i < admittedRowCount; i++)",
    "RequireStableCsvRowCounts(rows, rowCountContract);",
    "var row = rows[i];",
    "RequireStableCsvRowCounts(rows, rowCountContract);",
    "if (row == null)",
    "RequireStableCsvRowCounts(rows, rowCountContract);",
    "return builder.ToString();",
]
pos = -1
for token in ordered:
    nxt = method.find(token, pos + 1)
    if nxt < 0:
        raise SystemExit(f"MEP/TBQ CSV multi-Count stability guard missing ordered token: {token}")
    pos = nxt

historical_tokens = [
    "[ModuleInitializer]",
    "GrowthAfterFirstRowRejectsBeforeUnexpectedIndexerRead",
    "ShrinkAfterFirstRowRejectsBeforeMissingIndexerRead",
    "IndexerInducedCountDriftPreemptsNullRowValidation",
    "PostTraversalCountDriftRejects",
    "NegativeCountRejectsBeforeIndexerRead",
    "OversizedCountRejectsBeforeIndexerRead",
    "NullRowValidationRemainsInsideAdmittedCount",
    "StableRowsSerializeDeterministically",
]
for token in historical_tokens:
    if token not in historical_smoke:
        raise SystemExit(f"MEP/TBQ CSV multi-Count guard lost historical smoke token: {token}")

multi_count_tokens = [
    "[ModuleInitializer]",
    "ConflictingGenericCountRejectsBeforeIndexerRead",
    "ConflictingNonGenericCountRejectsBeforeIndexerRead",
    "IndexerInducedGenericCountDriftRejectsBeforeRowAcceptance",
    "StableThreeChannelRowsRemainAccepted",
    "ICollection<MepTbqReportRow>",
    "ICollection",
    "source.IndexerReads == 0",
    "source.IndexerReads == 1",
]
for token in multi_count_tokens:
    if token not in multi_count_smoke:
        raise SystemExit(f"MEP/TBQ CSV multi-Count guard missing hostile smoke token: {token}")

print("PASS MEP/TBQ CSV row Count stability across read-only, generic, and non-generic Count channels")
