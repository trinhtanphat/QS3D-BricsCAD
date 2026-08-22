#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/BbsCsvCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing BbsCsvCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DBBSCSV", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'ProjectStateSnapshot.CreateDetachedCopy(project)',
        'RegenerateDirty(snapshot)',
        'ProjectRebarScheduleBuilder.Build(snapshot)',
        'QuantityReportMath.Add(totalWeight, row.TotalWeightKg, "BBS CSV total weight")',
        'if (dialog.ShowDialog() != true) return;',
        'RebarCsvExporter.Export(dialog.FileName, rows);',
        'Report(document, "QS3DBBSCSV lỗi: không thể xuất BBS CSV.");',
        'document.Editor.WriteMessage("\\n[QS3D] Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.")',
        '// Export has already committed; UI reporting is best effort only.',
    )
    for token in required:
        if token not in text:
            errors.append("BBS CSV error-redaction contract missing token: " + token)

    forbidden = (
        'catch (System.Exception ex)',
        'ex.Message',
        'QS3DBBSCSV lỗi: " +',
        'Cảnh báo UI sau export: " +',
    )
    for token in forbidden:
        if token in text:
            errors.append("BBS CSV must not reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: BBS CSV keeps detached validated export and best-effort post-commit UI behavior without reflecting exception details.")
