from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPdfHeaderSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "!IsSupportedPdfVersion(payload[5], payload[7])",
    "private static bool IsSupportedPdfVersion(byte major, byte minor)",
    "major == (byte)'1' && minor >= (byte)'0' && minor <= (byte)'7'",
    "major == (byte)'2' && minor == (byte)'0'",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing PDF version admission guard: {token}")

version_guard = source.index("!IsSupportedPdfVersion(payload[5], payload[7])")
eof_scan = source.index("var end = payload.Length - 1;")
if version_guard > eof_scan:
    raise SystemExit("PDF version admission must execute before terminal EOF admission checks")

required_smoke = [
    "AcceptsPdf10And20",
    "RejectsUnsupportedPdfVersions",
    'Ingest("%PDF-1.0\\n%%EOF")',
    'Ingest("%PDF-2.0\\n%%EOF")',
    'Ingest("%PDF-1.8\\n%%EOF")',
    'Ingest("%PDF-2.1\\n%%EOF")',
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing PDF version smoke coverage: {token}")

print("2D takeoff PDF version preflight: PASS")
