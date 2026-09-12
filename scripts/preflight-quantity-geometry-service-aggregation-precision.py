#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BEAM = ROOT / 'src' / 'QS3D.BricsCAD.V25' / 'Reporting' / 'BeamFormworkQuantityPolicy.cs'
beam = BEAM.read_text(encoding='utf-8')

required = (
    'QuantityReportMath.FiniteAccumulator',
    'deductionAccumulator.Add(',
    'sideAccumulator.Add(',
    'bottomAccumulator.Add(',
    'deductionAccumulator.Value(',
    'sideAccumulator.Value(',
    'bottomAccumulator.Value(',
)
for token in required:
    if token not in beam:
        raise SystemExit(f'beam formwork compensated aggregation missing: {token}')

for forbidden in (
    'allowedRows.Sum(x => x.Area)',
    '.Sum(x => x.NetArea)',
):
    if forbidden in beam:
        raise SystemExit(f'beam formwork regressed to order-sensitive accumulation: {forbidden}')

print('PASS beam formwork compensated aggregation precision source guard')
