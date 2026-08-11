#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReviewCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: src/QS3D.BricsCAD.V25/ReviewCommands.cs")
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

start = text.find("private static void RecognizeInternal(bool autoApply, bool scanCurrentSpace)")
end = text.find("[CommandMethod(\"QS3DREVBASE\"", start + 1) if start >= 0 else -1
body = text[start:end] if start >= 0 and end > start else ""

if not body:
    errors.append("cannot isolate Recognition command family")
else:
    tokens = (
        "EntitySnapshotReader.ReadCurrentSpace(doc)",
        "EntitySnapshotReader.ReadCurrentSelection(doc)",
        "ProjectContextCoordinator.TryGetReadOnly(doc, out var previewProject)",
        "if (snapshots.Count == 0)",
        "DrawingUnitWorkflow.EnsureResolved(doc, operation)",
        "ExistingProjectMutationContext.Require(doc, operation + \" recognition\")",
        "ProjectContextCoordinator.GetOrCreate(doc)",
        "new ProjectRecognitionService().SuggestBatch(project, snapshots)",
    )
    for token in tokens:
        if token not in body:
            errors.append("Recognition lifecycle token missing: " + token)

    read_space = body.find("EntitySnapshotReader.ReadCurrentSpace(doc)")
    empty = body.find("if (snapshots.Count == 0)")
    units = body.find("DrawingUnitWorkflow.EnsureResolved(doc, operation)")
    require_existing = body.find("ExistingProjectMutationContext.Require(doc, operation + \" recognition\")")
    create_new = body.find("ProjectContextCoordinator.GetOrCreate(doc)")
    suggest = body.find("new ProjectRecognitionService().SuggestBatch(project, snapshots)")
    if min(read_space, empty, units, require_existing, create_new, suggest) >= 0:
        if not (read_space < empty < units):
            errors.append("Recognition must acquire/guard CAD input before unit resolution/project bootstrap")
        if not (empty < require_existing < suggest and empty < create_new < suggest):
            errors.append("Recognition project binding/creation must occur only after usable CAD input exists")

for token in (
    "expectedProjectId = previewProject.ProjectId",
    "CollectGeneratedHandles(previewProject)",
    "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
    "QS3D project đã thay đổi trong lúc quét CAD source",
):
    if token not in text:
        errors.append("B4D read-only/canonical freshness guard missing: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: Recognition reads and rejects empty CAD input before unit/project bootstrap; B4D filters generated handles from read-only state and rebinds the same canonical ProjectId before mutation; valid source remains creation-capable when no project exists.")
