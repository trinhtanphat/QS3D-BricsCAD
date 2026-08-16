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

    for tab_id in (
        "QS3D_HOME",
        "QS3D_PROJECT",
        "QS3D_AUTHOR",
        "QS3D_BIM",
        "QS3D_RECOGNIZE",
        "QS3D_DRAW",
        "QS3D_TOOL",
        "QS3D_MODELING",
        "QS3D_VIEW",
        "QS3D_QTY",
        "QS3D_REV",
    ):
        require(augmenter, f'"{tab_id}"', augmenter_rel)

    for needle in (
        "if (HasCompleteVisibleIcon(item))",
        "GetProperty(item, \"ShowImage\") is bool showImage",
        'GetProperty(item, "Image") != null',
        'GetProperty(item, "LargeImage") != null',
        'SetProperty(item, "ShowImage", true);',
        'SetProperty(item, "Image", RibbonIconFactory.Create(icon, 16));',
        'SetProperty(item, "LargeImage", RibbonIconFactory.Create(icon, 32));',
        'return RibbonIconKind.UpdateStatus;',
        'return RibbonIconKind.SaveAs;',
        'return RibbonIconKind.Save;',
        'return RibbonIconKind.OpenProject;',
        'return RibbonIconKind.Settings;',
        'return RibbonIconKind.Update;',
        'return RibbonIconKind.Objects;',
        'return commandButtons > 0;',
    ):
        require(augmenter, needle, augmenter_rel)

    forbid(augmenter, 'SetProperty(item, "ShowImage", false);', augmenter_rel)

    for needle in (
        "ready = RibbonBootstrapIconAugmenter.TryInitialize() && ready;",
        "RibbonBootstrapIconAugmenter.Reset();",
        "ready = ProjectRibbonAugmenter.TryInitialize() && ready;",
        "ready = BltHomeRibbonAugmenter.TryInitialize() && ready;",
        "ready = BltDrawRibbonFailSafe.TryInitialize() && ready;",
    ):
        require(coordinator, needle, coordinator_rel)

    icon_index = coordinator.index("RibbonBootstrapIconAugmenter.TryInitialize")
    for predecessor in (
        "ProjectRibbonAugmenter.TryInitialize",
        "BltHomeRibbonAugmenter.TryInitialize",
        "BltDrawRibbonFailSafe.TryInitialize",
    ):
        if coordinator.index(predecessor) > icon_index:
            raise SystemExit(
                f"FAIL: complete ribbon icon decoration must run after {predecessor}"
            )

    print(
        "PASS: every canonical QS3D ribbon tab gets deterministic fallback icons for "
        "text-only command buttons while preserving already-polished Home/Draw/custom images."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
