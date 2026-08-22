#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COORDINATOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "RibbonInitializationCoordinator.cs"
FAIL_SAFE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltDrawRibbonFailSafe.cs"


def require(text, needle, label):
    if needle not in text:
        print(f"ERROR: missing {label}: {needle}")
        return False
    return True


def require_order(text, needles, label):
    positions = [text.find(needle) for needle in needles]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        print(f"ERROR: invalid {label} ordering: {' -> '.join(needles)}")
        return False
    return True


def main():
    coordinator = COORDINATOR.read_text(encoding="utf-8")
    fail_safe = FAIL_SAFE.read_text(encoding="utf-8")
    ok = True

    ok &= require(
        coordinator,
        "ready = BltDrawRibbonFailSafe.TryInitialize() && ready;",
        "coordinator fail-safe routing",
    )
    if "ready = BltDrawRibbonAugmenter.TryInitialize() && ready;" in coordinator:
        print("ERROR: coordinator still bypasses the BLT Draw fail-safe.")
        ok = False

    for panel_id in (
        "QS3D_DRAW_PRIMITIVES_PANEL_SOURCE",
        "QS3D_DRAW_TRANSFORM_PANEL_SOURCE",
        "QS3D_DRAW_EDIT_PANEL_SOURCE",
        "QS3D_DRAW_BLT_DRAW_PANEL_SOURCE",
        "QS3D_DRAW_BLT_TOOLS_PANEL_SOURCE",
        "QS3D_DRAW_BLT_IFC_PANEL_SOURCE",
    ):
        ok &= require(fail_safe, f'"{panel_id}"', f"panel ownership {panel_id}")

    ok &= require(fail_safe, "var fallback = CaptureFallback();", "pre-init fallback snapshot")
    ok &= require(fail_safe, "if (BltDrawRibbonAugmenter.TryInitialize())", "rich BLT initialization")
    ok &= require(fail_safe, "RestoreFallback(fallback);", "failure recovery")
    ok &= require(fail_safe, "TryRemoveOwnedPanel(fallback.Panels, sourceId);", "partial rich cleanup")
    ok &= require(
        fail_safe,
        "if (FindPanelBySourceId(fallback.Panels, captured.SourceId) == null)",
        "idempotent fallback presence check",
    )
    ok &= require(fail_safe, "Add(fallback.Panels, captured.Panel);", "reuse captured fallback panel")

    ok &= require_order(
        fail_safe,
        [
            "var fallback = CaptureFallback();",
            "if (BltDrawRibbonAugmenter.TryInitialize())",
            "RestoreFallback(fallback);",
            "return false;",
        ],
        "snapshot / rich-init / recovery",
    )
    ok &= require_order(
        fail_safe,
        [
            "foreach (var sourceId in RichPanelSourceIds)",
            "foreach (var captured in fallback.LegacyPanels)",
            "Add(fallback.Panels, captured.Panel);",
        ],
        "cleanup-before-restore",
    )

    if "RibbonBootstrapper.Reset()" in fail_safe or "RibbonBootstrapper.TryInitialize()" in fail_safe:
        print("ERROR: BLT failure recovery must not re-bootstrap unrelated Ribbon tabs.")
        ok = False

    if not ok:
        return 1

    print("PASS: BLT Draw rich-panel failure keeps transactional fallback recovery source contract.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
