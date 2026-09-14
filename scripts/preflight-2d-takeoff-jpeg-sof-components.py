from pathlib import Path

source = Path('src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs').read_text(encoding='utf-8')
smoke = Path('tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionJpegComponentSmoke.cs').read_text(encoding='utf-8')
legacy_smoke = Path('tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionJpegStructureSmoke.cs').read_text(encoding='utf-8')

required_source = [
    'var seenComponentIds = new bool[256];',
    'componentId == 0 || seenComponentIds[componentId]',
    'horizontalSampling < 1 || horizontalSampling > 4',
    'verticalSampling < 1 || verticalSampling > 4',
    'quantizationTable > 3',
]
for token in required_source:
    if token not in source:
        raise SystemExit(f'missing JPEG SOF component admission token: {token}')

validator = source.index('ValidateJpegFrameHeader(payload, offset, length);')
height_publish = source.index('height = (payload[offset + 3] << 8) | payload[offset + 4];')
if validator > height_publish:
    raise SystemExit('JPEG component validation must run before dimensions are published')

required_smoke = [
    'RejectsZeroComponentIdentifier',
    'RejectsDuplicateComponentIdentifier',
    'RejectsZeroHorizontalSampling',
    'RejectsZeroVerticalSampling',
    'RejectsSamplingFactorAboveFour',
    'RejectsQuantizationTableAboveThree',
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f'missing JPEG component smoke coverage: {token}')

if '(byte)0x11' not in legacy_smoke:
    raise SystemExit('preceding JPEG structure fixture must use a canonical non-zero sampling descriptor')

print('2D takeoff JPEG SOF component preflight PASS')
