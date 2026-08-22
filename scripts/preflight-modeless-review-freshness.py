#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
REVIEW = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
READER = ROOT / "src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs"
errors = []

for path in (REVIEW, READER):
    if not path.is_file():
        errors.append("missing modeless freshness contract file: " + str(path.relative_to(ROOT)))

if REVIEW.is_file():
    text = REVIEW.read_text(encoding="utf-8")

    for token in (
        'LocateCurrentElement(doc, row.ElementId, "BBS Locate")',
        'LocateCurrentElement(doc, row.ElementId, "Revision Locate")',
<<<<<<< Updated upstream
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
=======
        "if (!ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject))",
>>>>>>> Stashed changes
        "var element = currentProject.FindElement(elementId)",
        "SourceHandleResolver.Resolve(currentProject, new[] { element.Id })",
        "Application.DocumentManager.MdiActiveDocument, document",
    ):
        if token not in text:
            errors.append("BBS/Revision modeless Locate missing current-project read-only re-resolution token: " + token)

    helper_start = text.find("private static int LocateCurrentElement")
    helper_end = text.find("private static HashSet<string> CollectGeneratedHandles", helper_start)
    helper = text[helper_start:helper_end] if helper_start >= 0 and helper_end > helper_start else ""
    if "ProjectContextCoordinator.GetOrCreate(document)" in helper:
        errors.append("BBS/Revision modeless Locate must not create/cache replacement project state")

    apply_pos = text.find("Action<RecognitionResult> apply = result =>")
    locate_pos = text.find("Action<RecognitionResult> locate = result =>", apply_pos)
    apply_body = text[apply_pos:locate_pos] if apply_pos >= 0 and locate_pos > apply_pos else ""
    for token in (
        "EntitySnapshotReader.ReadHandles(doc, new[] { result.Handle })",
        "var currentProject = ProjectContextCoordinator.GetOrCreate(doc);",
        "new ProjectRecognitionService().Suggest(currentProject, liveSnapshots[0])",
        "candidate.Category != expectedCandidate.Category",
        "!refreshed.IsCaptureReady",
        "SemanticCaptureService.CaptureSnapshot(doc, refreshed.Snapshot, candidate.Category)",
        "AuditTrail.ForProject(currentProject).Record",
        "đã commit; UI refresh warning:",
    ):
        if token not in apply_body:
            errors.append("Recognition modeless Apply missing live-state token: " + token)

    for forbidden in (
        "SemanticCaptureService.CaptureSnapshot(doc, result.Snapshot",
        "SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, result.Handle)",
        "AuditTrail.ForProject(project).Record(\"recognition.apply\"",
    ):
        if forbidden in apply_body:
            errors.append("Recognition modeless Apply still uses stale captured state: " + forbidden)

if READER.is_file():
    text = READER.read_text(encoding="utf-8")
    read_handles_pos = text.find("public static IReadOnlyList<EntitySnapshot> ReadHandles")
    read_selection_pos = text.find("private static IReadOnlyList<EntitySnapshot> ReadSelection", read_handles_pos)
    read_handles_body = text[read_handles_pos:read_selection_pos] if read_handles_pos >= 0 and read_selection_pos > read_handles_pos else ""
    for token in (
        "CadHandleService.Resolve(document, handles)",
        "StartOpenCloseTransaction()",
        "AddSnapshot(transaction, id, result)",
    ):
        if token not in read_handles_body:
            errors.append("EntitySnapshotReader.ReadHandles missing live CAD read token: " + token)
    if "SetImpliedSelection" in read_handles_body or "GetSelection" in read_handles_body:
        errors.append("EntitySnapshotReader.ReadHandles must not mutate or prompt for PICKFIRST selection")

if errors:
    for error in errors:
        print("[FAIL] " + error)
    sys.exit(1)

print("[PASS] BBS/Revision Locate re-resolve current semantic state read-only and Recognition Apply re-reads live CAD before modeless commit")
