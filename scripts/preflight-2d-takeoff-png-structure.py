from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs"
smoke = root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPngStructureSmoke.cs"
source_text = source.read_text(encoding="utf-8")
smoke_text = smoke.read_text(encoding="utf-8")

for token in (
    "ReadInt32BigEndian(payload, 8) != 13",
    "PNG first chunk must be an IHDR chunk with length 13.",
    "payload[12] != (byte)'I'",
    "PNG first chunk must be IHDR.",
    "PNG payload is truncated before the complete IHDR chunk.",
):
    if token not in source_text:
        raise SystemExit(f"2D takeoff PNG structure preflight: missing source guard: {token}")

for token in (
    "[ModuleInitializer]",
    "AcceptsCanonicalIhdr",
    "RejectsWrongFirstChunkType",
    "RejectsWrongIhdrLength",
    "RejectsTruncatedIhdr",
):
    if token not in smoke_text:
        raise SystemExit(f"2D takeoff PNG structure preflight: missing smoke coverage: {token}")

print("2D takeoff PNG IHDR structure preflight passed.")
