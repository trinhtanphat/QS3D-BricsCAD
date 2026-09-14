#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'src/QS3D.Core/Export/XlsxQuantityExporter.cs'
SMOKE = ROOT / 'tests/QS3D.Core.SmokeTests/XlsxQuantityProvenanceGenerationSmoke.cs'
for path in (SOURCE, SMOKE):
    if not path.is_file():
        raise SystemExit('XLSX quantity provenance generation preflight missing file: ' + str(path.relative_to(ROOT)))
source = SOURCE.read_text(encoding='utf-8')
smoke = SMOKE.read_text(encoding='utf-8')
for token in (
    'Quantity XLSX provenance values changed during snapshot.',
    'string.Equals(source[index] ?? string.Empty, target[index] ?? string.Empty, StringComparison.Ordinal)',
):
    if token not in source:
        raise SystemExit('XLSX quantity provenance production contract missing: ' + token)
if source.count('if (source.Count != count)') < 2:
    raise SystemExit('XLSX quantity provenance snapshot must re-check count around value replay')
for token in (
    'RejectsStandardElementIdGenerationDrift',
    'RejectsEd2SourceHandleGenerationDrift',
    'SameCountDriftingList',
    'provenance generation drift created output before failing closed',
):
    if token not in smoke:
        raise SystemExit('XLSX quantity provenance smoke missing contract: ' + token)
print('PASS XLSX quantity provenance generation stability contract')
