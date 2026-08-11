#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"

COMMANDS = {
    "RebarHealthCommands.cs": True,
    "ColumnTieHealthCommands.cs": True,
    "ShapeRebarHealthCommands.cs": True,
    "FoundationMeshHealthCommands.cs": True,
    "StructuralWallMeshHealthCommands.cs": False,
    "CurtainWallFrameHealthCommands.cs": True,
    "RebarModeHealthCommands.cs": True,
}

errors = []

for name, has_modeless_locate in COMMANDS.items():
    path = SRC / name
    if not path.is_file():
        errors.append("missing Health command source: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in text:
        errors.append(name + " must resolve the inspection snapshot with TryGetReadOnly.")
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append(name + " must not create/cache project state merely to run a Health inspection.")
    if "lệnh kiểm tra không tạo project mới" not in text:
        errors.append(name + " must explain that a blocked read-only Health inspection does not create a project.")

    if has_modeless_locate:
        if "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)" not in text:
            errors.append(name + " Locate callback must re-resolve the current read-only project at click time.")
        if "currentProject.FindElement(issue.ElementId)" not in text:
            errors.append(name + " Locate callback must resolve the selected element from the current project.")
        if "var element = project.FindElement(issue.ElementId);" in text:
            errors.append(name + " Locate callback must not use the ProjectState captured when the window opened.")

room_finish = SRC / "RoomFinishHealthCommands.cs"
if not room_finish.is_file():
    errors.append("missing RoomFinishHealthCommands.cs")
else:
    text = room_finish.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "SourceHandleResolver.Resolve(currentProject, new[] { issue.ElementId })",
        "lệnh kiểm tra không tạo project mới",
    ):
        if token not in text:
            errors.append("RoomFinishHealthCommands.cs missing read-only token: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("RoomFinishHealthCommands.cs must not create/cache project state merely to inspect Health.")
    if "SourceHandleResolver.Resolve(project, new[] { issue.ElementId })" in text:
        errors.append("RoomFinishHealthCommands.cs Locate callback must not resolve handles from the snapshot captured at window open.")

semantic_tag = SRC / "SemanticTagHealthCommands.cs"
if not semantic_tag.is_file():
    errors.append("missing SemanticTagHealthCommands.cs")
else:
    text = semantic_tag.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "lệnh kiểm tra không tạo project mới",
    ):
        if token not in text:
            errors.append("SemanticTagHealthCommands.cs missing read-only token: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("SemanticTagHealthCommands.cs must not create/cache project state merely to inspect Health.")

health_all = SRC / "HealthAllCommands.cs"
if not health_all.is_file():
    errors.append("missing HealthAllCommands.cs")
else:
    text = health_all.read_text(encoding="utf-8")
    for token in (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
        "LocateProjectArtifactHandles(currentProject, issue.Code)",
        "currentProject.FindElement(issue.ElementId)",
        "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
    ):
        if token not in text:
            errors.append("HealthAllCommands.cs missing read-only Locate token: " + token)
    if "ProjectContextCoordinator.GetOrCreate" in text:
        errors.append("HealthAllCommands.cs must not create/cache project state merely to inspect Health.")
    for stale in (
        "LocateProjectArtifactHandles(project, issue.Code)",
        "var element = project.FindElement(issue.ElementId);",
        "SourceHandleResolver.Resolve(project, new[] { element.Id })",
    ):
        if stale in text:
            errors.append("HealthAllCommands.cs Locate callback still captures stale project state: " + stale)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: focused and comprehensive Health inspections are read-only, and modeless Locate callbacks re-resolve current project state by stable identity.")
