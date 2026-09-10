from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/MeasurementWorkItemCoverageCsvExporter.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/MeasurementWorkItemCoverageCsvBoundedWriteSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.exists() else ""

required_source = [
    "MaxCsvBytes",
    "StrictUtf8WithBom",
    "BoundedUtf8TextWriter",
    "WriteCsv(",
    "WriteQuotedCsvValue(",
    "WriteJoinedIssues(",
    "WriteJoinedValues(",
    "WriteEscapedCsvFragment(",
    "checked",
    "AtomicFileCommit.ReplaceWithoutBackup",
    "RequiresSpreadsheetFormulaEscape",
    "RequireLiteralCsvIdentity",
]
forbidden_source = [
    "var content = ToCsv(matrix);",
    "writer.Write(content);",
    "var sb = new StringBuilder();",
    "string.Join(\"|\", cell.Issues",
    "string.Join(\"|\", cell.AffectedElementIds",
    "private static string Q(string? value)",
]
required_smoke = [
    "ExistingCoverageCsvSurvivesBudgetFailure",
    "HostileMultibyteCoverageFailsClosed",
    "ToCsvHonorsTheSameByteCeiling",
    "FormulaPrefixesRemainFailClosed",
    "StrictUtf8RoundTripPreservesProvenance",
]

missing = [token for token in required_source if token not in source]
forbidden = [token for token in forbidden_source if token in source]
missing_smoke = [token for token in required_smoke if token not in smoke]

if missing or forbidden or missing_smoke:
    if missing:
        print("missing production bounded-write token(s): " + ", ".join(missing))
    if forbidden:
        print("forbidden eager publication token(s): " + ", ".join(forbidden))
    if missing_smoke:
        print("missing hostile regression token(s): " + ", ".join(missing_smoke))
    raise SystemExit(1)

print("PASS Measurement Work Item Coverage CSV bounded publication guard")
