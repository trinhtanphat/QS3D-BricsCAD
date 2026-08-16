#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def read(relative):
    path = ROOT / relative
    if not path.is_file():
        fail(f"missing required source: {relative}")
    return path.read_text(encoding="utf-8")


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def main():
    ribbon = read("src/QS3D.BricsCAD.V25/Ribbon/ProjectRibbonAugmenter.cs")
    command = read("src/QS3D.BricsCAD.V25/ProjectPropertiesCommands.cs")
    window = read("src/QS3D.BricsCAD.V25/UI/ProjectPropertiesWindow.cs")
    v26_project = read("src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj")

    # The BLT3D reference exposes exactly three Project Setup entry points. Keep their
    # production routes distinct: Info -> Project Tools, Floors -> Level UI, Properties ->
    # the bounded Project Properties surface rather than the broad Project Tools dashboard.
    for token in (
        'new ButtonSpec("QS3D_PROJECT_INFO", "Thông tin\\ndự án", "QS3DPROJECTTOOLS")',
        'new ButtonSpec("QS3D_PROJECT_FLOORS", "Cài đặt\\ntầng", "QS3DLEVELS")',
        'new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\\ndự án", "QS3DPROJECTPROPERTIES")',
    ):
        require(ribbon, token, "Project Setup ribbon routing")

    wrong_route = 'new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\\ndự án", "QS3DPROJECTTOOLS")'
    if wrong_route in ribbon:
        fail("Project Properties must not route to the broad QS3DPROJECTTOOLS dashboard")

    for token in (
        '[CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]',
        'var window = new ProjectPropertiesWindow();',
        'Application.ShowModelessWindow(IntPtr.Zero, window, true);',
    ):
        require(command, token, "Project Properties command")

    # The supplied BLT3D screenshot explicitly marks this screen as not built. Preserve that
    # visible contract without inventing unsupported persistence fields or mutating ProjectState.
    for token in (
        '(Chưa xây dựng — Thuộc tính dự án)',
        'Background = new SolidColorBrush(Color.FromRgb(20, 20, 20))',
        'HorizontalAlignment = HorizontalAlignment.Center',
        'VerticalAlignment = VerticalAlignment.Center',
    ):
        require(window, token, "Project Properties BLT3D placeholder")

    forbidden = (
        "ProjectState",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "Touch()",
        "SetMetadata",
        "SetProperty",
    )
    for token in forbidden:
        if token in window:
            fail(f"Project Properties placeholder must remain read-only: found {token}")

    # V26 must continue consuming the same V25 command/window source automatically.
    require(v26_project, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared adapter source")

    print("PASS: BLT3D Project Properties route is dedicated and the reference placeholder stays read-only.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
