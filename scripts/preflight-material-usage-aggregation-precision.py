from pathlib import Path

source = Path("src/QS3D.Core/Reporting/MaterialUsageSchedule.cs").read_text(encoding="utf-8")
smoke = Path("tests/QS3D.Core.SmokeTests/MaterialUsageAggregationPrecisionSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "Dictionary<string, UsageGroup>",
    "StableAccumulator",
    "FinalizeQuantities()",
    "QuantityReportMath.NonNegative(value, label)",
    "_sawSwallowedContribution",
    "lost a non-zero swallowed contribution",
    "group.Row.ElementIds.Add(element.Id)",
    "ReportingRowProvenance.AppendSourceHandles(group.Row.SourceHandles, element.SourceHandles)",
]
for token in required_source:
    if token not in source:
        raise SystemExit("Material Usage aggregation precision guard missing source contract: " + token)

required_smoke = [
    "10000000000000002d",
    'Build("LengthM", 1e16, 1d, 1d)',
    'Build("NetWallAreaM2", 1e16, 1d, 1d)',
    'Build("NetVolumeM3", 1e16, 1d, 1d)',
    'Build("WeightKg", 1e16, 1d, 1d)',
    "PreservesOrdinaryDecimalAggregation",
    'BuildTwo("NetVolumeM3", 2.8d, 1.6d)',
    "row.ElementCount != 2",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit("Material Usage aggregation precision guard missing smoke contract: " + token)

if "QuantityReportMath.Add(row.LengthM" in source or "QuantityReportMath.Add(row.AreaM2" in source:
    raise SystemExit("Material Usage aggregation reverted to direct fail-on-swallow row addition.")

print("Material Usage aggregation precision source guard passed.")
