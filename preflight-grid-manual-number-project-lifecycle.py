#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "GridNamingCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: src/QS3D.BricsCAD.V25/GridNamingCommands.cs")
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void RenumberGrid()")
end = text.find("private static IReadOnlyList<string> CaptureAnnotatedGridIds", start + 1) if start >= 0 else -1
body = text[start:end] if start >= 0 and end > start else ""
if not body:
    errors.append("cannot isolate QS3DGRIDNUMBER command body")
else:
    tokens = (
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "expectedProjectId = previewProject.ProjectId",
        "AcquireOrderedGridSelection(document, previewProject)",
        "AcquireOptions(document.Editor)",
        "ExistingProjectMutationContext.Require(document, \"Grid Renumber\")",
        "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
        "ResolveOrderedGridIds(project, selectionPlan.Handles)",
        "SameGridIdentityPlan(selectionPlan.ElementIds, orderedIds)",
        "CaptureAnnotatedGridIds(project, orderedIds)",
        "ProjectStateSnapshot.Capture(project)",
        "GridNamingService.Renumber(project, orderedIds, options)",
    )
    positions = [body.find(token) for token in tokens]
    if any(position < 0 for position in positions):
        errors.append("QS3DGRIDNUMBER missing read-only-input/canonical-freshness/mutation lifecycle token")
    elif positions != sorted(positions):
        errors.append("QS3DGRIDNUMBER must finish Grid selection and naming input on read-only preview before canonical binding/re-resolution/mutation")
    if "ProjectContextCoordinator.GetOrCreate" in body:
        errors.append("QS3DGRIDNUMBER must not create a replacement project")

for token in (
    "private static GridSelectionPlan? AcquireOrderedGridSelection(Document document, ProjectState previewProject)",
    "var result = editor.GetEntity(promptOptions)",
    "previewProject.Elements",
    "handles.Add(handle)",
    "elementIds.Add(element.Id)",
    "private static IReadOnlyList<string> ResolveOrderedGridIds(ProjectState project, IReadOnlyList<string> handles)",
    "project.Elements",
    "private static bool SameGridIdentityPlan(",
    "string.Equals(previewIds[i], currentIds[i], StringComparison.OrdinalIgnoreCase)",
):
    if token not in text:
        errors.append("Grid manual renumber lifecycle/freshness guard missing: " + token)

selection_start = text.find("private static GridSelectionPlan? AcquireOrderedGridSelection")
selection_end = text.find("private static IReadOnlyList<string> ResolveOrderedGridIds", selection_start + 1) if selection_start >= 0 else -1
selection = text[selection_start:selection_end] if selection_start >= 0 and selection_end > selection_start else ""
for forbidden in ("ExistingProjectMutationContext", "ProjectContextCoordinator.GetOrCreate", "ProjectStateSnapshot", "GridNamingService.Renumber"):
    if forbidden in selection:
        errors.append("manual Grid selection helper must remain non-mutating/read-only: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DGRIDNUMBER uses an existing-project read-only preview for ordered Grid selection and naming prompts, binds canonical state only after input completes, then re-resolves stable source handles/semantic IDs before snapshot, renumber and annotation rebuild.")
