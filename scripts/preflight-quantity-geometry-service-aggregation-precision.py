#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SERVICE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "QuantityGeometryExplanationService.cs"
BEAM = ROOT / "src" / "QS3D.BricsCAD.V25" / "Reporting" / "BeamFormworkQuantityPolicy.cs"

service = SERVICE.read_text(encoding="utf-8")
beam = BEAM.read_text(encoding="utf-8")

required_service = (
    "QuantityReportMath.FiniteAccumulator",
    "grossVolumeAccumulator.Add(",
    "netVolumeAccumulator.Add(",
    "individualVolumeAccumulators",
    "residualAreaAccumulators",
    "coverageAccumulator.Add(",
    "individualVolumeAccumulator.Value(",
)
for token in required_service:
    if token not in service:
        raise SystemExit(f"quantity geometry service compensated aggregation missing: {token}")

for forbidden in (
    "grossVolumeCad += SafeVolumeCad(target);",
    "netVolumeCad += SafeVolumeCad(volumeResidual);",
    "residualAreasCad[best] += areaCad;",
    "totalCad += areaCad;",
    "individualVolumeCad.Values.Sum()",
    "values[key] = (values.TryGetValue(key, out var current) ? current : 0d) + value",
):
    if forbidden in service:
        raise SystemExit(f"quantity geometry service regressed to order-sensitive accumulation: {forbidden}")

required_beam = (
    "QuantityReportMath.FiniteAccumulator",
    "deductionAccumulator.Add(",
    "sideAccumulator.Add(",
    "bottomAccumulator.Add(",
)
for token in required_beam:
    if token not in beam:
        raise SystemExit(f"beam formwork compensated aggregation missing: {token}")

for forbidden in (
    "allowedRows.Sum(x => x.Area)",
    ".Sum(x => x.NetArea)",
):
    if forbidden in beam:
        raise SystemExit(f"beam formwork regressed to order-sensitive accumulation: {forbidden}")

print("PASS quantity geometry service/formwork compensated aggregation precision source guard")
