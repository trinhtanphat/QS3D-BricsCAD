from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Revisions/QuantityRevisionReport.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QuantityRevisionSummaryCountStabilitySmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "knownCount.HasValue && index >= knownCount.Value",
    "KnownSummaryCountTraversalMismatch(knownCount.Value, index + 1)",
    "RequireStableKnownSummaryCount(rows, knownCount);",
    "var moved = enumerator.MoveNext();",
    "var row = enumerator.Current;",
    "known Count changed during traversal",
]
required_smoke = [
    "EarlyKnownCountOverrunWinsBeforeUnexpectedRowValidation();",
    "TransientMoveNextCountDriftFailsBeforeCurrent();",
    "TransientCurrentCountDriftFailsBeforeRetention();",
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

post_move = source.index("RequireStableKnownSummaryCount(rows, knownCount);", source.index("var moved = enumerator.MoveNext();"))
overrun = source.index("knownCount.HasValue && index >= knownCount.Value", post_move)
current = source.index("var row = enumerator.Current;", overrun)
post_current = source.index("RequireStableKnownSummaryCount(rows, knownCount);", current)
null_validation = source.index("if (row == null)", post_current)
if not post_move < overrun < current < post_current < null_validation:
    raise SystemExit("Known-Count overrun and Current rebound ordering regressed.")

final_rebind = source.index("RequireStableKnownSummaryCount(rows, knownCount);", source.index("if (knownCount.HasValue && index != knownCount.Value)"))
publication = source.index("var result = new List<QuantityRevisionSummary>();")
if final_rebind > publication:
    raise SystemExit("Post-traversal Count rebinding must occur before summary publication work.")

print("PASS quantity revision summary known-Count stability source guard")
