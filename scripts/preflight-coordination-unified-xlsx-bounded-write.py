#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CoordinationUnifiedWorkbook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CoordinationUnifiedWorkbookSmoke.cs"
text = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""

required = {
    "writer-side worksheet row bound": "MaxExportDataRowsPerSheet",
    "writer-side XML entry bound": "MaxExportXmlEntryBytes",
    "writer-side aggregate XML bound": "MaxExportXmlTotalBytes",
    "writer-side final archive bound": "MaxExportWorkbookBytes",
    "bounded archive stream": "BoundedArchiveWriteStream",
    "streamed worksheet writer": "WriteSheet",
    "aggregate budget reservation": "ReserveExportXmlBytes",
}

missing = [label for label, token in required.items() if token not in text]
if "private static string BuildSheet(" in text or "var sb = new StringBuilder();" in text or "var builder = new StringBuilder();" in text:
    missing.append("remove unbounded whole-sheet StringBuilder materialization")

if "new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)" in text and "BoundedArchiveWriteStream" not in text:
    missing.append("bound temporary XLSX archive growth before validation")

for token, label in {
    "RejectsOversizedWorksheetBeforeArchiveCommit": "hostile oversized-worksheet regression",
    "destination sentinel": "destination atomicity assertion",
    "owned temp": "owned-temp cleanup assertion",
}.items():
    if token not in smoke:
        missing.append(label)

if missing:
    raise SystemExit(
        "ERROR: Coordination Unified XLSX export remains unbounded before package validation:\n - "
        + "\n - ".join(missing)
    )

print("PASS: Coordination Unified XLSX writer is bounded before package materialization")
