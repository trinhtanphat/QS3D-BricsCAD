from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFamilyWorkspace.cs"


def require(text: str, needle: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: integrated BLT3D workspace contract missing: {needle}")


def require_order(text: str, earlier: str, later: str) -> None:
    left = text.find(earlier)
    right = text.find(later)
    if left < 0 or right < 0 or left >= right:
        raise SystemExit(
            "FAIL: integrated BLT3D workspace ordering contract is missing or reversed: "
            f"{earlier!r} must precede {later!r}"
        )


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")

    # One integrated palette: model navigation remains visible in the first column while the
    # Family/type list and Properties share the single right-hand column vertically.
    for needle in (
        "ApplyBlt3dFamilyWorkspace",
        "RestoreBlt3dWorkspaceColumns",
        "RestoreBlt3dFamilyRows",
        "HideRetiredDashboardBands",
        "EnsureBlt3dFoundationTree",
        "ModelTree.SelectedItemChanged",
        "FamilyList",
        "PropertyList",
        "modelColumn.MinWidth = 150",
        "modelColumn.MaxWidth = 220",
        "familyColumn.Width = new GridLength(1, GridUnitType.Star)",
        "familyGrid.RowDefinitions[0].Height = new GridLength(55, GridUnitType.Star)",
        "familyGrid.RowDefinitions[2].Height = new GridLength(45, GridUnitType.Star)",
        "WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled",
        "WorkspaceOverflow.ScrollToHorizontalOffset(0)",
        "Grid.GetColumn(child) <= 2 ? Visibility.Visible : Visibility.Collapsed",
        'text.Text = "Zone làm việc"',
        'text.Text = "Tầng làm việc"',
        'text.Text = "Thuộc tính"',
        'RenameBlt3dButton("+ Thêm", "+ Add")',
    ):
        require(text, needle)

    # Keep the primary execution order stable: layout restoration first, then the production
    # interaction wiring, then presentation-only relabeling/styling.
    require_order(text, "RestoreBlt3dWorkspaceColumns();", "AttachFamilySubtypeInteractions();")
    require_order(text, "RestoreBlt3dFamilyRows();", "AttachFamilySubtypeInteractions();")
    require_order(text, "AttachFamilySubtypeInteractions();", "ApplyBlt3dWorkspaceLabels();")

    print(
        "PASS: integrated BLT3D workspace keeps model navigation visible, stacks Family above Properties, "
        "disables horizontal overflow, hides retired dashboard columns, and preserves production interaction wiring."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
