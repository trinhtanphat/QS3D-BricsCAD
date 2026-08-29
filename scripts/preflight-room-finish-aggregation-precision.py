from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Reporting/RoomFinishSchedule.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/RoomFinishAggregationPrecisionSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var aggregations = new Dictionary<string, FinishAggregationState>",
    "aggregation.LengthM.Add(metrics.LengthM",
    "aggregation.AreaM2.Add(metrics.AreaM2",
    "aggregation.PrimaryQuantity.Add(primary",
    "row.LengthM = aggregation.LengthM.Value",
    "row.AreaM2 = aggregation.AreaM2.Value",
    "row.PrimaryQuantity = aggregation.PrimaryQuantity.Value",
    "var correction = Math.Abs(_sum) >= Math.Abs(incoming)",
    "result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation)",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"Room Finish aggregation precision guard missing production token: {token}")

for forbidden in [
    "row.LengthM = Add(row.LengthM",
    "row.AreaM2 = Add(row.AreaM2",
    "row.PrimaryQuantity = Add(row.PrimaryQuantity",
]:
    if forbidden in source:
        raise SystemExit(f"Room Finish aggregation precision regressed to pairwise fail-fast accumulation: {forbidden}")

required_smoke = [
    "10000000000000000d",
    "10000000000000002d",
    "PreservesRepresentableSmallContributions",
    "PreservesRepresentableSmallContributionsWhenSmallValuesSortFirst",
    "FinalUnrepresentableTotalStillFailsClosed",
    "InvalidQuantityStillFailsClosed",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"Room Finish aggregation precision guard missing smoke token: {token}")

print("PASS Room Finish compensated aggregation preserves representable small contributions without weakening final precision refusal")
