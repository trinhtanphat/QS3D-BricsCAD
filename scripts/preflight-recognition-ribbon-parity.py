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
    require(ribbon, "bool enabled = true", "supported recognition button default")

    # Only the reference-placeholder actions remain visually disabled.
    for disabled_id in (
        "QS3D_RECOGNIZE_BLT_TEXT",
        "QS3D_RECOGNIZE_BLT_TABLE",
        "QS3D_RECOGNIZE_BLT_VALIDATE",
    ):
        marker = f'"{disabled_id}"'
        pos = ribbon.find(marker)
        if pos < 0 or "enabled: false" not in ribbon[pos : pos + 340]:
            raise SystemExit(f"FAIL: {disabled_id} must remain visually disabled")

    # Supported/adapted actions must not be accidentally disabled by changing the default or
    # adding a local enabled:false override.
    for enabled_id in (
        "QS3D_RECOGNIZE_BLT_RESTORE",
        "QS3D_RECOGNIZE_BLT_OPTIONS",
        "QS3D_RECOGNIZE_BLT_BOUNDARY",
        "QS3D_RECOGNIZE_BLT_LABEL",
        "QS3D_RECOGNIZE_BLT_AUTO",
    ):
        marker = f'"{enabled_id}"'
        pos = ribbon.find(marker)
        if pos < 0:
            raise SystemExit(f"FAIL: {enabled_id} missing from recognition parity surface")
        if "enabled: false" in ribbon[pos : pos + 340]:
            raise SystemExit(f"FAIL: {enabled_id} must not be disabled by the recognition parity layer")

    for adapter in (
        "QS3DRECOGNITIONRESTORE",
        "QS3DRECOGNITIONOPTIONS",
        "QS3DRECOGNITIONBOUNDARY",
        "QS3DRECOGNITIONLABEL",
        "QS3DRECOGNITIONAUTO",
    ):
        require(commands, f'CommandMethod("{adapter}"', f"recognition adapter {adapter}")

    restore = section(
        commands,
        "public void RestoreSelected()",
        '[CommandMethod("QS3DRECOGNITIONOPTIONS")]',
        "restore adapter",
    )
    options = section(
        commands,
        "public void RecognitionOptions()",
        '[CommandMethod("QS3DRECOGNITIONBOUNDARY"',
        "options adapter",
    )
    boundary = section(
        commands,
        "public void SelectBoundary()",
        '[CommandMethod("QS3DRECOGNITIONLABEL"',
        "boundary adapter",
    )
    label = section(
        commands,
        "public void SelectLabel()",
        '[CommandMethod("QS3DRECOGNITIONAUTO"',
        "label adapter",
    )
    auto = section(
        commands,
        "public void AutoRecognize()",
        "private static Teigha.DatabaseServices.ObjectId[]? ResolveSelection",
        "auto adapter",
    )

    # Preserve the two pre-existing inspection adapters outside the three-regression correction.
    require(restore, 'Queue(document, "QS3DINSPECT")', "restore inspection route")
    require(label, 'Queue(document, "QS3DINSPECT")', "label inspection route")

    # Options has no generic recognition-options workflow yet, so it must fail closed locally.
    if "Queue(document" in options or "SendStringToExecute" in options:
        raise SystemExit("FAIL: recognition options must fail closed instead of dispatching another workflow")
    require(options, "không mở MEP Review/Takeoff thay thế", "recognition options fail-closed message")

    # Boundary and Auto must use the production recognition pipelines, never CustomQuantity takeoff.
    require(boundary, 'Queue(document, "QS3DRECOGNIZE")', "boundary recognition route")
    require(auto, 'Queue(document, "QS3DRECOGNIZEAUTO")', "auto recognition route")
    require(review, '[CommandMethod("QS3DRECOGNIZE", CommandFlags.UsePickSet)]', "manual recognition workflow")
    require(review, '[CommandMethod("QS3DRECOGNIZEAUTO", CommandFlags.UsePickSet)]', "atomic auto-recognition workflow")

    for unrelated in ("QS3DMEPREVIEW", "QS3DTAKEOFF"):
        if unrelated in commands:
            raise SystemExit(f"FAIL: recognition topbar adapter regressed to unrelated workflow token: {unrelated}")

    require(init, "BltRecognitionRibbonAugmenter.TryInitialize()", "recognition parity initialization")
    require(init, "BltRecognitionRibbonAugmenter.Reset()", "recognition parity reset")

    # RibbonBootstrapIconAugmenter now uses a single recursive DecorateItem traversal rather than
    # the older ApplyIconsToCollection helper. Guard behavior/recursion, not a retired helper name.
    require(icons, "DecorateItem(item, visited, ref commandButtons)", "nested-row icon traversal entry")
    require(icons, "DecorateItem(child, visited, ref commandButtons)", "nested-row recursive traversal")
    require(icons, 'GetProperty(item, "Items")', "nested-row readiness traversal")
    require(icons, "HasCompleteVisibleIcon(item)", "preserve richer recognition artwork")

    print(
        "PASS: NHẬN DẠNG topbar keeps BLT3D reference layout/artwork, preserves Restore/Label, "
        "fails Options closed, and routes Boundary/Auto through the production recognition workflows."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
