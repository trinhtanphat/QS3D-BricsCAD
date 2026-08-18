#!/usr/bin/env python3
"""Guard the KHỞI ĐẦU recent-project route through the shared open service."""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
START_CENTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "BltStartCenterPanel.cs"
PROJECT_UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "ProjectFileUiService.cs"


def require(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


def method_block(source: str, signature: str) -> str:
    start = source.find(signature)
    require(start >= 0, f"{signature} is missing.")
    brace = source.find("{", start)
    require(brace >= 0, f"{signature} body is missing.")

    depth = 0
    for index in range(brace, len(source)):
        char = source[index]
        if char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return source[start : index + 1]

    raise AssertionError(f"{signature} body is unterminated.")


start_center = START_CENTER.read_text(encoding="utf-8")
project_ui = PROJECT_UI.read_text(encoding="utf-8")

snippet = method_block(
    start_center,
    "private void OpenRecentProject(StartCenterRecentProject recent)",
)

required_markers = (
    "StartCenterUserStateStore.TryNormalizeDwgPath(recent.Path, out var normalized)",
    "File.Exists(normalized)",
    "ProjectFileUiService.OpenProject(normalized);",
    "StartCenterUserStateStore.RecordProject(normalized);",
    "RefreshRecentProjects();",
    "catch (Exception ex)",
)
for marker in required_markers:
    require(marker in snippet, f"OpenRecentProject must retain expected behavior: {marker}")

forbidden_markers = (
    "Application.DocumentManager.Open(",
    "ProjectContextCoordinator.GetOrCreate(",
)
for marker in forbidden_markers:
    require(marker not in snippet, f"OpenRecentProject must not bypass the shared project-open route: {marker}")

require(
    snippet.index("ProjectFileUiService.OpenProject(normalized);")
    < snippet.index("StartCenterUserStateStore.RecordProject(normalized);"),
    "The recent project entry must only be recorded after the shared open service succeeds.",
)

open_drawing = method_block(
    project_ui,
    "private static void OpenDrawing(string drawingPath)",
)

project_route_markers = (
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "ProjectOperationResultWindow.ShowOpenSuccess(",
)
for marker in project_route_markers:
    require(marker in open_drawing, f"Shared DWG open route must preserve project sidecar behavior: {marker}")

print("[OK] Start Center recent projects route through ProjectFileUiService.")
