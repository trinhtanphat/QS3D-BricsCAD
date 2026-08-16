#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionRibbonAugmenter.cs"
INIT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
ICONS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapIconAugmenter.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def main() -> int:
    ribbon = RIBBON.read_text(encoding="utf-8")
    init = INIT.read_text(encoding="utf-8")
    icons = ICONS.read_text(encoding="utf-8")

    require(ribbon, 'RecognitionTabId = "QS3D_RECOGNIZE"', "recognition tab ownership")
    require(ribbon, '"Nhận dạng"', "reference group caption")
    require(ribbon, '"Dầm"', "beam group caption")

    for label in (
        "Khôi phục đã chọn",
        "Nhận dạng chữ",
        "Tùy chọn nhận dạng",
        "Bảng biểu phần tử",
        "Chọn đường biên",
        "Chọn nhãn",
        "Tự động nhận dạng",
        "Xác định Kiểm tra",
    ):
        require(ribbon, f'"{label}"', f"reference label {label}")

    require(ribbon, 'Create("Bricscad.Windows.RibbonRowPanel")', "compact stacked columns")
    require(ribbon, 'Create("Bricscad.Windows.RibbonRowBreak")', "three-row stacking")
    require(ribbon, 'SetEnumProperty(button, "Size", "Standard")', "small-with-text density")
    require(ribbon, 'SetProperty(button, "ShowImage", true)', "reference icon visibility")
    require(ribbon, 'SetProperty(button, "IsEnabled", spec.Enabled)', "reference disabled states")

    # The screenshot intentionally shows these commands greyed out.
    for disabled_id in (
        "QS3D_RECOGNIZE_BLT_TEXT",
        "QS3D_RECOGNIZE_BLT_TABLE",
        "QS3D_RECOGNIZE_BLT_VALIDATE",
    ):
        marker = f'"{disabled_id}"'
        pos = ribbon.find(marker)
        if pos < 0 or "enabled: false" not in ribbon[pos : pos + 340]:
            raise SystemExit(f"FAIL: {disabled_id} must remain visually disabled")

    require(init, "BltRecognitionRibbonAugmenter.TryInitialize()", "recognition parity initialization")
    require(init, "BltRecognitionRibbonAugmenter.Reset()", "recognition parity reset")
    require(icons, "ApplyIconsToCollection", "nested-row icon traversal")
    require(icons, 'GetProperty(item, "Items")', "nested-row readiness traversal")

    print(
        "PASS: NHẬN DẠNG topbar keeps the BLT3D reference labels, two compact groups, "
        "stacked small-icon rows, disabled-state parity, and nested ribbon readiness."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
