#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
REVIEW = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
APPLY = ROOT / "src/QS3D.BricsCAD.V25/Services/RecognitionApplyBatchService.cs"
READER = ROOT / "src/QS3D.BricsCAD.V25/Cad/EntitySnapshotReader.cs"
errors = []

for path in (REVIEW, APPLY, READER):
    if not path.is_file():
        errors.append("missing modeless freshness contract file: " + str(path.relative_to(ROOT)))

if REVIEW.is_file():
    text = REVIEW.read_text(encoding="utf-8")

    for token in (
        'LocateCurrentElement(doc, row.ElementId, "BBS Locate")',
        'LocateCurrentElement(doc, row.ElementId, "Revision Locate")',
        "ProjectContextCoordinator.TryGetReadOnly(document, out var currentProject)",
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

    review_project_pos = text.find("var reviewProjectId = project.ProjectId;")
    batch_pos = text.find("var batch = new ProjectRecognitionService().SuggestBatch(project, snapshots)")
    apply_pos = text.find("Func<IReadOnlyList<RecognitionResult>, bool, int> apply")
    if min(review_project_pos, batch_pos, apply_pos) < 0 or not review_project_pos < batch_pos < apply_pos:
        errors.append("Recognition modeless review must capture ProjectId before building the batch and Apply callback")

    for token in (
        "RecognitionApplyBatchService.PrepareStrict(",
        "reviewProjectId,",
        "requireAutoAcceptance: requireLiveConfidence",
        "RecognitionApplyBatchService.Commit(doc, reviewProjectId, plan)",
        "RecognitionApplyBatchService.PrepareBestEffort(doc, reviewProjectId, batch.AutoAccepted)",
        "RecognitionApplyBatchService.Commit(doc, reviewProjectId, autoPlan)",
    ):
        if token not in text:
            errors.append("Recognition modeless review missing atomic batch route token: " + token)

    for forbidden in (
        "SemanticCaptureService.CaptureSnapshot(doc, result.Snapshot",
        "var currentProject = ProjectContextCoordinator.GetOrCreate(doc);",
    ):
        if forbidden in text:
            errors.append("Recognition review must not use stale inline/replacement-creating apply state: " + forbidden)

if APPLY.is_file():
    text = APPLY.read_text(encoding="utf-8")
    required = (
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "string.Equals(project.ProjectId, expectedProjectId, StringComparison.OrdinalIgnoreCase)",
        "var version = project.ChangeVersion;",
        "EntitySnapshotReader.ReadHandles(document, new[] { result.Handle })",
        "new ProjectRecognitionService().Suggest(project, liveSnapshots[0])",
        "candidate.Category != expectedCandidate.Category",
        "!refreshed.IsCaptureReady",
        "EnsureProjectUnchanged(document, project, expectedProjectId, version",
        "if (project.ChangeVersion != plan.ProjectChangeVersion)",
        "var rollback = ProjectStateSnapshot.Capture(project);",
        "SemanticCaptureService.CaptureSnapshot(document, item.Snapshot, item.Category)",
        "rollback.Restore(project)",
        'audit.Record("recognition.skip", skip.Handle, skip.Reason);',
    )
    for token in required:
        if token not in text:
            errors.append("RecognitionApplyBatchService missing live freshness/atomicity token: " + token)

    require_current = text.find("private static ProjectState RequireCurrentProject")
    require_body = text[require_current:] if require_current >= 0 else ""
    if "ProjectContextCoordinator.GetOrCreate" in require_body:
        errors.append("Recognition batch service must not create/cache replacement project state")

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

print("[PASS] BBS/Revision Locate re-resolve current semantic state read-only; Recognition batch apply re-reads live CAD, verifies reviewed ProjectId/version/candidate readiness, and commits atomically with rollback/audit")
