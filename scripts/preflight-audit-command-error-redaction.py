#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/AuditCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing AuditCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        '[CommandMethod("QS3DAUDIT", CommandFlags.Modal)]',
        'ProjectContextCoordinator.TryGetReadOnly(document, out var project)',
        '_window = new AuditLogWindow(document);',
        '_window.Closed += (_, __) => _window = null;',
        'Application.ShowModelessWindow(IntPtr.Zero, _window, true);',
        'Đã mở Nhật ký thay đổi • chưa có QS3D project hiện hữu; không tạo project mới.',
        'const string status = "Nhật ký thay đổi lỗi: không thể mở nhật ký thay đổi.";',
        'document.Editor.WriteMessage("\\nQS3DAUDIT error: không thể mở nhật ký thay đổi.")',
        'PaletteCoordinator.SetStatus(status)',
    )
    for token in required:
        if token not in text:
            errors.append("Audit command contract missing token: " + token)

    forbidden = (
        'catch (System.Exception ex)',
        'ex.Message',
        'QS3DAUDIT error: " +',
        'Nhật ký thay đổi lỗi: " +',
    )
    for token in forbidden:
        if token in text:
            errors.append("Audit command must not reflect exception detail: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DAUDIT keeps read-only/modeless behavior and protected failure sinks without reflecting exception details.")
