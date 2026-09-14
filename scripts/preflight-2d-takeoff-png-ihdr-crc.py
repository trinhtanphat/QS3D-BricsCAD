from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPngStructureSmoke.cs").read_text(encoding="utf-8")

for token in (
    'ValidatePngChunkCrc(payload, 12, 13, "IHDR")',
    'PNG " + chunkName + " chunk CRC is invalid.',
    '0xedb88320u',
    'ReadUInt32BigEndian',
):
    if token not in source:
        raise SystemExit(f"2D takeoff PNG IHDR CRC preflight: missing production token: {token}")

for token in (
    'RejectsCorruptedIhdrData()',
    'RejectsCorruptedIhdrCrc()',
    'WriteCrc(bytes, 12, 13, 29)',
    'corrupted IHDR data',
    'corrupted IHDR CRC',
):
    if token not in smoke:
        raise SystemExit(f"2D takeoff PNG IHDR CRC preflight: missing smoke token: {token}")

if source.index('ValidatePngChunkCrc(payload, 12, 13, "IHDR")') > source.index('width = ReadInt32BigEndian(payload, 16)'):
    raise SystemExit("2D takeoff PNG IHDR CRC preflight: dimensions must not be published before IHDR CRC validation")

print("2D takeoff PNG IHDR CRC preflight passed.")
