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

    for token in (
        'new ButtonSpec("QS3D_PROJECT_INFO", "Thông tin\\ndự án", "QS3DPROJECTINFO",',
        'new ButtonSpec("QS3D_PROJECT_FLOORS", "Cài đặt\\ntầng", "QS3DLEVELS",',
        'new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\\ndự án", "QS3DPROJECTPROPERTIES",',
    ):
        require(ribbon, token, "Project Setup ribbon routing")

    wrong_route = 'new ButtonSpec("QS3D_PROJECT_PROPERTIES", "Thuộc tính\\ndự án", "QS3DPROJECTTOOLS",'
    if wrong_route in ribbon:
        fail("Project Properties must not route to the broad QS3DPROJECTTOOLS dashboard")

    for token in (
        '[CommandMethod("QS3DPROJECTPROPERTIES", CommandFlags.Modal)]',
        'private static ProjectPropertiesWindow? _pending;',
        'private static ProjectPropertiesWindow? _published;',
        'var pending = _pending;',
        'CloseOwnerBeforeReplacement(pending, "pending");',
        'var published = _published;',
        'if (published.IsLoaded)',
        'published.Activate();',
        'var window = new ProjectPropertiesWindow();',
        'window.Closed += (_, __) =>',
        'if (ReferenceEquals(_pending, window)) _pending = null;',
        'if (ReferenceEquals(_published, window)) _published = null;',
        '_pending = window;',
        'Application.ShowModelessWindow(IntPtr.Zero, window, true);',
        'if (!window.IsLoaded)',
        'if (!ReferenceEquals(_pending, window))',
        '_published = window;',
        'CloseOwnerBeforeReplacement(ProjectPropertiesWindow window, string state)',
        'ex.GetType().Name',
    ):
        require(command, token, "Project Properties command")

    try:
        pending = command.index('_pending = window;')
        show = command.index('Application.ShowModelessWindow(IntPtr.Zero, window, true);', pending)
        loaded = command.index('if (!window.IsLoaded)', show)
        exact = command.index('if (!ReferenceEquals(_pending, window))', loaded)
        clear = command.index('_pending = null;', exact)
        publish = command.index('_published = window;', clear)
        if not (pending < show < loaded < exact < clear < publish):
            fail("Project Properties must retain pending owner through host show/Loaded/exact-owner proof before publication")
    except ValueError as exc:
        fail("Project Properties publication ordering marker missing: " + str(exc))

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

    if "+ ex.Message" in command:
        fail("Project Properties must not expose raw host exception messages")

    require(v26_project, '<Compile Include="..\\QS3D.BricsCAD.V25\\**\\*.cs"', "V26 shared adapter source")

    print("PASS: BLT3D Project Properties stays dedicated/read-only and uses exact pending-owned, Loaded-before-publication singleton lifecycle.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
