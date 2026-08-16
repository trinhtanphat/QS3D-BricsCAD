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
    v26_entry_rel = "src/QS3D.BricsCAD.V26/PluginEntry.cs"

    augmenter = read(augmenter_rel)
    coordinator = read(coordinator_rel)
    v26_entry = read(v26_entry_rel)

    for tab_id in (
        "QS3D_PROJECT",
        "QS3D_BIM",
        "QS3D_RECOGNIZE",
        "QS3D_DRAW",
        "QS3D_TOOL",
        "QS3D_MODELING",
        "QS3D_VIEW",
        "QS3D_QTY",
        "QS3D_REV",
        "QS3D_AUTHOR",
    ):
        require(augmenter, f'"{tab_id}"', augmenter_rel)

    for needle in (
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

    for needle in (
        "RibbonBootstrapIconAugmenter.TryInitialize();",
        "RibbonBootstrapIconAugmenter.Reset();",
    ):
        require(v26_entry, needle, v26_entry_rel)

    if v26_entry.index("QuantityReferenceRibbonAugmenter.TryInitialize") > v26_entry.index("RibbonBootstrapIconAugmenter.TryInitialize"):
        raise SystemExit("FAIL: V26 bootstrap icon decoration must run after feature ribbon augmenters")

    print(
        "PASS: bootstrap ribbon buttons from MÔ HÌNH BIM through TẠO MỚI, plus the existing "
        "THIẾT LẬP DỰ ÁN lane, receive deterministic QS3D-generated icons in V25 and V26 "
        "without taking over KHỞI ĐẦU."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
