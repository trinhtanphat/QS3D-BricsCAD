from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ReferencePaletteLayout.cs"
COMPAT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ReferencePaletteFunctionalCompatibility.cs"
RUNTIME = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def main() -> int:
    layout = LAYOUT.read_text(encoding="utf-8")
    compat = COMPAT.read_text(encoding="utf-8")
    runtime = RUNTIME.read_text(encoding="utf-8")

    require(layout, "DispatcherPriority.ApplicationIdle", "reference palette presentation")
    require(compat, "DispatcherPriority.SystemIdle", "functional compatibility ordering")
    require(compat, 'RestoreReferencePaletteButton("Làm mới")', "refresh action")
    require(compat, 'RestoreReferencePaletteButton("Vẽ 3D")', "native 3D action")
    require(compat, 'RestoreReferencePaletteButton("Kiểm tra")', "health action")
    require(compat, 'FindTextBlock("Phạm vi sửa")', "property edit scope")
    require(compat, "FindNearestAncestor<Border>(PropertySearch)", "property search")
    require(compat, 'RenameBlt3dButton("⚡ Nhập tự động", "⚡ Nhập từ chọn")', "truthful selection-import label")
    require(compat, 'RestoreReferencePaletteButton("⚡ Nhập từ chọn")', "selection import action")

    # Owner screenshot contract: the docked QS3D surface must end with two visible left plugin
    # regions before the host-owned BricsCAD modelspace: Zone/Floor/model tree, then Family/Properties.
    for needle in (
        "two adjacent plugin columns: Model/Zone/Floor, then Family",
        "DispatcherPriority.SystemIdle",
        "var modelPane = workspace.Children",
        "var familyPane = workspace.Children",
        "columns[0].Width = new GridLength(38, GridUnitType.Star);",
        "columns[1].Width = new GridLength(4);",
        "columns[2].Width = new GridLength(62, GridUnitType.Star);",
        "Grid.SetColumn(modelPane, 0);",
        "modelPane.Visibility = Visibility.Visible;",
        "Grid.SetColumn(familyPane, 2);",
        "familyPane.Visibility = Visibility.Visible;",
        "FamilyList.MinHeight = 100;",
        "PropertyList.MinHeight = 120;",
    ):
        require(runtime, needle, "owner reference two-left-pane runtime layout")

    print(
        "PASS: final reference palette keeps both owner-reference left panes visible and preserves "
        "production refresh, native 3D, health, property scope/search and selection-import semantics."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
