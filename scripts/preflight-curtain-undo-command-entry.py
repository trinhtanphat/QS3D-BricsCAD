#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SNAPSHOT = ROOT / "src" / "QS3D.Core" / "Persistence" / "ProjectStateSnapshot.cs"
COORD = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallUndoCoordinator.cs"
BUILD = ROOT / "src" / "QS3D.BricsCAD.V25" / "CurtainWallBuildCommands.cs"
SMOKE = ROOT / "tests" / "QS3D.Core.SmokeTests" / "CurtainUndoElementSnapshotSmoke.cs"
REG = ROOT / "tests" / "QS3D.Core.SmokeTests" / "CurtainUndoElementSnapshotRegistration.cs"
errors = []

for path in (SNAPSHOT, COORD, BUILD, SMOKE, REG):
    if not path.is_file():
        errors.append("missing command-entry Undo dependency: " + str(path.relative_to(ROOT)))

snapshot = SNAPSHOT.read_text(encoding="utf-8") if SNAPSHOT.is_file() else ""
coord = COORD.read_text(encoding="utf-8") if COORD.is_file() else ""
build = BUILD.read_text(encoding="utf-8") if BUILD.is_file() else ""
smoke = SMOKE.read_text(encoding="utf-8") if SMOKE.is_file() else ""
reg = REG.read_text(encoding="utf-8") if REG.is_file() else ""

for token in (
    "public static ElementStateSnapshot CaptureElement(ProjectState project, string elementId)",
    "public bool Matches(ProjectState project)",
    "public void Restore(ProjectState project)",
):
    if token not in snapshot:
        errors.append("scoped element snapshot contract missing: " + token)

for token in (
    "ProjectStateSnapshot.ElementStateSnapshot",
    "ProjectStateSnapshot.CaptureElement(project, element.Id)",
    "ElementState.Restore(elementProject)",
):
    if token not in coord:
        errors.append("Curtain owner command-entry snapshot contract missing: " + token)

capture = build.find("CurtainWallUndoCoordinator.OwnerStateSnapshot.CaptureSelectedOwners(")
begin = build.find("CurtainWallUndoCoordinator.BeginTransition(document, project, undoBefore)")
regen = build.find("RegenerateDirty(project)")
line_host = build.find("WallSolidBuilder.BuildSelectedLineWalls")
if min(capture, begin, regen, line_host) < 0:
    errors.append("cannot establish Curtain command-entry ordering")
elif not (capture < begin < regen < line_host):
    errors.append("Curtain Undo must capture/register exact command-entry owner state before semantic regeneration and native mutation")

for token in (
    "RestoresExactOwnerWithoutTouchingUnrelatedElement();",
    "RefusesReplacementElementGenerationBeforeMutation();",
    "CurtainUndoElementSnapshotSmoke.Run();",
):
    if token not in (smoke + "\n" + reg):
        errors.append("command-entry scoped snapshot smoke contract missing: " + token)

if errors:
    for error in errors:
        print("ERROR: " + error)
    sys.exit(1)

print("PASS: Curtain Undo command-entry semantic snapshot contract")
