#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "GridAutoNumberCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: src/QS3D.BricsCAD.V25/GridAutoNumberCommands.cs")
    text = ""
else:
    text = SOURCE.read_text(encoding="utf-8")

start = text.find("public void AutoNumberGrid()")
end = text.find("private static CandidateExtraction ExtractParallelLineCandidates", start + 1) if start >= 0 else -1
body = text[start:end] if start >= 0 and end > start else ""
if not body:
    errors.append("cannot isolate QS3DGRIDNUMBERAUTO command body")
else:
    tokens = (
        "EntitySnapshotReader.ReadCurrentSelection(document)",
        "ProjectContextCoordinator.TryGetReadOnly(document, out var previewProject)",
        "var expectedProjectId = previewProject.ProjectId",
        "ExtractParallelLineCandidates(document, previewProject, selected)",
        "AcquireOrderingAxis(document.Editor)",
        "GridSpatialOrderingPlanner.OrderParallelLines(previewExtraction.Curves, orderingAxis.Value)",
        "AcquireNamingOptions(document.Editor)",
        "ConfirmPlan(document.Editor, previewOrdered, namingOptions)",
        "ExistingProjectMutationContext.Require(document, \"Grid Auto Number\")",
        "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
        "ExtractParallelLineCandidates(document, project, selected)",
        "GridSpatialOrderingPlanner.OrderParallelLines(currentExtraction.Curves, orderingAxis.Value)",
        "SamePreviewPlan(previewOrdered, currentOrdered)",
        "ProjectStateSnapshot.Capture(project)",
        "GridNamingService.Renumber(project, orderedIds, namingOptions)",
    )
    positions = [body.find(token) for token in tokens]
    if any(position < 0 for position in positions):
        errors.append("QS3DGRIDNUMBERAUTO missing read-only-preview/prompt/canonical-freshness/mutation lifecycle token")
    elif positions != sorted(positions):
        errors.append("QS3DGRIDNUMBERAUTO must preview read-only, finish all prompts/confirmation, then canonical-bind, re-read/re-order/freshness-check and only then mutate")

    if "ProjectContextCoordinator.GetOrCreate" in body:
        errors.append("QS3DGRIDNUMBERAUTO must not create a replacement project")

    require_pos = body.find("ExistingProjectMutationContext.Require(document, \"Grid Auto Number\")")
    for prompt_token in (
        "AcquireOrderingAxis(document.Editor)",
        "AcquireNamingOptions(document.Editor)",
        "ConfirmPlan(document.Editor, previewOrdered, namingOptions)",
    ):
        prompt_pos = body.find(prompt_token)
        if min(prompt_pos, require_pos) >= 0 and prompt_pos > require_pos:
            errors.append("Grid Auto Number canonical project binding must occur after prompt/confirmation: " + prompt_token)

for token in (
    "private const double PlanCoordinateFreshnessTolerance = 1e-8d;",
    "private static bool SamePreviewPlan(",
    "preview.Count != current.Count",
    "string.Equals(preview[i].ElementId, current[i].ElementId, StringComparison.OrdinalIgnoreCase)",
    "current[i].Coordinate - preview[i].Coordinate",
    "Math.Abs(delta) > PlanCoordinateFreshnessTolerance",
    "Grid auto-number preview không còn khớp source/ordering hiện tại",
):
    if token not in text:
        errors.append("Grid Auto Number freshness guard missing: " + token)

extract_start = text.find("private static CandidateExtraction ExtractParallelLineCandidates")
extract_end = text.find("private static Point2? AcquireOrderingAxis", extract_start + 1) if extract_start >= 0 else -1
extract = text[extract_start:extract_end] if extract_start >= 0 and extract_end > extract_start else ""
for token in (
    "element.SourceHandles",
    "ResolveHandle(document.Database, snapshot.Handle)",
    "transaction.GetObject(objectId, OpenMode.ForRead, false) as Line",
    "line.StartPoint",
    "line.EndPoint",
    "GridReferenceCurve.Line(",
):
    if token not in extract:
        errors.append("Grid Auto Number canonical freshness must re-read authoritative live Grid LINE geometry: " + token)

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: QS3DGRIDNUMBERAUTO completes read-only preview, ordering/naming prompts and explicit confirmation before canonical project binding; commit re-resolves the same project, re-reads authoritative live Grid LINE geometry, re-orders and rejects stale preview plans before semantic renumber mutation.")
