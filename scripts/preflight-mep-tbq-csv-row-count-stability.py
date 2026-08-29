#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Mep/MepTbqProjection.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MepTbqCsvRowCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var admittedRowCount = rows.Count;",
    "RequireCsvRowCountAdmission(admittedRowCount);",
    "RequireStableCsvRowCount(rows, admittedRowCount);",
    "var row = rows[i] ?? throw new ArgumentException",
    "MEP/TBQ CSV row Count changed during serialization.",
    "MEP/TBQ CSV row Count must not be negative.",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"MEP/TBQ CSV Count stability guard missing source token: {token}")

method_start = source.index("public string SerializeCsv(IReadOnlyList<MepTbqReportRow> rows)")
method_end = source.index("public static bool IsOwnedItem", method_start)
method = source[method_start:method_end]

if "i < rows.Count" in method:
    raise SystemExit("MEP/TBQ CSV Count stability guard forbids live Count as the serialization loop bound")

ordered = [
    "var admittedRowCount = rows.Count;",
    "RequireCsvRowCountAdmission(admittedRowCount);",
    "for (var i = 0; i < admittedRowCount; i++)",
    "RequireStableCsvRowCount(rows, admittedRowCount);",
    "var row = rows[i] ?? throw new ArgumentException",
    "RequireStableCsvRowCount(rows, admittedRowCount);",
    "return builder.ToString();",
]
pos = -1
for token in ordered:
    nxt = method.find(token, pos + 1)
    if nxt < 0:
        raise SystemExit(f"MEP/TBQ CSV Count stability guard missing ordered token: {token}")
    pos = nxt

required_smoke = [
    "[ModuleInitializer]",
    "GrowthAfterFirstRowRejectsBeforeUnexpectedIndexerRead",
    "ShrinkAfterFirstRowRejectsBeforeMissingIndexerRead",
    "PostTraversalCountDriftRejects",
    "NegativeCountRejectsBeforeIndexerRead",
    "OversizedCountRejectsBeforeIndexerRead",
    "StableRowsSerializeDeterministically",
    "source.IndexerReads == 1",
    "source.IndexerReads == 0",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"MEP/TBQ CSV Count stability guard missing smoke token: {token}")

print("PASS MEP/TBQ CSV row Count stability source guard")
