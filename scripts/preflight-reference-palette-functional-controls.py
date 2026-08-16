from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ReferencePaletteLayout.cs"
COMPAT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.ReferencePaletteFunctionalCompatibility.cs"


def require(text: str, needle: str, label: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: {label} missing required contract: {needle}")


def main() -> int:
    layout = LAYOUT.read_text(encoding="utf-8")
    compat = COMPAT.read_text(encoding="utf-8")

    require(layout, "DispatcherPriority.ApplicationIdle", "reference palette presentation")
    require(compat, "DispatcherPriority.SystemIdle", "functional compatibility ordering")
    require(compat, 'RestoreReferencePaletteButton("Làm mới")', "refresh action")
    require(compat, 'RestoreReferencePaletteButton("Vẽ 3D")', "native 3D action")
    require(compat, 'RestoreReferencePaletteButton("Kiểm tra")', "health action")
    require(compat, 'FindTextBlock("Phạm vi sửa")', "property edit scope")
    require(compat, "FindNearestAncestor<Border>(PropertySearch)", "property search")
    require(compat, 'RenameBlt3dButton("⚡ Nhập tự động", "⚡ Nhập từ chọn")', "truthful selection-import label")
    require(compat, 'RestoreReferencePaletteButton("⚡ Nhập từ chọn")', "selection import action")

    print(
        "PASS: final reference palette presentation keeps production refresh, native 3D, health, "
        "property scope/search and selection-import semantics reachable after visual-density passes."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
