#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/RoomFinishScheduleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing RoomFinishScheduleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DFINISHXLSX", CommandFlags.Modal)]',
        'if (dialog.ShowDialog() != true) return;',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'Report(document, "HT_Phòng XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");',
        'ProjectStateSnapshot.CreateDetachedCopy(project)',
        'RegenerateDirty(snapshot)',
        'RoomFinishScheduleBuilder.Build(snapshot)',
        'QuantityReportMath.AddCount(count, row.Count)',
        'QuantityReportMath.NonNegative(row.PrimaryQuantity, "HT_Phòng export primary quantity")',
        'primaryAccumulator.Value("HT_Phòng export primary quantity")',
        'RoomFinishXlsxExporter.Export(dialog.FileName, rows);',
        'Report(document, "QS3DFINISHXLSX lỗi: không thể xuất bảng hoàn thiện phòng.");',
        'document.Editor.WriteMessage("\\n[QS3D] Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.")',
    )
    for token in required:
        if token not in text:
            errors.append("Room Finish XLSX error-redaction contract missing token: " + token)

    for token in ('throw new InvalidOperationException("HT_Phòng XLSX cần', 'catch (System.Exception ex)', 'ex.Message', 'QS3DFINISHXLSX lỗi: " +'):
        if token in text:
            errors.append("Room Finish XLSX must not depend on/reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Room Finish XLSX preserves detached validated export and explicit blocked guidance without reflecting exception details.")
