#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONTRACT = ROOT / "src/QS3D.BricsCAD.V25/SingleFootingContract.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SingleFooting.cs"
PROPERTIES = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SingleFooting.Properties.cs"
REGEN = ROOT / "src/QS3D.BricsCAD.V25/SingleFootingRegenerationService.cs"
DRAW = ROOT / "src/QS3D.BricsCAD.V25/SingleFootingCommands.cs"
QUICK_DRAW = ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs"
GEOMETRY = ROOT / "src/QS3D.Core/Geometry/SingleFootingGeometry.cs"


def read(path: Path) -> str:
    if not path.is_file():
        raise AssertionError(f"missing required file: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise AssertionError(f"missing {label}: {needle}")


def main() -> int:
    contract = read(CONTRACT)
    workspace = read(WORKSPACE)
    properties = read(PROPERTIES)
    regen = read(REGEN)
    draw = read(DRAW)
    quick_draw = read(QUICK_DRAW)
    geometry = read(GEOMETRY)

    require(contract, 'TreeTag = "Foundation.SingleFooting"', "stable subtype identity")
    require(workspace, "RouteSingleFootingAddActions(singleFootingSelected);", "selection-owned Add routing")
    require(workspace, "button.Click += OnSingleFootingAwareAddClick;", "dedicated Add handler")
    require(workspace, "new SingleFootingDimensionsDialog()", "six-value dialog")
    require(workspace, "existing.Tag = SingleFootingContract.TreeTag;", "tree identity assignment")

    for name in ("L1", "W1", "L2", "W2", "H1", "H2"):
        require(properties, f'"{name}"', f"property row {name}")
    require(properties, "SingleFootingRegenerationService.ApplyFamilyDimensions", "edit-to-regenerate bridge")
    require(regen, "ProjectStateSnapshot.Capture(project)", "semantic rollback snapshot")
    require(regen, "StartTransaction()", "native transaction")
    require(regen, "GeneratedGeometryService.PrepareReplacement", "owned Solid3d replacement")
    require(regen, "GeneratedGeometryService.CommitReplacement", "ownership metadata commit")
    require(regen, "ResizeFootprint", "source footprint refresh")
    require(regen, "SingleFootingContract.Apply(element, dimensions)", "instance parameter refresh")

    require(geometry, "if (L2M > L1M)", "L2 <= L1 validation")
    require(geometry, "if (W2M > W1M)", "W2 <= W1 validation")
    require(geometry, "H2M = RequireNonNegativeFinite", "H2 non-negative validation")
    require(draw, "if (!(h2 > 0d))", "box-only H2=0 path")
    require(draw, "CreateTaperedLoft", "tapered upper geometry path")
    require(draw, "AllowNone = true", "repeat-pick Enter termination")
    require(quick_draw, "new SingleFootingCommands().DrawSingleFooting();", "active-family quick draw dispatch")

    print("PASS single-footing workspace complete preflight")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
