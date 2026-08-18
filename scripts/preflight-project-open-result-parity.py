#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT_UI_REL = "src/QS3D.BricsCAD.V25/ProjectFileUiService.cs"
RESULT_REL = "src/QS3D.BricsCAD.V25/UI/ProjectOperationResultWindow.cs"


def read(rel):
    return (ROOT / rel).read_text(encoding="utf-8")


def require(text, needle, rel):
    if needle not in text:
        raise SystemExit(f"FAIL: {rel} missing required contract: {needle}")


def forbid(text, needle, rel):
    if needle in text:
        raise SystemExit(f"FAIL: {rel} contains forbidden contract: {needle}")


def main():
    project_ui = read(PROJECT_UI_REL)
    result = read(RESULT_REL)

    open_drawing_marker = "private static void OpenDrawing(string drawingPath)"
    next_marker = "private static void PublishSelectedProject("
    require(project_ui, open_drawing_marker, PROJECT_UI_REL)
    require(project_ui, next_marker, PROJECT_UI_REL)
    open_drawing = project_ui.split(open_drawing_marker, 1)[1].split(next_marker, 1)[0]

    for needle in (
        "var total = Stopwatch.StartNew();",
        "var bind = Stopwatch.StartNew();",
        "Application.DocumentManager.Open(drawingPath, false)",
        "Application.DocumentManager.MdiActiveDocument = document;",
        "var read = Stopwatch.StartNew();",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "PaletteCoordinator.RefreshProject();",
        "ProjectOperationResultWindow.ShowOpenSuccess(",
        "read.ElapsedMilliseconds",
        "bind.ElapsedMilliseconds",
        "total.ElapsedMilliseconds",
    ):
        require(open_drawing, needle, PROJECT_UI_REL + "::OpenDrawing")

    forbid(open_drawing, "ProjectContextCoordinator.GetOrCreate(", PROJECT_UI_REL + "::OpenDrawing")

    if open_drawing.index("ProjectContextCoordinator.TryGetReadOnly(document, out var project)") > open_drawing.index(
        "ProjectOperationResultWindow.ShowOpenSuccess("
    ):
        raise SystemExit("FAIL: DWG project detection must happen before the success popup.")

    if "if (!ProjectContextCoordinator.TryGetReadOnly(document, out var project))" not in open_drawing:
        raise SystemExit("FAIL: bare DWG opens must remain valid without manufacturing a QS3D sidecar.")

    for needle in (
        "ShowOpenSuccess",
        'var summary = "Đã mở',
        'Text = "✓"',
        'Content = "OK"',
        "WindowStyle = System.Windows.WindowStyle.None",
        "AllowsTransparency = true",
    ):
        require(result, needle, RESULT_REL)

    print("PASS: project-open success popup covers .blt3d/.qsdb and DWG-with-existing-sidecar routes without creating a project for a bare DWG.")


if __name__ == "__main__":
    main()
