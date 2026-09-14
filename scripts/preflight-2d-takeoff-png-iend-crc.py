from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPngStructureSmoke.cs").read_text(encoding="utf-8")

for token in (
    'ValidatePngChunkCrc(payload, offset + 4, 0, "IEND")',
    'PNG " + chunkName + " chunk CRC is invalid.',
    'ValidatePngTerminator(payload)',
):
    if token not in source:
        raise SystemExit(f"2D takeoff PNG IEND CRC preflight: missing production token: {token}")

for token in (
    'RejectsCorruptedIendCrc()',
    'corrupted IEND CRC',
    '0xAE, 0x42, 0x60, 0x82',
):
    if token not in smoke:
        raise SystemExit(f"2D takeoff PNG IEND CRC preflight: missing smoke token: {token}")

terminator = source.index('private static void ValidatePngTerminator')
crc_check = source.index('ValidatePngChunkCrc(payload, offset + 4, 0, "IEND")', terminator)
terminator_end = source.index('private static bool TryJpeg', terminator)
if crc_check >= terminator_end:
    raise SystemExit("2D takeoff PNG IEND CRC preflight: IEND CRC validation must remain inside the PNG terminator fence")

print("2D takeoff PNG IEND CRC preflight passed.")
