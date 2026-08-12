#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
FILES = {
    "RoomFinishScheduleCommands.cs": ("QS3DFINISHXLSX lỗi: không thể xuất bảng hoàn thiện phòng.", "HT_Phòng XLSX: project chưa có finish semantic để xuất."),
    "DoorOpeningScheduleCommands.cs": ("QS3DDOORXLSX lỗi: không thể xuất bảng Cửa / Lỗ mở.", "Door XLSX: project chưa có Cửa/Lỗ mở semantic để xuất."),
    "CurtainWallScheduleCommands.cs": ("QS3DCURTAINXLSX lỗi: không thể xuất bảng Vách Kính.", "Curtain XLSX: chưa có Vách Kính semantic để xuất."),
}
errors = []

for name, (error_status, empty_status) in FILES.items():
    path = SRC / name
    if not path.is_file():
        errors.append("missing schedule export source: " + name)
        continue
    text = path.read_text(encoding="utf-8")
    for token in (
        "if (dialog.ShowDialog() != true) return;",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectStateSnapshot.CreateDetachedCopy(project)",
        "RegenerateDirty(snapshot)",
        "private static void FinalizeUi(Document document, string status, string fileName)",
        "private static void Report(Document document, string status)",
        "try { PaletteCoordinator.SetStatus(status); } catch { }",
        "try { document.Editor.WriteMessage(\"\\nQS3D \" + status); } catch { }",
        "catch (System.Exception)",
        'Report(document, "' + empty_status + '");',
        'Report(document, "' + error_status + '");',
    ):
        if token not in text:
            errors.append(name + " missing export safety token: " + token)
    for forbidden in ("ex.Message", "exception.Message", ".StackTrace"):
        if forbidden in text:
            errors.append(name + " must not expose path-bearing exception diagnostics: " + forbidden)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(name + " must not create/cache project state during schedule export.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Room Finish, Door/Opening and Curtain XLSX exports are cancel-first, read-only, detached-regeneration workflows with privacy-safe best-effort empty/error reporting.")
