#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/MaterialUsageScheduleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing MaterialUsageScheduleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DMATERIALXLSX", CommandFlags.Modal)]',
        'if (dialog.ShowDialog() != true) return;',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'Report(document, "Material XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");',
        'ProjectStateSnapshot.CreateDetachedCopy(project)',
        'RegenerateDirty(snapshot)',
        'MaterialUsageScheduleBuilder.Build(snapshot)',
        'QuantityReportMath.AddCount(elements, row.ElementCount)',
        'MaterialUsageXlsxExporter.Export(dialog.FileName, rows);',
        'Report(document, "QS3DMATERIALXLSX lỗi: không thể xuất bảng vật liệu.");',
        'document.Editor.WriteMessage("\\n[QS3D] Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.")',
        '// Export has already committed; UI reporting is best effort only.',
    )
    for token in required:
        if token not in text:
            errors.append("Material XLSX error-redaction contract missing token: " + token)

    forbidden = (
        'throw new InvalidOperationException("Material XLSX cần',
        'catch (System.Exception ex)',
        'ex.Message',
        'QS3DMATERIALXLSX lỗi: " +',
        'Cảnh báo UI sau export: " +',
    )
    for token in forbidden:
        if token in text:
            errors.append("Material XLSX must not depend on/refelect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Material XLSX keeps explicit blocked guidance, detached validated export and post-commit UI best effort without reflecting exception details.")
