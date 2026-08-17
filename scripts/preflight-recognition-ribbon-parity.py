#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RIBBON = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltRecognitionRibbonAugmenter.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "RecognitionTopbarCommands.cs"
REVIEW = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReviewCommands.cs"
INIT = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
ICONS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonBootstrapIconAugmenter.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def section(text: str, start: str, end: str, label: str) -> str:
    begin = text.find(start)
    if begin < 0:
        raise SystemExit(f"FAIL: {label} missing start marker: {start}")
    finish = text.find(end, begin + len(start))
    if finish < 0:
        raise SystemExit(f"FAIL: {label} missing end marker: {end}")
    return text[begin:finish]


def require_button_state(ribbon: str, button_id: str, enabled: bool) -> None:
    marker = f'"{button_id}"'
    pos = ribbon.find(marker)
    if pos < 0:
        raise SystemExit(f"FAIL: {button_id} missing from recognition parity surface")
    snippet = ribbon[pos : pos + 360]
    expected = "enabled: true" if enabled else "enabled: false"
    if expected not in snippet:
        raise SystemExit(f"FAIL: {button_id} must declare {expected}")


def main() -> int:
    ribbon = RIBBON.read_text(encoding="utf-8")
    commands = COMMANDS.read_text(encoding="utf-8")
    review = REVIEW.read_text(encoding="utf-8")
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

    for disabled_id in (
        "QS3D_RECOGNIZE_BLT_RESTORE",
        "QS3D_RECOGNIZE_BLT_TEXT",
        "QS3D_RECOGNIZE_BLT_OPTIONS",
        "QS3D_RECOGNIZE_BLT_TABLE",
        "QS3D_RECOGNIZE_BLT_LABEL",
        "QS3D_RECOGNIZE_BLT_VALIDATE",
    ):
        require_button_state(ribbon, disabled_id, False)
    for enabled_id in (
        "QS3D_RECOGNIZE_BLT_BOUNDARY",
        "QS3D_RECOGNIZE_BLT_AUTO",
    ):
        require_button_state(ribbon, enabled_id, True)

    for adapter in (
        "QS3DRECOGNITIONRESTORE",
        "QS3DRECOGNITIONOPTIONS",
        "QS3DRECOGNITIONBOUNDARY",
        "QS3DRECOGNITIONLABEL",
        "QS3DRECOGNITIONAUTO",
    ):
        require(commands, f'CommandMethod("{adapter}"', f"recognition adapter {adapter}")

    restore = section(commands, "public void RestoreSelected()", '[CommandMethod("QS3DRECOGNITIONOPTIONS")]', "restore adapter")
    options = section(commands, "public void RecognitionOptions()", '[CommandMethod("QS3DRECOGNITIONBOUNDARY"', "options adapter")
    boundary = section(commands, "public void SelectBoundary()", '[CommandMethod("QS3DRECOGNITIONLABEL"', "boundary adapter")
    label = section(commands, "public void SelectLabel()", '[CommandMethod("QS3DRECOGNITIONAUTO"', "label adapter")
    auto = section(commands, "public void AutoRecognize()", "private static void WriteUnavailable", "auto adapter")

    for name, adapter in (("restore", restore), ("options", options), ("label", label)):
        if "Queue(document" in adapter or "SendStringToExecute" in adapter:
            raise SystemExit(f"FAIL: {name} recognition adapter must fail closed instead of dispatching another workflow")
        require(adapter, "WriteUnavailable", f"{name} fail-closed adapter")

    require(boundary, 'Queue(document, "QS3DRECOGNIZE")', "boundary recognition route")
    require(auto, 'Queue(document, "QS3DRECOGNIZEAUTO")', "auto recognition route")
    require(review, '[CommandMethod("QS3DRECOGNIZE", CommandFlags.UsePickSet)]', "manual recognition workflow")
    require(review, '[CommandMethod("QS3DRECOGNIZEAUTO", CommandFlags.UsePickSet)]', "atomic auto-recognition workflow")

    for unrelated in ("QS3DINSPECT", "QS3DMEPREVIEW", "QS3DTAKEOFF"):
        if unrelated in commands:
            raise SystemExit(f"FAIL: recognition topbar adapter must not dispatch unrelated workflow token: {unrelated}")
    require(commands, "chưa có workflow nhận dạng tương ứng", "fail-closed recognition message")

    require(init, "BltRecognitionRibbonAugmenter.TryInitialize()", "recognition parity initialization")
    require(init, "BltRecognitionRibbonAugmenter.Reset()", "recognition parity reset")
    require(icons, "DecorateItem(item, visited, ref commandButtons)", "nested-row icon traversal entry")
    require(icons, "DecorateItem(child, visited, ref commandButtons)", "nested-row recursive traversal")
    require(icons, 'GetProperty(item, "Items")', "nested-row readiness traversal")
    require(icons, "HasCompleteVisibleIcon(item)", "preserve richer recognition artwork")

    print(
        "PASS: NHẬN DẠNG topbar keeps BLT3D layout/artwork, disables unsupported Restore/Options/Label, "
        "and routes only Boundary/Auto through matching production recognition workflows."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
