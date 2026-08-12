#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/DoorOpeningScheduleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing DoorOpeningScheduleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DDOORXLSX", CommandFlags.Modal)]',
        'if (dialog.ShowDialog() != true) return;',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'Report(document, "Door XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");',
        'ProjectStateSnapshot.CreateDetachedCopy(project)',
        'RegenerateDirty(snapshot)',
        'DoorOpeningScheduleBuilder.Build(snapshot)',
        'QuantityReportMath.AddCount(count, row.Count)',
        'QuantityReportMath.Add(area, row.OpeningAreaM2, "Door/Opening export area")',
        'rows.SelectMany(x => x.HostIds).Distinct(StringComparer.OrdinalIgnoreCase).Count()',
        'DoorOpeningXlsxExporter.Export(dialog.FileName, rows);',
        'Report(document, "QS3DDOORXLSX lỗi: không thể xuất bảng Cửa / Lỗ mở.");',
        'document.Editor.WriteMessage("\\n[QS3D] Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.")',
    )
    for token in required:
        if token not in text:
            errors.append("Door XLSX error-redaction contract missing token: " + token)

    for token in ('throw new InvalidOperationException("Door XLSX cần', 'catch (System.Exception ex)', 'ex.Message', 'QS3DDOORXLSX lỗi: " +'):
        if token in text:
            errors.append("Door XLSX must not depend on/reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Door XLSX preserves detached validated export and explicit blocked guidance without reflecting exception details.")
