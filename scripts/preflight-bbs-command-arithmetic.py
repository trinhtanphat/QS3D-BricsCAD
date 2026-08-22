#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/Commands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/RebarScheduleWindow.xaml.cs"
EXPORTER = ROOT / "src/QS3D.Core/Export/XlsxRebarScheduleExporter.cs"
errors = []

for path in (COMMANDS, WINDOW, EXPORTER):
    if not path.is_file():
        errors.append("missing " + str(path.relative_to(ROOT)))

if COMMANDS.is_file():
    text = COMMANDS.read_text(encoding="utf-8")
    required = (
        'var totalWeight = 0d;',
        'QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")',
        'if (row == null) throw new InvalidOperationException("BBS không được chứa dòng null.");',
    )
    for token in required:
        if token not in text:
            errors.append("QS3DBBS missing safe aggregate token: " + token)
    if "rows.Sum(x => x.TotalWeightKg)" in text:
        errors.append("QS3DBBS must not use unchecked LINQ Sum for total weight")
    total_index = text.find('QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS command total weight")')
    dialog_index = text.find('Title = "Xuất Bar Bending Schedule"')
    export_index = text.find("XlsxRebarScheduleExporter.Export(dialog.FileName, rows);")
    if min(total_index, dialog_index, export_index) >= 0 and not total_index < dialog_index < export_index:
        errors.append("QS3DBBS must validate aggregate weight before opening/writing the export")

if WINDOW.is_file():
    text = WINDOW.read_text(encoding="utf-8")
    if 'QuantityReportMath.Add(totalWeightKg, row.TotalWeightKg, "BBS visible total weight")' not in text:
        errors.append("BBS modeless visible total must remain on QuantityReportMath.Add")

if EXPORTER.is_file():
    text = EXPORTER.read_text(encoding="utf-8")
    if "double.IsNaN(value) || double.IsInfinity(value)" not in text:
        errors.append("BBS XLSX exporter must reject non-finite numeric cells")
    if "AtomicFileCommit.ReplaceWithoutBackup" not in text:
        errors.append("BBS XLSX exporter must retain atomic final-file replacement")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] QS3DBBS validates finite aggregate weight before export and matches modeless BBS arithmetic")
