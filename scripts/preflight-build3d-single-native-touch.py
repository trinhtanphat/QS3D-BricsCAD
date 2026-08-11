#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/Build3DCommands.cs"
BUILDERS = (
    (ROOT / "src/QS3D.BricsCAD.V25/Cad/StructuralSolidBuilder.cs", "StructuralSolidBuilder"),
    (ROOT / "src/QS3D.BricsCAD.V25/Cad/WallSolidBuilder.cs", "WallSolidBuilder"),
    (ROOT / "src/QS3D.BricsCAD.V25/Cad/PolylineWallSolidBuilder.cs", "PolylineWallSolidBuilder"),
    (ROOT / "src/QS3D.BricsCAD.V25/Cad/WallPierProfileSolidBuilder.cs", "WallPierProfileSolidBuilder"),
)
errors = []


def read(path, label):
    if not path.is_file():
        errors.append("missing " + label)
        return ""
    return path.read_text(encoding="utf-8")


command = read(COMMAND, "Build3DCommands.cs")
if command:
    for token in (
        "var semanticRollback = ProjectStateSnapshot.Capture(project);",
        "var ownershipBefore = CaptureGeneratedSolidHandles(project, elementIds);",
        "document.Editor.SetImpliedSelection(sourceIds.ToArray());",
        "built = BuildCategory(document, project, category, sourceType);",
        "if (GeneratedSolidHandlesMatch(project, ownershipBefore))",
        "semanticRollback.Restore(project);",
        "FinalizeUi(document, elementIds, sourceHandles, built, regenerated, category, project);",
    ):
        if token not in command:
            errors.append("Build3D command contract missing token: " + token)

    build_call = command.find("built = BuildCategory(document, project, category, sourceType);")
    finalize = command.find("FinalizeUi(document, elementIds, sourceHandles, built, regenerated, category, project);", build_call + 1)
    if build_call < 0 or finalize <= build_call:
        errors.append("could not isolate successful BuildCategory -> FinalizeUi path")
    else:
        post_builder = command[build_call:finalize]
        if "project.Touch();" in post_builder:
            errors.append("QS3DBUILD3D must not add a second project Touch after native ownership has committed.")

    for token in (
        "WallPierProfileSolidBuilder.BuildSelectedLinePiers(document, project)",
        "WallSolidBuilder.BuildSelectedLineWalls(document, project, category)",
        "PolylineWallSolidBuilder.BuildSelected(document, project, category)",
        "StructuralSolidBuilder.BuildSelected(document, project, category)",
    ):
        if token not in command:
            errors.append("BuildCategory dispatch missing native builder: " + token)

for path, label in BUILDERS:
    text = read(path, label + ".cs")
    if not text:
        continue
    for token in (
        "ProjectStateSnapshot.Capture(project)",
        "if (pending.Count > 0) project.Touch();",
        "transaction.Commit();",
    ):
        if token not in text:
            errors.append(label + " must retain native ownership commit token: " + token)
    touch = text.find("if (pending.Count > 0) project.Touch();")
    commit = text.find("transaction.Commit();", touch + 1) if touch >= 0 else -1
    if touch < 0 or commit <= touch:
        errors.append(label + " must advance ChangeVersion inside its rollback-capable semantic/CAD commit boundary.")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DBUILD3D delegates the single native ownership revision to the selected builder, while preserving semantic rollback, PICKFIRST handoff and post-commit UI isolation.")
