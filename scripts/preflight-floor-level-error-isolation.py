#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/UI/FloorLevelWindow.xaml.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    'ReportFailure("Lưu tầng"',
    'ReportFailure("Xóa tầng"',
    'ReportFailure("Đặt tầng active"',
    'ReportFailure("Gán tầng"',
    'ReportFailure(operation',
    'ReportFailure("Kiểm tra selection"',
    'ReportFailure("Đọc Floor/Level"',
    'ReportPostCommitWarning(successMessage, context)',
    'private void ReportFailure(string operation)',
    'private void ReportPostCommitWarning(string successMessage, string context)',
]
for token in required:
    if token not in text:
        raise SystemExit(f"Floor Level error-isolation guard missing token: {token}")

for forbidden in (
    'ex.Message',
    'refreshError.Message',
    '.StackTrace',
    'InnerException.Message',
):
    if forbidden in text:
        raise SystemExit(f"Floor Level UI must not publish raw exception detail: {forbidden}")

stable_failure = 'không hoàn tất. Không có thay đổi chưa xác nhận nào được giữ lại; hãy Refresh Level Picker và thử lại.'
stable_sync = 'đã commit; đồng bộ UI chưa hoàn tất. Hãy Refresh Level Picker.'
for token in (stable_failure, stable_sync):
    if token not in text:
        raise SystemExit(f"Floor Level stable diagnostic missing: {token}")

# Preserve the fail-closed product boundaries this package must not weaken.
for token in (
    'ProjectStateSnapshot.Capture(project)',
    'RestoreOrThrow(project, rollback, operationError',
    'RequireBoundProjectForMutation(',
    'RequireBoundProjectForRead(',
    'EnsureBoundDrawingIsActive(',
    'SemanticSelectionResolver.ResolveImplied(_document, previewProject)',
    'SequenceEqual(currentIds, StringComparer.OrdinalIgnoreCase)',
):
    if token not in text:
        raise SystemExit(f"Floor Level safety boundary missing after error isolation: {token}")

print("PASS Floor Level UI error isolation")
