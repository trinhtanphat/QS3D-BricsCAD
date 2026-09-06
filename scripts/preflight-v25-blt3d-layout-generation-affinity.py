#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
FAMILY = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFamilyWorkspace.cs"
FIVE_ZONE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        print(f"ERROR: missing {label}: {needle}")
        raise SystemExit(1)


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        print(f"ERROR: stale {label} remains: {needle}")
        raise SystemExit(1)


def main() -> int:
    family = FAMILY.read_text(encoding="utf-8")
    five_zone = FIVE_ZONE.read_text(encoding="utf-8")

    require(family, "_blt3dFamilyWorkspaceAttachmentGeneration", "family attachment generation field")
    require(family, "FrameworkElement.UnloadedEvent", "family unload class handler")
    require(family, "OnBlt3dFamilyWorkspaceUnloaded", "family unload invalidation handler")
    require(family, "capturedGeneration", "family queued generation capture")
    require(family, "panel.IsLoaded && panel._blt3dFamilyWorkspaceAttachmentGeneration == capturedGeneration", "family exact generation fence")
    reject(family, "new Action(panel.ApplyBlt3dFamilyWorkspace)", "unfenced family dispatcher callback")

    require(five_zone, "_blt3dFiveZoneRuntimeLayoutAttachmentGeneration", "five-zone attachment generation field")
    require(five_zone, "FrameworkElement.UnloadedEvent", "five-zone unload class handler")
    require(five_zone, "OnBlt3dFiveZoneRuntimeLayoutUnloaded", "five-zone unload invalidation handler")
    require(five_zone, "capturedGeneration", "five-zone queued generation capture")
    require(five_zone, "panel.IsLoaded && panel._blt3dFiveZoneRuntimeLayoutAttachmentGeneration == capturedGeneration", "five-zone exact generation fence")
    reject(five_zone, "new Action(panel.ApplyBlt3dFiveZoneRuntimeLayout)", "unfenced five-zone dispatcher callback")

    if "_blt3dFamilyWorkspaceAttachmentGeneration" in five_zone or "_blt3dFiveZoneRuntimeLayoutAttachmentGeneration" in family:
        print("ERROR: BLT3D layout features must keep independent attachment generations")
        return 1

    print("PASS: BLT3D queued layout callbacks are fenced to independent attachment generations")
    return 0


if __name__ == "__main__":
    sys.exit(main())
