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


def spec_line(text: str, button_id: str) -> str:
    marker = f'"{button_id}"'
    pos = text.find(marker)
    if pos < 0:
        raise SystemExit(f"FAIL: {button_id} missing from recognition parity surface")
    start = text.rfind("\n", 0, pos) + 1
    end = text.find("\n", pos)
    if end < 0:
        end = len(text)
    return text[start:end]


def spec_block(text: str, button_id: str) -> str:
    marker = f'"{button_id}"'
    pos = text.find(marker)
    if pos < 0:
        raise SystemExit(f"FAIL: {button_id} missing from recognition parity surface")
    start = text.rfind("new RecognitionButtonSpec(", 0, pos)
    if start < 0:
        raise SystemExit(f"FAIL: {button_id} missing RecognitionButtonSpec start")
    end = text.find(")", pos)
    if end < 0:
        raise SystemExit(f"FAIL: {button_id} missing RecognitionButtonSpec end")
    return text[start:end + 1]


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
    require(
        ribbon,
        'SetProperty(button, "IsEnabled", spec.Enabled || spec.PreserveSourceColorWhenNonInteractive);',
        "reference host presentation state",
    )
    require(ribbon, "bool enabled = true", "supported recognition button default")

    for disabled_id in (
        "QS3D_RECOGNIZE_BLT_TEXT",
        "QS3D_RECOGNIZE_BLT_TABLE",
    ):
        if "enabled: false" not in spec_line(ribbon, disabled_id):
            raise SystemExit(f"FAIL: {disabled_id} must remain visually disabled")

    validate_spec = spec_block(ribbon, "QS3D_RECOGNIZE_BLT_VALIDATE")
    require(validate_spec, "string.Empty", "Validate remains without a command")
    require(validate_spec, "enabled: false", "Validate executable-command authority remains disabled")
    require(
        validate_spec,
        "preserveSourceColorWhenNonInteractive: true",
        "Validate host presentation preserves source status colors",
    )
    require(
        ribbon,
        "if (spec.Enabled && !string.IsNullOrWhiteSpace(spec.Command))",
        "recognition command routing remains gated by executable authority",
    )

    for enabled_id in (
        "QS3D_RECOGNIZE_BLT_RESTORE",
        "QS3D_RECOGNIZE_BLT_OPTIONS",
        "QS3D_RECOGNIZE_BLT_BOUNDARY",
        "QS3D_RECOGNIZE_BLT_LABEL",
        "QS3D_RECOGNIZE_BLT_AUTO",
    ):
        if "enabled: false" in spec_line(ribbon, enabled_id):
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

    require(restore, 'Queue(document, "QS3DINSPECT")', "restore inspection route")
    require(label, 'Queue(document, "QS3DINSPECT")', "label inspection route")

    if "Queue(document" in options or "SendStringToExecute" in options:
        raise SystemExit("FAIL: recognition options must fail closed instead of dispatching another workflow")
    require(options, "không mở MEP Review/Takeoff thay thế", "recognition options fail-closed message")

    require(boundary, "document.Editor.SetImpliedSelection(ids)", "boundary PICKFIRST preservation")
    require(boundary, 'Queue(document, "QS3DRECOGNIZE")', "boundary recognition route")
    require(auto, "document.Editor.SetImpliedSelection(ids)", "auto PICKFIRST preservation")
    require(auto, 'Queue(document, "QS3DRECOGNIZEAUTO")', "auto recognition route")
    require(review, '[CommandMethod("QS3DRECOGNIZE", CommandFlags.UsePickSet)]', "manual recognition workflow")
    require(review, '[CommandMethod("QS3DRECOGNIZEAUTO", CommandFlags.UsePickSet)]', "atomic auto-recognition workflow")

    for unrelated in ("QS3DMEPREVIEW", "QS3DTAKEOFF"):
        if unrelated in commands:
            raise SystemExit(f"FAIL: recognition topbar adapter regressed to unrelated workflow token: {unrelated}")

    require(init, "BltRecognitionRibbonAugmenter.TryInitialize()", "recognition parity initialization")
    require(init, "BltRecognitionRibbonAugmenter.Reset()", "recognition parity reset")

    require(icons, "DecorateItem(item, visited, ref commandButtons)", "nested-row icon traversal entry")
    require(icons, "DecorateItem(child, visited, ref commandButtons)", "nested-row recursive traversal")
    require(icons, 'GetProperty(item, "Items")', "nested-row readiness traversal")
    require(icons, "HasCompleteVisibleIcon(item)", "preserve richer recognition artwork")

    print(
        "PASS: NHẬN DẠNG topbar keeps BLT3D reference layout/artwork, preserves Restore/Label, "
        "fails Options closed, keeps Text/Table visually disabled, preserves Validate status colors "
        "without granting command authority, and routes Boundary/Auto through the production recognition workflows."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
