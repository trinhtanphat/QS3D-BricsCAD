#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"

COMMANDS = {
    "RebarHealthCommands.cs": "currentProject.FindElement(issue.ElementId)",
    "RebarModeHealthCommands.cs": "currentProject.FindElement(issue.ElementId)",
    "ColumnTieHealthCommands.cs": "currentProject.FindElement(issue.ElementId)",
    "ShapeRebarHealthCommands.cs": "currentProject.FindElement(issue.ElementId)",
    "FoundationMeshHealthCommands.cs": "currentProject.FindElement(issue.ElementId)",
    "CurtainWallFrameHealthCommands.cs": "currentProject.FindElement(issue.ElementId)",
    "RoomFinishHealthCommands.cs": "SourceHandleResolver.Resolve(currentProject, new[] { issue.ElementId })",
    "StructuralWallMeshHealthCommands.cs": None,
    "SemanticTagHealthCommands.cs": None,
}

MIXED_HEALTH_COMMANDS = {
    "BeamStirrupCommands.cs": (
        "public void BeamStirrupHealth()",
        "private static void FinalizeUi",
    ),
    "SlabMeshCommands.cs": (
        "public void SlabMeshHealth()",
        "private static void FinalizeUi",
    ),
}

errors = []

for name, current_locate_token in COMMANDS.items():
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

    if current_locate_token:
        if "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)" not in text:
            errors.append(name + " Locate callback must re-resolve the current read-only project at click time.")
        if current_locate_token not in text:
            errors.append(name + " Locate callback must resolve CAD identity from the current project: " + current_locate_token)
        for stale in (
            "var element = project.FindElement(issue.ElementId);",
            "SourceHandleResolver.Resolve(project, new[] { issue.ElementId })",
        ):
            if stale in text:
                errors.append(name + " Locate callback must not use the ProjectState captured when the window opened: " + stale)

for name, (start_marker, end_marker) in MIXED_HEALTH_COMMANDS.items():
    path = SRC / name
    if not path.is_file():
        errors.append("missing mixed authoring/Health command source: " + str(path.relative_to(ROOT)))
        continue

    text = path.read_text(encoding="utf-8")
    start = text.find(start_marker)
    end = text.find(end_marker, start + len(start_marker)) if start >= 0 else -1
    if start < 0 or end < 0:
        errors.append(name + " Health method boundaries changed; update the regression gate before accepting lifecycle changes.")
        continue

    health = text[start:end]
    if "ProjectContextCoordinator.TryGetReadOnly(document, out var project)" not in health:
        errors.append(name + " Health method must resolve project state with TryGetReadOnly.")
<<<<<<< HEAD
    if "ProjectContextCoordinator.GetOrCreate" in health or "ExistingProjectMutationContext." in health:
        errors.append(name + " Health method must not create/cache or bind mutable project state; mutation context belongs only to explicit authoring methods.")
=======
    if "ProjectContextCoordinator.GetOrCreate" in health or "ExistingProjectMutationContext" in health:
        errors.append(name + " Health method must remain read-only and must not bind/create mutation state.")
>>>>>>> origin/main
    if "lệnh kiểm tra không tạo project mới" not in health:
        errors.append(name + " Health method must explain that blocked inspection does not create a project.")

    authoring = text[:start]
<<<<<<< HEAD
    if "ExistingProjectMutationContext.Require(document," not in authoring:
        errors.append(name + " explicit authoring path must bind a canonical existing project without cold-creating state; review command lifecycle instead of weakening this gate.")
=======
    if "ExistingProjectMutationContext.Require(document" not in authoring:
        errors.append(name + " explicit authoring path must bind canonical existing project state before native mutation.")
    if "ProjectContextCoordinator.GetOrCreate(document)" in authoring:
        errors.append(name + " explicit authoring path must not directly create/cache replacement project state.")
>>>>>>> origin/main

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

<<<<<<< HEAD
print("PASS: focused, mixed authoring/Health, and comprehensive Health inspections are read-only; mixed explicit authoring binds canonical existing project state, and modeless Locate callbacks re-resolve current project state by stable identity.")
=======
print("PASS: focused, mixed authoring/Health, and comprehensive Health inspections are read-only; explicit authoring binds canonical existing project state, and modeless Locate callbacks re-resolve current project state by stable identity.")
>>>>>>> origin/main
