#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
INVARIANT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.BltReferenceInvariant.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "FloorLevelWindow.xaml"


def require(text: str, needle: str, label: str, errors: list[str]) -> None:
    if needle not in text:
        errors.append(f"missing {label}: {needle}")


def main() -> int:
    errors: list[str] = []
    if not INVARIANT.is_file():
        errors.append(f"missing invariant source: {INVARIANT.relative_to(ROOT)}")
    if not XAML.is_file():
        errors.append(f"missing floor setup XAML: {XAML.relative_to(ROOT)}")
    if errors:
        for error in errors:
            print("ERROR:", error)
        return 1

    invariant = INVARIANT.read_text(encoding="utf-8")
    xaml = XAML.read_text(encoding="utf-8")

    require(invariant, "static FloorLevelWindow()", "one-time class-handler registration", errors)
    require(invariant, "EventManager.RegisterClassHandler", "routed click guard", errors)
    require(invariant, "ButtonBase.ClickEvent", "button click route", errors)
    require(invariant, "checkBox.Tag is BltFloorRow row", "reference-row identity", errors)
    require(invariant, "EnsureBltReferenceInvariant(row)", "post-click invariant call", errors)
    require(invariant, "references.Count == 1", "single-reference fast path", errors)
    require(invariant, "references.FirstOrDefault() ?? clickedRow", "fallback reference keeper", errors)
    require(invariant, "item.IsReference = ReferenceEquals(item, keeper)", "exclusive reference normalization", errors)
    require(invariant, "references.Count == 0", "uncheck recovery", errors)

    require(xaml, 'IsChecked="{Binding IsReference, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"', "two-way reference binding", errors)
    require(xaml, 'Tag="{Binding}"', "reference row tag", errors)
    require(xaml, 'Click="OnBltReferenceClick"', "canonical reference click handler", errors)

    if errors:
        print("Project Setup floor-reference preflight FAILED:")
        for error in errors:
            print(" -", error)
        return 1

    print("PASS: BLT3D Project Setup keeps exactly one visible floor reference after checkbox clicks.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
