#!/usr/bin/env python3
from pathlib import Path

root = Path(__file__).resolve().parents[1]
build = (root / "src/QS3D.BricsCAD.V25/CurtainWallBuildCommands.cs").read_text(encoding="utf-8")
coord = (root / "src/QS3D.BricsCAD.V25/CurtainWallUndoCoordinator.cs").read_text(encoding="utf-8")
snapshot = (root / "src/QS3D.Core/Persistence/ProjectElementStateSnapshot.cs")
errors = []

if not snapshot.is_file():
    errors.append("missing bounded ProjectElementStateSnapshot Core primitive")

for token in (
    "ProjectElementStateSnapshot.Capture(project, element.Id)",
    "FullState.Restore(project)",
    "FullState.Matches(project)",
    "HasSameOwnerGenerationSet",
):
    if token not in coord:
        errors.append("Curtain OwnerState full-element contract missing: " + token)

for token in (
    "var undoAdmission = CurtainWallUndoCoordinator.OwnerStateSnapshot.Capture(project, undoBefore.OwnerIds);",
    "CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore, undoAdmission)",
):
    if token not in build:
        errors.append("Curtain command admission split missing: " + token)
capture = build.find("var undoBefore = CurtainWallUndoCoordinator.OwnerStateSnapshot.CaptureSelectedOwners(")
regen = build.find("RegenerateDirty(project)")
admission = build.find("var undoAdmission = CurtainWallUndoCoordinator.OwnerStateSnapshot.Capture(project, undoBefore.OwnerIds);")
begin = build.find("CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore, undoAdmission)")
line_host = build.find("WallSolidBuilder.BuildSelectedLineWalls")
if min(capture, regen, admission, begin, line_host) < 0:
    errors.append("cannot establish command-entry/full-state Curtain ordering")
elif not (capture < regen < admission < begin < line_host):
    errors.append("Curtain Undo must capture target before regeneration, capture current admission after regeneration, then register before native mutation")

for token in (
    "OwnerStateSnapshot before,",
    "OwnerStateSnapshot admission)",
    "before.HasSameOwnerGenerationSet(admission)",
    "if (!admission.Matches(project))",
):
    if token not in coord:
        errors.append("Curtain BeginTransition admission contract missing: " + token)

if "RebindSemanticState" in build or "RebindSemanticState" in coord:
    errors.append("Curtain Undo must not rebind an older target checkpoint to newer semantics")

if errors:
    for error in errors:
        print("FAIL: " + error)
    raise SystemExit(1)
print("PASS: Curtain Undo keeps full command-entry targets separate from post-regeneration admission and restores exact owner generations symmetrically across Undo/Redo.")
