#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "CurtainWallWindow.xaml.cs"


def method_slice(text: str, name: str, next_name: str | None = None) -> str:
    marker = f"private void {name}("
    start = text.find(marker)
    if start < 0:
        return ""
    if next_name:
        end = text.find(f"private void {next_name}(", start + len(marker))
        if end >= 0:
            return text[start:end]
    return text[start:]


def require(condition: bool, message: str, failures: list[str]) -> None:
    if not condition:
        failures.append(message)


def main() -> int:
    text = SOURCE.read_text(encoding="utf-8")
    failures: list[str] = []

    save = method_slice(text, "OnSaveClick", "OnRecalculateClick")
    recalc = method_slice(text, "OnRecalculateClick", "OnCommandClick")
    refresh = method_slice(text, "RefreshAll", "LoadSelectedFamily")
    summary = method_slice(text, "RefreshSummary", "ClearProjectView")
    clear = method_slice(text, "ClearProjectView", "ClearSummary")
    bound_guard = method_slice(text, "EnsureBoundProject", "ApplyFamilyValue")

    require("private ProjectState? _boundProject;" in text, "missing exact ProjectState binding field", failures)
    require(text.count("_boundProject = project;") == 1, "project binding must occur exactly once", failures)
    require("_boundProject = project;" in refresh, "RefreshAll must establish the exact project binding", failures)
    require("_boundProject = null;" in refresh, "RefreshAll must invalidate any previous binding before/following a failed refresh", failures)
    require("_boundProject = null;" in clear, "ClearProjectView must clear the project binding", failures)

    require("EnsureBoundProject(project, \"lưu Family Vách Kính\");" in save, "Save must reject a stale/replaced project before mutation", failures)
    require("EnsureBoundProject(project, \"tính lại Vách Kính\");" in recalc, "Recalculate must reject a stale/replaced project before mutation", failures)
    require("ReferenceEquals(_boundProject, project)" in bound_guard, "exact project guard must use reference identity", failures)
    require("_boundProject == null" in bound_guard, "exact project guard must fail closed before first successful refresh", failures)

    require("ProjectContextCoordinator.TryGetReadOnly(_document, out var project)" in summary, "summary refresh must resolve the canonical read-only project", failures)
    require("ReferenceEquals(_boundProject, project)" in summary, "summary refresh must reject replacement-project data", failures)

    require("ExistingProjectMutationContext.TryGet(_document, out var project)" in save, "Save must preserve canonical mutation-context resolution", failures)
    require("ExistingProjectMutationContext.TryGet(_document, out var project)" in recalc, "Recalculate must preserve canonical mutation-context resolution", failures)
    require("ProjectStateSnapshot.Capture(project)" in save and "ProjectStateSnapshot.Capture(project)" in recalc, "rollback snapshots must remain on both mutation paths", failures)
    require("RestoreOrThrow(project, rollback" in save and "RestoreOrThrow(project, rollback" in recalc, "rollback restore guards must remain on both mutation paths", failures)
    require("RegenerateDirty(project)" in save and "RegenerateDirty(project)" in recalc, "dirty regeneration must remain on both mutation paths", failures)
    require("PaletteCoordinator.RefreshProject();" in save and "PaletteCoordinator.RefreshProject();" in recalc, "post-commit UI synchronization must remain", failures)
    require("SendStringToExecute" in text, "existing command dispatch must remain available", failures)
    require("Viewport3D" not in text, "Curtain Wall Hub must not introduce a parallel/fake viewport", failures)

    require("_boundProject = project;" not in save, "Save must never silently rebind to a replacement project", failures)
    require("_boundProject = project;" not in recalc, "Recalculate must never silently rebind to a replacement project", failures)

    if failures:
        for failure in failures:
            print("FAIL:", failure)
        return 1

    print("PASS: CurtainWallWindow exact ProjectState identity guard is source-safe")
    return 0


if __name__ == "__main__":
    sys.exit(main())
