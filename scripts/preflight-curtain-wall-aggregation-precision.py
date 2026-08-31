#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "CurtainWallSchedule.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "CurtainWallAggregationPrecisionSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var accumulators = new Dictionary<string, CurtainWallAggregateState>",
    "aggregate.TotalWallLengthM.Add(Q(element, \"LengthM\")",
    "aggregate.GrossWallAreaM2.Add(Q(element, \"GrossWallAreaM2\")",
    "aggregate.OpeningAreaM2.Add(Q(element, \"OpeningAreaM2\")",
    "aggregate.NetGlassAreaM2.Add(Q(element, \"CurtainNetGlassAreaM2\")",
    "aggregate.FrameFaceAreaM2.Add(Q(element, \"CurtainFrameFaceAreaM2\")",
    "aggregate.FrameLengthM.Add(Q(element, \"CurtainFrameLengthM\")",
    "row.TotalWallLengthM = aggregate.TotalWallLengthM.Value(\"TotalWallLengthM\")",
    "lost a non-zero compensation at floating-point precision",
    "IsStrictlyBelowHalfUlp",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing Curtain Wall aggregation precision source token: {token}")

for forbidden in (
    "row.TotalWallLengthM = Add(row.TotalWallLengthM",
    "row.GrossWallAreaM2 = Add(row.GrossWallAreaM2",
    "row.OpeningAreaM2 = Add(row.OpeningAreaM2",
    "row.NetGlassAreaM2 = Add(row.NetGlassAreaM2",
    "row.FrameFaceAreaM2 = Add(row.FrameFaceAreaM2",
    "row.FrameLengthM = Add(row.FrameLengthM",
):
    if forbidden in source:
        raise SystemExit(f"Curtain Wall continuous grouped metric regressed to pairwise aggregation: {forbidden}")

accumulate = source.index("aggregate.TotalWallLengthM.Add")
finalize = source.index("row.TotalWallLengthM = aggregate.TotalWallLengthM.Value")
if accumulate >= finalize:
    raise SystemExit("Curtain Wall metrics must accumulate before final representability validation/publication")

required_smoke = [
    "LargeFirstRepresentableAggregateIsPreserved",
    "SmallFirstRepresentableAggregateIsPreserved",
    "MetricsAndGroupsRemainIsolated",
    "FinalUnrepresentableAggregateFailsClosed",
    "NonFiniteInputStillFailsClosed",
    "10000000000000002d",
    "9007199254740992d",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing Curtain Wall aggregation precision smoke token: {token}")

print("PASS Curtain Wall compensated aggregation precision guard")
