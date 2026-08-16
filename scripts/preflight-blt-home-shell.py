#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def main():
    ribbon_rel = "src/QS3D.BricsCAD.V25/Ribbon/BltHomeRibbonAugmenter.cs"
    activation_rel = "src/QS3D.BricsCAD.V25/Ribbon/HomeTabActivationCoordinator.cs"
    init_rel = "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"
    shell_rel = "src/QS3D.BricsCAD.V25/UI/BltStartCenterWindow.cs"
    command_rel = "src/QS3D.BricsCAD.V25/StartCenterCommands.cs"

    ribbon = read(ribbon_rel)
    for needle in (
        '"Dự án"',
        '"Mở..."',
        '"Lưu"',
        '"Lưu thành..."',
        '"Cấu hình"',
        '"Cài đặt"',
        '"Đối tượng\\nhệ thống"',
        'SetProperty(button, "ShowImage", true)',
        'SetProperty(button, "LargeImage", image)',
        'ProjectPanelSourceId',
        'ConfigPanelSourceId',
    ):
        require(ribbon, needle, ribbon_rel)

    activation = read(activation_rel)
    for needle in ('HomeTabId = "QS3D_HOME"', '"QS3DSTART "', 'DispatcherTimer'):
        require(activation, needle, activation_rel)

    init = read(init_rel)
    require(init, "BltHomeRibbonAugmenter.TryInitialize()", init_rel)
    require(init, "HomeTabActivationCoordinator.TryInitialize()", init_rel)
    require(init, "HomeTabActivationCoordinator.Stop()", init_rel)

    shell = read(shell_rel)
    for needle in (
        'Text = "QS3D"',
        'BIM Modeling & Quantity Application',
        'Text = "QUY TRÌNH NHANH"',
        '"Tạo dự án mới"',
        '"Mở tệp dự án..."',
        '"Lưu thành..."',
        'Text = "DỰ ÁN GẦN ĐÂY"',
        'StatusButton("Mô hình", "QS3D")',
        'StatusButton("BQ", "QS3DBQ")',
        'StatusItem("○ Nền sáng")',
        'StatusItem("◐ Tương phản")',
        'StatusItem("⌞ Vuông góc")',
        'StatusItem("⌖ Bắt điểm", highlighted: true)',
        'StartCenterUserStateStore.GetSnapshot().RecentProjects',
    ):
        require(shell, needle, shell_rel)

    command = read(command_rel)
    require(command, "createdWindow = new BltStartCenterWindow();", command_rel)

    print("PASS: BLT3D-familiar KHỞI ĐẦU ribbon/icons/dividers, start shell and bottom bar are source-guarded.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
