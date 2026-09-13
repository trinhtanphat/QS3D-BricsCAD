#!/usr/bin/env python3
from pathlib import Path

source = Path("src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")

required = [
    "ValidatePngTerminator(payload);",
    "ValidateJpegTerminator(payload);",
    "PNG payload is truncated or missing the terminal IEND chunk.",
    "JPEG payload is truncated or missing the terminal EOI marker.",
    "payload[payload.Length - 2] != 0xFF",
    "payload[payload.Length - 1] != 0xD9",
    "payload[offset + 4] != (byte)'I'",
    "payload[offset + 7] != (byte)'D'",
]

missing = [token for token in required if token not in source]
if missing:
    raise SystemExit("takeoff raster completeness guard missing: " + ", ".join(missing))

png_probe = source.index("ValidatePngTerminator(payload);")
png_publish = source.index("format = RasterSheetFormat.Png;")
jpeg_probe = source.index("ValidateJpegTerminator(payload);")
jpeg_publish = source.index("format = RasterSheetFormat.Jpeg;")
if png_probe > png_publish or jpeg_probe > jpeg_publish:
    raise SystemExit("raster terminator validation must precede format admission")

print("takeoff raster completeness guard: OK")
