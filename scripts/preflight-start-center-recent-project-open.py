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

failure_text = '_statusText.Text = "Không thể mở dự án gần đây an toàn. Hãy kiểm tra tệp và thử lại.";'
required_markers = (
    "StartCenterUserStateStore.TryNormalizeDwgPath(recent.Path, out var normalized)",
    "File.Exists(normalized)",
    "ProjectFileUiService.OpenProject(normalized);",
    "StartCenterUserStateStore.RecordProject(normalized);",
    "RefreshRecentProjects();",
    "catch (Exception)",
    failure_text,
    '_statusText.Text = "Đã mở " + Path.GetFileName(normalized) + ".";',
)
for marker in required_markers:
    require(marker in snippet, f"OpenRecentProject must retain expected behavior: {marker}")

forbidden_markers = (
    "Application.DocumentManager.Open(",
    "ProjectContextCoordinator.GetOrCreate(",
    "ex.Message",
    ".Message",
)
for marker in forbidden_markers:
    require(marker not in snippet, f"OpenRecentProject must not bypass/redact the shared project-open route: {marker}")

open_index = snippet.index("ProjectFileUiService.OpenProject(normalized);")
failure_index = snippet.index(failure_text)
success_index = snippet.index('_statusText.Text = "Đã mở " + Path.GetFileName(normalized) + ".";')
record_index = snippet.index("StartCenterUserStateStore.RecordProject(normalized);")

require(
    open_index < failure_index < success_index < record_index,
    "Open failure handling must finish before success is declared and before recent-project bookkeeping begins.",
)
require(
    "return;" in snippet[failure_index:success_index],
    "A failed shared open must return before the success/bookkeeping path.",
)
require(
    "catch (Exception)" not in snippet[success_index:record_index],
    "The open-failure catch must not wrap post-open recent-project bookkeeping.",
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

print("[OK] Start Center recent-project opens preserve shared routing, redact open failures, and cannot be misreported by post-open bookkeeping failures.")
