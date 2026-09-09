#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.Core/Export/CoordinationWorkbook.cs"
SMOKE = ROOT / "tests/QS3D.Core.SmokeTests/CoordinationWorkbookSmoke.cs"
REGISTRATION = ROOT / "tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs"

text = SOURCE.read_text(encoding="utf-8")
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
registration = REGISTRATION.read_text(encoding="utf-8")

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

if "private static string BuildClashSheet(" in text or "private static string BuildTraceSheet(" in text:
    missing.append("remove whole-sheet string materialization")
if "var sb = BeginSheet(" in text or "new StringBuilder()" in text:
    missing.append("remove unbounded worksheet StringBuilder path")
if "new FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None)" in text and "BoundedArchiveWriteStream" not in text:
    missing.append("bound physical XLSX archive growth before validation")

for token, label in {
    "RejectsSourceCardinalityAboveBoundBeforeArchiveCommit": "registered source-cardinality RED regression",
    "RejectsOversizedWorksheetBeforeArchiveCommit": "hostile oversized-worksheet regression",
    "destination sentinel": "destination atomicity assertions",
    "owned temp": "owned-temp cleanup assertions",
}.items():
    if token not in smoke:
        missing.append(label)

if "CoordinationWorkbookSmoke.Run();" not in registration:
    missing.append("smoke registration")

if missing:
    raise SystemExit(
        "ERROR: legacy Coordination XLSX export remains unbounded before package validation:\n - "
        + "\n - ".join(missing)
    )

print("PASS: legacy Coordination XLSX writer is bounded before package materialization")
