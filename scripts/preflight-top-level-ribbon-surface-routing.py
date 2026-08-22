#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = "src/QS3D.BricsCAD.V25/Ribbon/BltBimWorkspaceActivationCoordinator.cs"


def fail(message):
    print("ERROR:", message)
    raise SystemExit(1)


def require(text, needle, label):
    if needle not in text:
        fail(f"{label}: expected source contract not found: {needle}")


def section(text, start, end):
    start_at = text.find(start)
    if start_at < 0:
        fail(f"missing section start: {start}")
    end_at = text.find(end, start_at + len(start))
    if end_at < 0:
        fail(f"missing section end: {end}")
    return text[start_at:end_at]


def main():
    path = ROOT / SOURCE
    if not path.is_file():
        fail(f"missing required source: {SOURCE}")
    activation = path.read_text(encoding="utf-8")

    for token in (
        'private const string HomeTabId = "QS3D_HOME";',
        'private const string ProjectTabId = "QS3D_PROJECT";',
        'private const string BimTabId = "QS3D_BIM";',
        'RouteHomeSurface();',
        'RouteProjectSurface();',
        'ReassertBimWorkspace();',
    ):
        require(activation, token, "top-level Ribbon surface routes")

    home = section(activation, "private static void RouteHomeSurface()", "private static void RouteProjectSurface()")
    require(home, "ProjectSetupPaletteCoordinator.Hide();", "HOME releases Project surface")
    require(home, "PaletteCoordinator.Hide();", "HOME releases BIM workspace")
    require(home, "StartCenterPaletteCoordinator.Show();", "HOME opens Start Center")

    project = section(activation, "private static void RouteProjectSurface()", "private static bool ReassertBimWorkspace()")
    require(project, "StartCenterPaletteCoordinator.Hide();", "PROJECT releases Start Center")
    require(project, "PaletteCoordinator.Hide();", "PROJECT releases BIM workspace")
    require(project, "ProjectSetupPaletteCoordinator.ShowProjectInformation();", "PROJECT opens Project Information")

    bim = section(activation, "private static bool ReassertBimWorkspace()", "private static string ResolveCurrentTabId")
    require(bim, "StartCenterPaletteCoordinator.Hide();", "BIM releases Start Center")
    require(bim, "ProjectSetupPaletteCoordinator.Hide();", "BIM releases Project surface")
    require(bim, "PaletteCoordinator.ShowBimWorkspace();", "BIM opens Workspace")

    resolver = section(activation, "private static string ResolveCurrentTabId", "private static string TabId")
    selected_index = resolver.find('new[] { "SelectedTabIndex", "SelectedIndex", "CurrentTabIndex" }')
    active_marker = resolver.find('ReadBool(tab, "IsActive")')
    direct_fallback = resolver.find('new[] { "SelectedTab", "ActiveTab", "CurrentTab" }')
    if selected_index < 0 or active_marker < 0 or direct_fallback < 0:
        fail("tab resolver must expose selected-index, active-marker, and direct-property fallback paths")
    if not (selected_index < direct_fallback and active_marker < direct_fallback):
        fail("tab resolver must prefer explicit selected/active evidence before direct Ribbon properties")

    for forbidden in ("SendStringToExecute", "CommandMethod(", "ProjectState", "ProjectContextCoordinator"):
        if forbidden in activation:
            fail(f"presentation-only top-level routing must not mutate CAD/project state: {forbidden}")

    print("PASS: HOME/PROJECT/BIM top-level tabs own mutually exclusive dedicated surfaces with robust selected-tab resolution.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
