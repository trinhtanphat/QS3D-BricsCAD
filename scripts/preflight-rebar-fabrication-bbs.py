#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SCHEDULE = ROOT / "src/QS3D.Core/Rebar/RebarSchedule.cs"
CSV = ROOT / "src/QS3D.Core/Export/RebarCsvExporter.cs"
XLSX = ROOT / "src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs"
UI = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml"
errors = []

for path in (SCHEDULE, CSV, XLSX, UI):
    if not path.is_file():
        errors.append("missing BBS fabrication provenance source: " + str(path.relative_to(ROOT)))

row_tokens = (
    "FabricationStatus",
    "FabricationStandardCode",
    "FabricationDetailingRevision",
)

if SCHEDULE.is_file():
    text = SCHEDULE.read_text(encoding="utf-8")
    for token in row_tokens:
        if token not in text:
            errors.append("RebarSchedule.cs missing provenance token: " + token)

if CSV.is_file():
    text = CSV.read_text(encoding="utf-8")
    for token in (
        "FabricationStatus,FabricationStandardCode,FabricationDetailingRevision",
        "row.FabricationStatus",
        "row.FabricationStandardCode",
        "row.FabricationDetailingRevision",
    ):
        if token not in text:
            errors.append("RebarCsvExporter.cs missing provenance output: " + token)

if XLSX.is_file():
    text = XLSX.read_text(encoding="utf-8")
    for token in (
        '"Fabrication Status", "Standard Code", "Detailing Revision"',
        'var range = "A1:O"',
        "row.FabricationStatus",
        "row.FabricationStandardCode",
        "row.FabricationDetailingRevision",
    ):
        if token not in text:
            errors.append("XlsxRebarScheduleExporter.cs missing provenance output: " + token)

if UI.is_file():
    try:
        ET.parse(UI)
    except ET.ParseError as exc:
        errors.append("RebarScheduleWindow.xaml is not well-formed XML/XAML: " + str(exc))
    text = UI.read_text(encoding="utf-8")
    for token in (
        'Binding="{Binding FabricationStatus}"',
        'Binding="{Binding FabricationStandardCode}"',
        'Binding="{Binding FabricationDetailingRevision}"',
        'Text="PROVENANCE ≠ CODE COMPLIANCE • DOUBLE-CLICK TO LOCATE • EXPORT XLSX"',
    ):
        if token not in text:
            errors.append("RebarScheduleWindow.xaml missing provenance/review boundary: " + token)

print("QS3D BBS fabrication provenance preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: BBS rows, CSV, XLSX and review UI carry fabrication provenance while explicitly avoiding a code-compliance claim.")
