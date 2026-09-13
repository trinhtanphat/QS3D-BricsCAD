#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsCubicostDownstream.cs"
AGGREGATION = ROOT / "src/QS3D.Core/BenchmarkParity/QsCubicostQuantityAggregation.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsCubicostDownstreamSmoke.cs"
for path in (SOURCE, AGGREGATION, SMOKE):
    if not path.is_file():
        raise SystemExit("Cubicost downstream numeric preflight missing file: " + str(path.relative_to(ROOT)))
source = SOURCE.read_text(encoding="utf-8")
aggregation = AGGREGATION.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
for token in (
    "private readonly struct InventoryGroupKey",
    "private readonly struct DownstreamLineKey",
    "CubicostQuantityAggregation.SumFinite(g.Select(x => x.Quantity)",
):
    if token not in source:
        raise SystemExit("Cubicost downstream production contract missing: " + token)
for token in (
    "internal static class CubicostQuantityAggregation",
    "internal static double SumFinite(IEnumerable<double> values, string label)",
    "Math.Abs(sum) >= Math.Abs(value)",
    "double.IsNaN(result) || double.IsInfinity(result)",
    "return result == 0d ? 0d : result;",
):
    if token not in aggregation:
        raise SystemExit("Cubicost shared numeric aggregation contract missing: " + token)
for stale in (
    'x.InventoryClassification + "\\u001f" + x.Unit',
    'line.ComponentId + "\\u001f" + line.InventoryClassification + "\\u001f" + line.Unit',
    "g.Sum(x => x.Quantity)",
    "private static double CompensatedSum",
):
    if stale in source:
        raise SystemExit("Cubicost downstream unsafe or duplicated identity/numeric contract remains: " + stale)
for token in (
    "InventorySemanticKeysAndNumericTotalsStayExact();",
    "1e16 + 2d",
    'const string separator = "\\u001f";',
    'Equal(2, grouped.Count, "embedded separator must not alias inventory grouping identity");',
    'Equal(6, commercial.Count, "embedded separator must not alias downstream validation identity");',
):
    if token not in smoke:
        raise SystemExit("Cubicost downstream smoke missing regression contract: " + token)
print("PASS Cubicost downstream inventory identity and shared numeric stability contract")
