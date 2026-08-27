#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CONTRACT = ROOT / "src/QS3D.BricsCAD.V25/SingleFootingContract.cs"
WORKSPACE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SingleFooting.cs"
BLT = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.Blt3dFamilyWorkspace.cs"
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


def reject(text: str, needle: str, label: str) -> None:
    if needle in text:
        raise AssertionError(f"forbidden {label}: {needle}")


def main() -> int:
    contract = read(CONTRACT)
    workspace = read(WORKSPACE)
    blt = read(BLT)
    properties = read(PROPERTIES)
    regen = read(REGEN)
    draw = read(DRAW)
    quick_draw = read(QUICK_DRAW)
    geometry = read(GEOMETRY)

    require(contract, 'CategoryCode = "Foundation.SingleFooting"', "stable subtype identity")
    require(contract, 'TreeTag = CategoryCode', "tree identity alias")
    for name in ("L1", "W1", "L2", "W2", "H1", "H2"):
        require(contract, f'public const string {name}Key = "SINGLE_FOOTING_{name}";', f"canonical parameter {name}")
    require(contract, "family.Properties[CategoryCodeKey] = CategoryCode;", "family category-code persistence")
    require(contract, "element.Properties[CategoryCodeKey] = CategoryCode;", "element category-code persistence")
    require(contract, "LegacyL1Key", "10221 parameter compatibility")

    for token in (
        "DispatcherPriority.ContextIdle",
        "RouteSingleFootingAddActions(",
        "OnSingleFootingAwareAddClick",
        "button.Click += OnSingleFootingAwareAddClick",
    ):
        reject(workspace, token, "deferred/rewired SingleFooting Add route")

    require(workspace, "existing.Tag = SingleFootingContract.CategoryCode;", "stable tree identity assignment")
    require(workspace, "new SingleFootingDimensionsDialog()", "six-value Add dialog")
    require(workspace, "if (dialog.ShowDialog() != true || dialog.Dimensions == null)", "cancel-before-create boundary")
    require(workspace, "CreateSingleFootingFamily(dialog.Dimensions);", "dialog-to-family bridge")

    add_start = blt.find("private void OnBlt3dFamilyAddClick(object sender, RoutedEventArgs e)")
    add_end = blt.find("private void ShowBlt3dFamilyModeChooser", add_start)
    add_method = blt[add_start:add_end] if add_start >= 0 and add_end > add_start else ""
    ordered = (
        "e.Handled = true;",
        "if (IsSingleFootingSelected())",
        "HandleSingleFootingAdd(e);",
        "return;",
        "ShowBlt3dFamilyModeChooser();",
    )
    last = -1
    for token in ordered:
        pos = add_method.find(token)
        if pos < 0:
            raise AssertionError("missing direct BLT Add route token: " + token)
        if pos <= last:
            raise AssertionError("BLT Add route is out of order: " + token)
        last = pos

    for name in ("L1", "W1", "L2", "W2", "H1", "H2"):
        require(properties, f'"{name}"', f"property row {name}")
    require(properties, 'Unit = "mm"', "dimension unit display")
    require(properties, "SingleFootingRegenerationService.ApplyFamilyDimensions", "edit-to-regenerate bridge")
    reject(properties, 'Name = "Bề dày"', "generic thickness editor")

    for token in (
        "ProjectStateSnapshot.Capture(project)",
        "StartTransaction()",
        "GeneratedGeometryService.PrepareReplacement",
        "GeneratedGeometryService.MarkGenerated",
        "GeneratedGeometryService.CommitReplacement",
        "ResizeFootprint",
        "SingleFootingContract.Apply(element, dimensions)",
        "rollback.Restore(project)",
        "new AggregateException(operationError, restoreError)",
        "candidate.GetBulgeAt(index)",
    ):
        require(regen, token, "atomic regeneration contract")

    for token in (
        "if (L2M > L1M)",
        "if (W2M > W1M)",
        "H2M = RequireNonNegativeFinite",
    ):
        require(geometry, token, "dimension validation")

    require(draw, "if (!(h2 > 0d))", "box-only H2=0 path")
    require(draw, "CreateTaperedLoft", "tapered upper geometry path")
    require(draw, "AllowNone = true", "repeat-pick Enter termination")
    require(quick_draw, "new SingleFootingCommands().DrawSingleFooting();", "active-family Vẽ dispatch")

    print("PASS single-footing workspace complete preflight")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except AssertionError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
