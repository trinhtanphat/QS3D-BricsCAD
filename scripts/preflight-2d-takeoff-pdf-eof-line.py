from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/QS3D.Core/BenchmarkParity/Qs2DSheetIngestion.cs").read_text(encoding="utf-8")
smoke = (root / "tests/QS3D.Core.SmokeTests/Qs2DSheetIngestionPdfHeaderSmoke.cs").read_text(encoding="utf-8")

required_source = [
    "var markerStart = end - PdfEofMarker.Length + 1;",
    "if (markerStart == 0 || !IsPdfLineTerminator(payload[markerStart - 1]))",
    "PDF terminal %%EOF marker must begin on its own line.",
]
for token in required_source:
    if token not in source:
        raise SystemExit(f"missing PDF EOF-line guard token: {token}")

if source.index("if (markerStart == 0 || !IsPdfLineTerminator(payload[markerStart - 1]))") < source.index("for (var i = 0; i < PdfEofMarker.Length; i++)"):
    raise SystemExit("PDF EOF line-boundary validation must follow exact terminal marker validation")

required_smoke = [
    "AcceptsCanonicalEofLineAndTrailingWhitespace();",
    "RejectsInlineEofAfterPdfData();",
    "RejectsSpaceIndentedEofMarker();",
]
for token in required_smoke:
    if token not in smoke:
        raise SystemExit(f"missing PDF EOF-line smoke coverage: {token}")

print("2D takeoff PDF EOF-line preflight: PASS")
