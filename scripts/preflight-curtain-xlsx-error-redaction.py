#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/CurtainWallScheduleCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing CurtainWallScheduleCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DCURTAINXLSX", CommandFlags.Modal)]',
        'if (dialog.ShowDialog() != true) return;',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        'Report(document, "Curtain XLSX: BLOCKED • cần một QS3D project hiện hữu; lệnh export không tạo project mới.");',
        'ProjectStateSnapshot.CreateDetachedCopy(project)',
        'RegenerateDirty(snapshot)',
        'CurtainWallScheduleBuilder.Build(snapshot)',
        'QuantityReportMath.AddCount(panels, row.PanelCount)',
        'glass.Add(row.NetGlassAreaM2, "Curtain export net glass area")',
        'frame.Add(row.FrameLengthM, "Curtain export frame length")',
        'glass.Value("Curtain export net glass area")',
        'frame.Value("Curtain export frame length")',
        'CurtainWallXlsxExporter.Export(dialog.FileName, rows);',
        'Report(document, "QS3DCURTAINXLSX lỗi: không thể xuất bảng Vách Kính.");',
        'document.Editor.WriteMessage("\\n[QS3D] Cảnh báo UI sau export: không thể cập nhật giao diện sau khi file đã được xuất.")',
    )
    for token in required:
        if token not in text:
            errors.append("Curtain XLSX error-redaction contract missing token: " + token)

    for token in ('throw new InvalidOperationException("Curtain XLSX cần', 'catch (System.Exception ex)', 'ex.Message', 'QS3DCURTAINXLSX lỗi: " +'):
        if token in text:
            errors.append("Curtain XLSX must not depend on/reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    sys.exit(1)

print("PASS: Curtain XLSX preserves detached validated export and explicit blocked guidance without reflecting exception details.")
