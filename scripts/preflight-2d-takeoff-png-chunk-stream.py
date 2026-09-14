#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPngStructureSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "ValidatePngChunkStream(payload);",
    "PNG chunk stream must contain IDAT image data before IEND.",
    "PNG chunk stream contains a duplicate IHDR chunk.",
    "PNG payload contains data after the terminal IEND chunk.",
    "ValidatePngChunkCrc(payload, typeOffset, dataLength, chunkName);",
]
required_smoke = [
    "AcceptsAncillaryAndMultipleIdatChunks();",
    "RejectsMissingIdat();",
    "RejectsDuplicateIhdr();",
    "RejectsCorruptedIntermediateChunkCrc();",
    "RejectsOversizedChunkLength();",
    "RejectsDataAfterIend();",
]

for token in required_source:
    assert token in source, f"missing production guard: {token}"
for token in required_smoke:
    assert token in smoke, f"missing regression smoke: {token}"

raster_start = source.index("public IngestedDrawingSheet2D IngestRaster")
raster_end = source.index("private static void RequirePayload", raster_start)
raster = source[raster_start:raster_end]
assert raster.index("ValidatePngChunkStream(payload);") < raster.index("format = RasterSheetFormat.Png;")
assert raster.index("format = RasterSheetFormat.Png;") < raster.index("var sheet = new DrawingSheet2D")
print("PASS: PNG chunk stream is validated before raster drawing/evidence publication")
