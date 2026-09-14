from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPdfHeaderSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "payload.Length < 9",
    "!IsPdfLineTerminator(payload[8])",
    "private static bool IsPdfLineTerminator",
    "return value == 10 || value == 13;",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing PDF header-line guard: {token}")

header_guard = source.index("!IsPdfLineTerminator(payload[8])")
eof_scan = source.index("var end = payload.Length - 1;")
if header_guard > eof_scan:
    raise SystemExit("PDF header-line guard must execute before terminal EOF admission checks")

required_smoke = [
    "AcceptsLfTerminatedHeader",
    "AcceptsCrTerminatedHeader",
    "AcceptsCrLfTerminatedHeader",
    "RejectsInlineDataAfterVersion",
    "RejectsSpaceAfterVersion",
    "RejectsTruncatedVersionOnlyHeader",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing PDF header-line smoke coverage: {token}")

print("2D takeoff PDF header-line preflight: PASS")
