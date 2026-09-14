from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionJpegMarkerSmoke.cs").read_text(encoding="utf-8")

for token in (
    "JPEG byte-stuffing marker is invalid before entropy-coded scan data.",
    "JPEG restart markers are invalid before entropy-coded scan data.",
    "JPEG payload contains a nested SOI marker before SOF.",
    "JPEG payload reached EOI before a supported SOF marker.",
    "JPEG payload reached SOS before a supported SOF marker.",
    "IsRestartMarker",
    "if (marker == 0x01)",
):
    if token not in source:
        raise SystemExit(f"JPEG marker-stream preflight: missing production token: {token}")

for token in (
    "AcceptsTemBeforeSof",
    "RejectsStuffedMarkerBeforeSof",
    "RejectsRestartMarkerBeforeSof",
    "RejectsNestedSoiBeforeSof",
    "RejectsPrematureEoiBeforeSof",
    "RejectsPrematureSosBeforeSof",
    "RejectsRawDataBeforeSof",
):
    if token not in smoke:
        raise SystemExit(f"JPEG marker-stream preflight: missing smoke token: {token}")

if source.index("if (marker == 0x00)") > source.index("if (IsStartOfFrame(marker))"):
    raise SystemExit("JPEG marker-stream preflight: marker semantics must be validated before SOF dimensions.")

print("2D takeoff JPEG marker-stream preflight passed.")
