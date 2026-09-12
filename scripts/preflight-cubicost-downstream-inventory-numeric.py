#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/BenchmarkParity/QsCubicostDownstream.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/QsCubicostDownstreamSmoke.cs"
for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit("Cubicost downstream numeric preflight missing file: " + str(path.relative_to(ROOT)))
source = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8")
for token in (
    "private readonly struct InventoryGroupKey",
    "private readonly struct DownstreamLineKey",
    "CompensatedSum(g.Select(x => x.Quantity)",
    "Math.Abs(sum) >= Math.Abs(value)",
    "double.IsNaN(result) || double.IsInfinity(result)",
):
    if token not in source:
        raise SystemExit("Cubicost downstream production contract missing: " + token)
for stale in (
    'x.InventoryClassification + "\\u001f" + x.Unit',
    'line.ComponentId + "\\u001f" + line.InventoryClassification + "\\u001f" + line.Unit',
    "g.Sum(x => x.Quantity)",
):
    if stale in source:
        raise SystemExit("Cubicost downstream unsafe identity/numeric contract remains: " + stale)
for token in (
    "InventorySemanticKeysAndNumericTotalsStayExact();",
    "1e16 + 2d",
    'const string separator = "\\u001f";',
    'Equal(2, grouped.Count, "embedded separator must not alias inventory grouping identity");',
    'Equal(6, commercial.Count, "embedded separator must not alias downstream validation identity");',
):
    if token not in smoke:
        raise SystemExit("Cubicost downstream smoke missing regression contract: " + token)
print("PASS Cubicost downstream inventory identity and numeric stability contract")
