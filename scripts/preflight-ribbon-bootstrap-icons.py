#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required ribbon icon contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden ribbon icon contract: {needle}")


def main():
    augmenter_rel = "src/QS3D.BricsCAD.V25/Ribbon/RibbonBootstrapIconAugmenter.cs"
    coordinator_rel = "src/QS3D.BricsCAD.V25/Ribbon/RibbonInitializationCoordinator.cs"

    augmenter = read(augmenter_rel)
    coordinator = read(coordinator_rel)

    for needle in (
        '"QS3D_PROJECT", "QS3D_AUTHOR"',
        'SetProperty(item, "ShowImage", true);',
        'SetProperty(item, "Image", RibbonIconFactory.Create(icon, 16));',
        'SetProperty(item, "LargeImage", RibbonIconFactory.Create(icon, 32));',
        'return RibbonIconKind.UpdateStatus;',
        'return RibbonIconKind.SaveAs;',
        'return RibbonIconKind.OpenProject;',
        'return RibbonIconKind.Settings;',
        'return RibbonIconKind.Update;',
        'return RibbonIconKind.Objects;',
        'return updatedButtons > 0;',
    ):
        require(augmenter, needle, augmenter_rel)

    forbid(augmenter, '"QS3D_HOME"', augmenter_rel)
    forbid(augmenter, 'SetProperty(item, "ShowImage", false);', augmenter_rel)

    for needle in (
        "ready = RibbonBootstrapIconAugmenter.TryInitialize() && ready;",
        "RibbonBootstrapIconAugmenter.Reset();",
        "ready = ProjectRibbonAugmenter.TryInitialize() && ready;",
    ):
        require(coordinator, needle, coordinator_rel)

    if coordinator.index("ProjectRibbonAugmenter.TryInitialize") > coordinator.index("RibbonBootstrapIconAugmenter.TryInitialize"):
        raise SystemExit("FAIL: bootstrap icon decoration must run after ProjectRibbonAugmenter adds its buttons")

    print(
        "PASS: THIẾT LẬP DỰ ÁN and TẠO MỚI ribbon buttons receive deterministic "
        "QS3D-generated icons after feature reconciliation without taking over KHỞI ĐẦU."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
