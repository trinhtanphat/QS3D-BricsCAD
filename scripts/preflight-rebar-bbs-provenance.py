#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(path: str) -> str:
    target = ROOT / path
    if not target.exists():
        print(f"[FAIL] missing {path}")
        sys.exit(1)
    return target.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        print(f"[FAIL] {label}: missing {token}")
        sys.exit(1)


schedule = read("src/QS3D.Core/Rebar/RebarSchedule.cs")
csv = read("src/QS3D.Core/Export/RebarCsvExporter.cs")
xlsx = read("src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs")
smoke = read("tests/QS3D.Core.SmokeTests/BbsRegressionSmoke.cs")

for token in [
    "public string FabricationStatus { get; set; } = string.Empty;",
    "public string FabricationStandardCode { get; set; } = string.Empty;",
    "public string FabricationDetailingRevision { get; set; } = string.Empty;",
    "RebarFabricationQualificationHealthService.StatusPropertyKey",
    "RebarFabricationQualificationHealthService.StandardCodePropertyKey",
    "RebarFabricationQualificationHealthService.DetailingRevisionPropertyKey",
    "FabricationStatus = fabricationStatus",
    "FabricationStandardCode = fabricationStandardCode",
    "FabricationDetailingRevision = fabricationDetailingRevision",
]:
    require(schedule, token, "BBS schedule provenance")

for token in [
    "FabricationStatus,FabricationStandardCode,FabricationDetailingRevision",
    ".Append(Q(row.FabricationStatus))",
    ".Append(Q(row.FabricationStandardCode))",
    ".Append(Q(row.FabricationDetailingRevision))",
]:
    require(csv, token, "BBS CSV provenance")

for token in [
    '"Fabrication Status", "Standard Code", "Detailing Revision"',
    'var range = "A1:O"',
    "AppendText(sb, CellRef(12, r), row.FabricationStatus, 0);",
    "AppendText(sb, CellRef(13, r), row.FabricationStandardCode, 0);",
    "AppendText(sb, CellRef(14, r), row.FabricationDetailingRevision, 0);",
]:
    require(xlsx, token, "BBS XLSX provenance")

for token in [
    "FabricationProvenanceFlowsToExports();",
    'const string standard = "STD-X";',
    'const string revision = "REV-A";',
    "RebarFabricationQualificationHealthService.StatusPropertyKey",
    "RebarCsvExporter.ToCsv(rows)",
    "XlsxRebarScheduleExporter.Export(path, rows)",
    'Require(sheet, "Fabrication Status");',
    'Require(sheet, "Standard Code");',
    'Require(sheet, "Detailing Revision");',
]:
    require(smoke, token, "BBS provenance regression")

print("[PASS] fabrication qualification provenance is statically guarded from semantic rebar properties through BBS rows into CSV and XLSX exports")
