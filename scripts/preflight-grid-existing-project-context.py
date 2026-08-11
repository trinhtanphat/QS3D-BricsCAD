#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "QS3D.BricsCAD.V25"
errors = []

contracts = {
    "GridAnnotationCommands.cs": (
        'CommandMethod("QS3DGRIDANNOTATE"',
        'CommandMethod("QS3DGRIDANNOTATEALL"',
        'ExistingProjectMutationContext.Require(document, "Grid Annotation")',
        'ExistingProjectMutationContext.Require(document, "Grid Annotation All")',
        "if (snapshots.Count == 0) return;",
    ),
    "GridNamingCommands.cs": (
        'CommandMethod("QS3DGRIDNUMBER"',
        'ExistingProjectMutationContext.Require(document, "Grid Renumber")',
        "ProjectStateSnapshot.Capture(project)",
        "rollback.Restore(project)",
        "GridAnnotationBuilder.Build(document, project",
    ),
}

for filename, tokens in contracts.items():
    path = SRC / filename
    if not path.is_file():
        errors.append("missing " + filename)
        continue
    text = path.read_text(encoding="utf-8")
    for token in tokens:
        if token not in text:
            errors.append(filename + ": missing lifecycle token: " + token)
    if "ProjectContextCoordinator.GetOrCreate(document)" in text:
        errors.append(filename + ": Grid mutation must not create/cache project state directly")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Grid renumber/annotation require canonical existing QS3D project state while selection cancel and rollback behavior remain intact.")
