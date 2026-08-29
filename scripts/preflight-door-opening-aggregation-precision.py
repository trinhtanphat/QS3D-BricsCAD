#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "DoorOpeningSchedule.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "DoorOpeningAggregationPrecisionSmoke.cs"

source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = (
    "var areaAggregations = new Dictionary<string, CompensatedAreaTotal>",
    "areaAggregations[key] = new CompensatedAreaTotal();",
    "areaAggregations[key].Add(areaM2, element.Id + \"/opening schedule area\");",
    "row.OpeningAreaM2 = areaAggregations[key].Value(\"door/opening schedule/OpeningAreaM2\");",
    "private sealed class CompensatedAreaTotal",
    "var correction = Math.Abs(_sum) >= Math.Abs(incoming)",
    "if (_compensation != 0d && result == _sum && !IsStrictlyBelowHalfUlp(_sum, _compensation))",
    "private static bool IsStrictlyBelowHalfUlp(double current, double compensation)",
)
for token in required_source:
    if token not in source:
        raise SystemExit(f"Door/opening aggregation precision guard missing source contract: {token}")

for forbidden in (
    "row.OpeningAreaM2 = QuantityReportMath.Add(row.OpeningAreaM2, areaM2",
    "row.OpeningAreaM2 += areaM2",
):
    if forbidden in source:
        raise SystemExit(f"Door/opening aggregation precision regressed to pairwise accumulation: {forbidden}")

required_smoke = (
    "PreservesRepresentableSmallContributions();",
    "PreservesRepresentableSmallContributionsWhenSmallValuesSortFirst();",
    "10000000000000002d",
    "9007199254740992d",
    "Capture<OverflowException>",
    "double.PositiveInfinity",
    "[ModuleInitializer]",
)
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"Door/opening aggregation precision guard missing deterministic smoke contract: {token}")

print("PASS door/opening compensated aggregation precision source guard")
