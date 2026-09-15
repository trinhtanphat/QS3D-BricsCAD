from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPngStructureSmoke.cs").read_text(encoding="utf-8")

old_fence = 'ValidatePngTerminator(payload)' in source and 'private static void ValidatePngTerminator' in source
new_fence = 'ValidatePngChunkStream(payload)' in source and 'private static void ValidatePngChunkStream' in source
if not old_fence and not new_fence:
    raise SystemExit("2D takeoff PNG IEND CRC preflight: missing PNG terminal/chunk-stream validation fence")

if old_fence:
    for token in (
        'ValidatePngChunkCrc(payload, offset + 4, 0, "IEND")',
        'PNG " + chunkName + " chunk CRC is invalid.',
    ):
        if token not in source:
            raise SystemExit(f"2D takeoff PNG IEND CRC preflight: missing production token: {token}")
    terminator = source.index('private static void ValidatePngTerminator')
    crc_check = source.index('ValidatePngChunkCrc(payload, offset + 4, 0, "IEND")', terminator)
    terminator_end = source.index('private static bool TryJpeg', terminator)
    if crc_check >= terminator_end:
        raise SystemExit("2D takeoff PNG IEND CRC preflight: IEND CRC validation must remain inside the PNG terminator fence")
else:
    for token in (
        "var isIend =",
        'PNG IEND chunk length must be zero.',
        'ValidatePngChunkCrc(payload, typeOffset, dataLength, chunkName);',
        'PNG payload contains data after the terminal IEND chunk.',
    ):
        if token not in source:
            raise SystemExit(f"2D takeoff PNG IEND CRC preflight: missing full-stream IEND guard: {token}")
    stream = source.index('private static void ValidatePngChunkStream')
    crc_check = source.index('ValidatePngChunkCrc(payload, typeOffset, dataLength, chunkName);', stream)
    stream_end = source.index('private static bool TryJpeg', stream)
    if crc_check >= stream_end:
        raise SystemExit("2D takeoff PNG IEND CRC preflight: full-stream CRC validation must remain inside the PNG chunk-stream fence")

for token in (
    'RejectsCorruptedIendCrc()',
    'corrupted IEND CRC',
    '0xAE, 0x42, 0x60, 0x82',
):
    if token not in smoke:
        raise SystemExit(f"2D takeoff PNG IEND CRC preflight: missing smoke token: {token}")

print("2D takeoff PNG IEND CRC preflight passed.")
