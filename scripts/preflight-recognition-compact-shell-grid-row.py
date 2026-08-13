#!/usr/bin/env python3
"""Guard Recognition compact-shell row lookup against the generated DataGrid name collision."""

from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RecognitionWindow.CompactShell.cs"
XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "RecognitionWindow.xaml"


def read(path: Path) -> str:
    if not path.is_file():
        raise SystemExit(f"FAIL: missing required source: {path.relative_to(ROOT)}")
    return path.read_text(encoding="utf-8")


def require(text: str, token: str, label: str) -> None:
    if token not in text:
        raise SystemExit(f"FAIL: {label}: missing {token!r}")


def main() -> None:
    partial = read(PARTIAL)
    xaml = read(XAML)

    require(xaml, '<DataGrid x:Name="Grid"', "generated DataGrid member collision fixture")
    require(partial, "private void TuneRecognitionHeader(Grid root)", "header compact-shell boundary")
    require(partial, "private void TuneRecognitionFooter(Grid root)", "footer compact-shell boundary")

    qualified = "System.Windows.Controls.Grid.GetRow(border)"
    if partial.count(qualified) != 2:
        raise SystemExit("FAIL: expected exactly two fully qualified WPF Grid.GetRow calls")

    require(
        partial,
        "System.Windows.Controls.Grid.GetRow(border) == 0",
        "header attached-row lookup",
    )
    require(
        partial,
        "System.Windows.Controls.Grid.GetRow(border) == 2",
        "footer attached-row lookup",
    )

    if "Grid.GetRow(" in partial.replace(qualified, ""):
        raise SystemExit("FAIL: unqualified Grid.GetRow can bind to the generated DataGrid member")

    print("PASS: V25 Recognition compact-shell Grid row lookup is type-qualified")


if __name__ == "__main__":
    main()
