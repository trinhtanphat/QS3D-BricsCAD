#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PANEL_REL = "src/QS3D.BricsCAD.V25/UI/BltStartCenterPanel.cs"
PROJECT_UI_REL = "src/QS3D.BricsCAD.V25/ProjectFileUiService.cs"


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden stale contract: {needle}")


def main():
    panel = read(PANEL_REL)
    project_ui = read(PROJECT_UI_REL)

    marker = "private void OpenRecentProject(StartCenterRecentProject recent)"
    next_marker = "private void RunUiAction(Action action)"
    require(panel, marker, PANEL_REL)
    require(panel, next_marker, PANEL_REL)
    block = panel.split(marker, 1)[1].split(next_marker, 1)[0]

    for needle in (
        "StartCenterUserStateStore.TryNormalizeDwgPath(recent.Path, out var normalized)",
        "File.Exists(normalized)",
        "ProjectFileUiService.OpenProject(normalized);",
        "StartCenterUserStateStore.RecordProject(normalized);",
        '_statusText.Text = "Đã mở " + Path.GetFileName(normalized) + ".";',
        "RefreshRecentProjects();",
        '_statusText.Text = "Không thể mở: " + ex.Message;',
    ):
        require(block, needle, PANEL_REL + "::OpenRecentProject")

    for stale in (
        "Application.DocumentManager.Open(normalized, false)",
        "Application.DocumentManager.MdiActiveDocument",
        "ProjectContextCoordinator.GetOrCreate",
    ):
        forbid(block, stale, PANEL_REL + "::OpenRecentProject")

    if block.index("ProjectFileUiService.OpenProject(normalized);") > block.index("StartCenterUserStateStore.RecordProject(normalized);"):
        raise SystemExit("FAIL: recent-project state must only be recorded after the shared open route succeeds.")

    for needle in (
        'if (string.Equals(extension, ".dwg", StringComparison.OrdinalIgnoreCase))',
        "OpenDrawing(fullProjectPath);",
        "if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))",
        "ProjectOperationResultWindow.ShowOpenSuccess(",
    ):
        require(project_ui, needle, PROJECT_UI_REL)

    open_drawing = project_ui.split("private static void OpenDrawing(string drawingPath)", 1)[1].split(
        "private static void PublishSelectedProject(", 1
    )[0]
    forbid(open_drawing, "ProjectContextCoordinator.GetOrCreate(", PROJECT_UI_REL + "::OpenDrawing")

    print("PASS: Start Center recent-project DWG opens reuse the shared project-open route, preserve recent/status behavior, and never manufacture a project solely for popup parity.")


if __name__ == "__main__":
    main()
