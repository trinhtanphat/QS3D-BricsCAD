from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionJpegStructureSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "ValidateJpegFrameHeader(payload, offset, length);",
    "if (length < 8)",
    "payload[offset + 2] == 0",
    "var componentCount = payload[offset + 7];",
    "var expectedLength = 8 + (3 * componentCount);",
    "if (length != expectedLength)",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing JPEG SOF production guard: {token}")

call_index = source.index("ValidateJpegFrameHeader(payload, offset, length);")
height_index = source.index("height = (payload[offset + 3] << 8)")
if call_index > height_index:
    raise SystemExit("JPEG SOF structure must be validated before dimensions are published")

required_smoke = [
    "AcceptsCanonicalSingleComponentFrame",
    "RejectsZeroSamplePrecision",
    "RejectsZeroComponentCount",
    "RejectsFrameLengthComponentMismatch",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing JPEG SOF smoke coverage: {token}")

print("2D takeoff JPEG SOF structure preflight passed")
