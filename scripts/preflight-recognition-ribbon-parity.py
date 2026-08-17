#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionRibbonAugmenter.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "RecognitionTopbarCommands.cs"
INIT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
ICONS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapIconAugmenter.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def main() -> int:
    ribbon = RIBBON.read_text(encoding="utf-8")
    commands = COMMANDS.read_text(encoding="utf-8")
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

    # No recognition action may be clickable until a production workflow with the same semantics exists.
    require(ribbon, "bool enabled = false", "fail-closed recognition button default")
    for disabled_id in (
        "QS3D_RECOGNIZE_BLT_RESTORE",
        "QS3D_RECOGNIZE_BLT_TEXT",
        "QS3D_RECOGNIZE_BLT_OPTIONS",
        "QS3D_RECOGNIZE_BLT_TABLE",
        "QS3D_RECOGNIZE_BLT_BOUNDARY",
        "QS3D_RECOGNIZE_BLT_LABEL",
        "QS3D_RECOGNIZE_BLT_AUTO",
        "QS3D_RECOGNIZE_BLT_VALIDATE",
    ):
        marker = f'"{disabled_id}"'
        pos = ribbon.find(marker)
        if pos < 0:
            raise SystemExit(f"FAIL: {disabled_id} missing from recognition parity surface")
        if "enabled: true" in ribbon[pos : pos + 340]:
            raise SystemExit(f"FAIL: {disabled_id} must remain fail-closed until a matching recognition workflow exists")

    # Dedicated-looking recognition adapters must not redirect to unrelated QS3D workflows.
    for adapter in (
        "QS3DRECOGNITIONRESTORE",
        "QS3DRECOGNITIONOPTIONS",
        "QS3DRECOGNITIONBOUNDARY",
        "QS3DRECOGNITIONLABEL",
        "QS3DRECOGNITIONAUTO",
    ):
        require(commands, f'CommandMethod("{adapter}"', f"recognition adapter {adapter}")

    for unrelated in ("QS3DINSPECT", "QS3DMEPREVIEW", "QS3DTAKEOFF", "SendStringToExecute"):
        if unrelated in commands:
            raise SystemExit(f"FAIL: recognition adapters must fail closed, not dispatch unrelated workflow token: {unrelated}")
    require(commands, "chưa có workflow nhận dạng tương ứng", "fail-closed recognition adapter message")

    require(init, "BltRecognitionRibbonAugmenter.TryInitialize()", "recognition parity initialization")
    require(init, "BltRecognitionRibbonAugmenter.Reset()", "recognition parity reset")

    # RibbonBootstrapIconAugmenter now uses a single recursive DecorateItem traversal rather than
    # the older ApplyIconsToCollection helper. Guard behavior/recursion, not a retired helper name.
    require(icons, "DecorateItem(item, visited, ref commandButtons)", "nested-row icon traversal entry")
    require(icons, "DecorateItem(child, visited, ref commandButtons)", "nested-row recursive traversal")
    require(icons, 'GetProperty(item, "Items")', "nested-row readiness traversal")
    require(icons, "HasCompleteVisibleIcon(item)", "preserve richer recognition artwork")

    print(
        "PASS: NHẬN DẠNG topbar keeps BLT3D reference labels/layout/artwork while every unsupported "
        "recognition action stays disabled and its command adapter fails closed without unrelated dispatch."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
