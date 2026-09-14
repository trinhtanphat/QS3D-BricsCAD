from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPngStructureSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "ValidatePngChunkCrc(payload, 12, 13, \"IHDR\");",
    "ValidatePngIhdrSemantics(payload);",
    "private static bool IsSupportedPngBitDepth(byte colorType, byte bitDepth)",
    "PNG IHDR color type and bit depth combination is invalid.",
    "PNG IHDR compression method must be 0.",
    "PNG IHDR filter method must be 0.",
    "PNG IHDR interlace method must be 0 or 1.",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing PNG IHDR semantic guard token: {token}")

crc_index = source.index("ValidatePngChunkCrc(payload, 12, 13, \"IHDR\");")
semantic_index = source.index("ValidatePngIhdrSemantics(payload);")
width_index = source.index("width = ReadInt32BigEndian(payload, 16);")
if not crc_index < semantic_index < width_index:
    raise SystemExit("PNG IHDR semantics must be validated after IHDR CRC and before dimension/evidence publication")

required_smoke = [
    "AcceptsSupportedIhdrSemantics();",
    "RejectsInvalidColorType();",
    "RejectsInvalidBitDepthForColorType();",
    "RejectsInvalidCompressionMethod();",
    "RejectsInvalidFilterMethod();",
    "RejectsInvalidInterlaceMethod();",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing PNG IHDR semantic smoke coverage: {token}")

print("2D takeoff PNG IHDR semantics preflight: PASS")
