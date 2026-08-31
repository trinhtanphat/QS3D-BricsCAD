from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/QuantityRevisionReport.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityRevisionSummaryCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "knownCount.HasValue && index >= knownCount.Value",
    "KnownSummaryCountTraversalMismatch(knownCount.Value, index + 1)",
    "var finalKnownCount = SnapshotKnownSummaryCount(rows);",
    "knownCount != finalKnownCount",
    "known Count changed during traversal",
]
required_smoke = [
    "EarlyKnownCountOverrunWinsBeforeUnexpectedRowValidation();",
    "PostTraversalCountDriftFailsClosed();",
    "PostTraversalNegativeCountFailsClosed();",
    "PostTraversalCountConflictFailsClosed();",
    "StableCountedAndStreamingSourcesRemainAccepted();",
    "MutableCountRows",
    "[ModuleInitializer]",
]

missing = [token for token in required_source if token not in source]
missing += [token for token in required_smoke if token not in smoke]
if missing:
    raise SystemExit(
        "Quantity revision summary Count-stability preflight failed; missing: "
        + ", ".join(missing)
    )

if source.index("knownCount.HasValue && index >= knownCount.Value") > source.index("if (row == null)"):
    raise SystemExit("Known-Count overrun must fail before unexpected-row semantic validation.")
if source.index("var finalKnownCount = SnapshotKnownSummaryCount(rows);") > source.index("var result = new List<QuantityRevisionSummary>();"):
    raise SystemExit("Post-traversal Count rebinding must occur before summary publication work.")

print("PASS quantity revision summary known-Count stability source guard")
