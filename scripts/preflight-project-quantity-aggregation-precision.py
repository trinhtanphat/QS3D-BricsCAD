#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.Core" / "Reporting" / "ProjectQuantityReportBuilder.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "ProjectQuantityAggregationPrecisionSmoke.cs"
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")

required_source = [
    "var accumulators = new Dictionary<string, QuantityReportAggregateState>",
    "aggregate.GrossConcreteM3.Add(",
    "aggregate.NetConcreteM3.Add(",
    "aggregate.FormworkM2.Add(",
    "aggregate.LengthM.Add(",
    "row.GrossConcreteM3 = aggregate.GrossConcreteM3.Value(\"GrossConcreteM3\")",
    "row.LengthM = aggregate.LengthM.Value(\"LengthM\")",
    "lost a non-zero compensation at floating-point precision",
    "IsStrictlyBelowHalfUlp",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing project quantity aggregation precision source token: {token}")

for forbidden in (
    "row.GrossConcreteM3 = QuantityReportMath.Add(row.GrossConcreteM3",
    "row.NetConcreteM3 = QuantityReportMath.Add(row.NetConcreteM3",
    "row.FormworkM2 = QuantityReportMath.Add(row.FormworkM2",
    "row.LengthM = QuantityReportMath.Add(row.LengthM",
):
    if forbidden in source:
        raise SystemExit(f"project quantity grouped metric regressed to pairwise aggregation: {forbidden}")

accumulate = source.index("aggregate.GrossConcreteM3.Add(")
finalize = source.index("row.GrossConcreteM3 = aggregate.GrossConcreteM3.Value")
if accumulate >= finalize:
    raise SystemExit("project quantity metrics must accumulate before final representability validation/publication")

required_smoke = [
    "LargeFirstRepresentableAggregateIsPreserved();",
    "SmallFirstRepresentableAggregateIsPreserved();",
    "MetricsAndGroupsRemainIsolated();",
    "DetailRowsRemainElementIsolated();",
    "FinalUnrepresentableAggregateFailsClosed();",
    "NonFiniteInputStillFailsClosed();",
    "10000000000000002d",
    "9007199254740992d",
    "[ModuleInitializer]",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing project quantity aggregation precision smoke token: {token}")

print("PASS project quantity aggregation precision guard")
