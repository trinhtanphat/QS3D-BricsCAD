#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
INVARIANT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.BltReferenceInvariant.cs"
ICON_PARITY = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.BltIconParity.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.xaml"


def require(text: str, needle: str, label: str, errors: list[str]) -> None:
    if needle not in text:
        errors.append(f"missing {label}: {needle}")


def forbid(text: str, needle: str, label: str, errors: list[str]) -> None:
    if needle in text:
        errors.append(f"forbidden {label}: {needle}")


def main() -> int:
    errors: list[str] = []
    for path, label in (
        (INVARIANT, "floor-reference invariant source"),
        (ICON_PARITY, "Project Setup icon-parity source"),
        (XAML, "floor setup XAML"),
    ):
        if not path.is_file():
            errors.append(f"missing {label}: {path.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    invariant = INVARIANT.read_text(encoding="utf-8")
    icon_parity = ICON_PARITY.read_text(encoding="utf-8")
    xaml = XAML.read_text(encoding="utf-8")

    require(invariant, "static FloorLevelWindow()", "one-time class-handler registration", errors)
    require(invariant, "EventManager.RegisterClassHandler", "routed click guard", errors)
    require(invariant, "ButtonBase.ClickEvent", "button click route", errors)
    require(invariant, "checkBox.Tag is BltFloorRow row", "reference-row identity", errors)
    require(invariant, "window.EnsureBltReferenceInvariant(row);", "synchronous post-click invariant call", errors)
    forbid(invariant, "Dispatcher.BeginInvoke", "deferred reference normalization", errors)
    forbid(invariant, "DispatcherPriority.Background", "background reference normalization", errors)
    require(invariant, "references.Count == 1", "single-reference fast path", errors)
    require(invariant, "references.FirstOrDefault() ?? clickedRow", "fallback reference keeper", errors)
    require(invariant, "item.IsReference = ReferenceEquals(item, keeper)", "exclusive reference normalization", errors)
    require(invariant, "references.Count == 0", "uncheck recovery", errors)

    require(icon_parity, "private static readonly bool BltIconParityRegistered = RegisterBltIconParity();", "one-time icon class-handler registration", errors)
    require(icon_parity, "RegisterBltIconParity", "icon class-handler registration", errors)
    require(icon_parity, "BuildBltNavIconContent", "deterministic vector nav icon builder", errors)
    require(icon_parity, 'case "Thông tin dự án"', "Project Info icon mapping", errors)
    require(icon_parity, 'case "Cài đặt tầng"', "Floor Settings icon mapping", errors)
    require(icon_parity, 'case "Thuộc tính dự án"', "Project Properties icon mapping", errors)
    require(icon_parity, 'case "Thêm"', "Add Zone button mapping", errors)
    require(icon_parity, 'case "Xóa"', "Delete Zone button mapping", errors)
    require(icon_parity, 'case "Chèn sàn"', "Insert Floor button mapping", errors)
    require(icon_parity, 'case "Chèn sàn xuống dưới"', "Insert Floor Below button mapping", errors)
    require(icon_parity, 'case "Xóa sàn"', "Delete Floor button mapping", errors)
    require(icon_parity, 'case "Áp dụng thay đổi"', "Apply Changes button mapping", errors)
    require(icon_parity, "Geometry.Parse(geometryData)", "vector geometry rendering", errors)
    require(icon_parity, "window.Dispatcher.HasShutdownStarted", "deferred icon shutdown guard", errors)

    require(xaml, 'x:Key="BltToolbarIcon"', "toolbar vector icon style", errors)
    require(xaml, 'IsChecked="{Binding IsReference, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"', "two-way reference binding", errors)
    require(xaml, 'Tag="{Binding}"', "reference row tag", errors)
    require(xaml, 'Click="OnBltReferenceClick"', "canonical reference click handler", errors)

    if errors:
        print("Project Setup floor-reference/icon preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: BLT3D Project Setup keeps one synchronous visible floor reference and deterministic semantic button icons.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
