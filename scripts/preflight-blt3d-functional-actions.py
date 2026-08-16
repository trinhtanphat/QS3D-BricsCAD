from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFunctionalActions.cs"


def require(text: str, needle: str) -> None:
    if needle not in text:
        raise SystemExit(f"FAIL: BLT3D functional-action contract missing: {needle}")


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")
    for needle in (
        "DispatcherPriority.ContextIdle",
        'FindButton("Làm mới")',
        "collapsedHeader.Children.Remove(refresh);",
        "host.Children.Insert",
        "refresh.Visibility = Visibility.Visible;",
        'FindButton("Vẽ 3D")',
        "native3d.Visibility = Visibility.Visible;",
    ):
        require(text, needle)

    print(
        "PASS: integrated BLT3D presentation keeps project refresh and native Family Solid3D actions accessible after compact-layout styling."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
