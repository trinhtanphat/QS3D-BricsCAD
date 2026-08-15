#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

capture = ROOT / "src/QS3D.BricsCAD.V25/Services/SemanticCaptureService.cs"
policy = ROOT / "src/QS3D.Core/Diagnostics/GeneratedHandleOwnershipPolicy.cs"
review = ROOT / "src/QS3D.BricsCAD.V25/ReviewCommands.cs"
apply_service = ROOT / "src/QS3D.BricsCAD.V25/Services/RecognitionApplyBatchService.cs"
snapshot = ROOT / "src/QS3D.Core/Persistence/ProjectStateSnapshot.cs"

for path in (capture, policy, review, apply_service, snapshot):
    if not path.is_file():
        errors.append("missing semantic-capture safety file: " + str(path.relative_to(ROOT)))

if capture.is_file():
    text = capture.read_text(encoding="utf-8")
    for needle in (
        "ProjectStateSnapshot.Capture(project)",
        "CaptureSnapshotCore(document, project, snapshot, category)",
        "GeneratedHandleOwnershipPolicy.TryFindOwner(project, snapshot.Handle",
        "output do QS3D sinh",
        "RestoreOrThrow(project, rollback, operationError",
        "rollback.Restore(project)",
        "AggregateException(operationError, restoreError)",
        'RestoreOrThrow(project, rollback, operationError, "Room finish generation")',
        'RestoreOrThrow(project, rollback, operationError, "Room finish synchronization")',
    ):
        if needle not in text:
            errors.append("SemanticCaptureService missing transactional/generated-source guard: " + needle)

    guard = text.find("GeneratedHandleOwnershipPolicy.TryFindOwner(project, snapshot.Handle")
    add = text.find("project.Elements.Add(element)")
    if guard < 0 or add < 0 or guard > add:
        errors.append("generated-output rejection must happen before semantic element mutation")

    capture_method = text.find("public static int Capture(Document document, ElementCategory category)")
    rollback = text.find("var rollback = ProjectStateSnapshot.Capture(project);", capture_method)
    loop = text.find("foreach (var snapshot in snapshots)", capture_method)
    if capture_method < 0 or rollback < 0 or loop < 0 or rollback > loop:
        errors.append("multi-selection semantic Capture must snapshot project before the capture loop")

    finish_method = text.find("public static int GenerateRoomFinishes(Document document)")
    finish_rollback = text.find("var rollback = ProjectStateSnapshot.Capture(project);", finish_method)
    finish_loop = text.find("foreach (var room in rooms)", finish_method)
    if finish_method < 0 or finish_rollback < 0 or finish_loop < 0 or finish_rollback > finish_loop:
        errors.append("GenerateRoomFinishes must snapshot project before mutating the finish batch")

    sync_method = text.find("public static int SyncExistingRoomFinishes(ProjectState project, ProjectElement room)")
    sync_rollback = text.find("var rollback = ProjectStateSnapshot.Capture(project);", sync_method)
    sync_mutation = text.find("RoomFinishSynchronizationService.SynchronizeExisting(project, room)", sync_method)
    if sync_method < 0 or sync_rollback < 0 or sync_mutation < 0 or sync_rollback > sync_mutation:
        errors.append("SyncExistingRoomFinishes must snapshot project before mutating existing finishes")

if policy.is_file():
    text = policy.read_text(encoding="utf-8")
    for needle in ("public static class GeneratedHandleOwnershipPolicy", "EnumerateOwnerHandles", "CollectOwnerHandles", "TryFindOwner"):
        if needle not in text:
            errors.append("Core generated ownership policy missing capture-safety API: " + needle)

if review.is_file():
    text = review.read_text(encoding="utf-8")
    for needle in (
        "RecognitionApplyBatchService.PrepareStrict(",
        "RecognitionApplyBatchService.PrepareBestEffort(doc, reviewProjectId, batch.AutoAccepted)",
        "RecognitionApplyBatchService.Commit(doc, reviewProjectId, plan)",
        "RecognitionApplyBatchService.Commit(doc, reviewProjectId, autoPlan)",
    ):
        if needle not in text:
            errors.append("Recognition/B4D review must route mutation through the atomic batch service: " + needle)

if apply_service.is_file():
    text = apply_service.read_text(encoding="utf-8")
    for needle in (
        "EntitySnapshotReader.ReadHandles(document, new[] { result.Handle })",
        "new ProjectRecognitionService().Suggest(project, liveSnapshots[0])",
        "candidate.Category != expectedCandidate.Category",
        "!refreshed.IsCaptureReady",
        "var rollback = ProjectStateSnapshot.Capture(project);",
        "SemanticCaptureService.CaptureSnapshot(document, item.Snapshot, item.Category)",
        "SemanticHandleOwnershipResolver.ResolveUniqueSourceOwner(project, item.Handle)",
        "rollback.Restore(project)",
    ):
        if needle not in text:
            errors.append("Recognition batch capture missing live-state/transaction token: " + needle)

    reread = text.find("EntitySnapshotReader.ReadHandles(document, new[] { result.Handle })")
    suggest = text.find("new ProjectRecognitionService().Suggest(project, liveSnapshots[0])", reread)
    capture_call = text.find("SemanticCaptureService.CaptureSnapshot(document, item.Snapshot, item.Category)")
    if min(reread, suggest, capture_call) < 0 or not reread < suggest < capture_call:
        errors.append("Recognition batch must live re-read/re-suggest before guarded semantic capture")

if snapshot.is_file():
    text = snapshot.read_text(encoding="utf-8")
    for needle in (
        "public static ProjectStateSnapshot Capture",
        "public void Restore",
        "target.Families.Clear()",
        "target.Elements.Clear()",
        "target.AuditEvents.Clear()",
        "targetMetadata.ReplacePersistenceState(source.Metadata)",
        "target.RestorePersistenceState(source.UpdatedUtc, source.ChangeVersion)",
    ):
        if needle not in text:
            errors.append("ProjectStateSnapshot is not deep enough for semantic capture rollback: " + needle)

print("QS3D semantic capture safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: semantic capture rejects generated output before mutation; Recognition/B4D live-revalidate through the atomic batch service; capture and room-finish batches restore full project state on failure.")
