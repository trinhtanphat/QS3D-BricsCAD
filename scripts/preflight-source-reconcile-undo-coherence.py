#!/usr/bin/env python3
from pathlib import Path

HERE = Path(__file__).resolve()
BASE = HERE.parent / "_guard_bases" / "source-reconcile-undo-coherence.py"
source = BASE.read_text(encoding="utf-8")

old = '''for token in (
    "SourceReconcileUndoCoordinator.Attach(docs.MdiActiveDocument)",
    "SourceReconcileUndoCoordinator.Attach(e.Document)",
    "SourceReconcileUndoCoordinator.Detach(document)",
    "SourceReconcileUndoCoordinator.Stop()",
):
    if token not in lifecycle:
        errors.append("Document lifecycle missing Undo coordination: " + token)
'''
new = '''for token in (
    "AttachCriticalServices(docs.MdiActiveDocument)",
    "AttachCriticalServices(e.Document)",
    "SourceReconcileUndoCoordinator.Attach(document)",
    "SourceReconcileUndoCoordinator.Detach(document)",
    "SourceReconcileUndoCoordinator.Stop()",
    "ScheduleReconcile(e.Document, false)",
    "ScheduleReconcile(e.Document, true)",
):
    if token not in lifecycle:
        errors.append("Document lifecycle missing staged Undo coordination: " + token)
if lifecycle.count("AttachCriticalServices(e.Document)") < 2:
    errors.append("Source Reconcile Undo critical attachment must cover both DocumentCreated and DocumentActivated")
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard lifecycle block drifted")
source = source.replace(old, new, 1)

old = '''prepare = service.find("GeneratedDependentGeometryInvalidator.Prepare")
units = service.find("CadUnitService.TryGetPolicy", prepare)
refresh = service.find("RefreshSourceDerivedState(project", prepare)
metadata = service.find("invalidation.CommitMetadata()", refresh)
after = service.find("var afterSnapshot = ProjectStateSnapshot.Capture(project);", metadata)
begin = service.find("SourceReconcileUndoCoordinator.BeginTransition(", after)
stage = service.find("undoTransition.StageAfter(project, afterSnapshot);", begin)
commit = service.find("transaction.Commit();", stage)
confirm = service.find("undoTransition.ConfirmCommitted();", commit)
committed = service.find("cadCommitted = true;", confirm)
restore = service.find("rollback.Restore(project);", committed)
positions = (prepare, units, refresh, metadata, after, begin, stage, commit, confirm, committed, restore)
if any(position < 0 for position in positions) or list(positions) != sorted(positions):
    errors.append(
        "Source Reconcile must finish invalidation/unit/refresh work -> capture Redo -> begin/stage marker -> native commit -> publish history, while preserving pre-commit semantic rollback"
    )

snapshot = service.find("var rollback = ProjectStateSnapshot.Capture(project);")
stamp = service.find(
    "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);",
    snapshot,
)
transaction = service.find("document.Database.TransactionManager.StartTransaction()", stamp)
if min(snapshot, stamp, transaction, begin) < 0 or not snapshot < stamp < transaction < begin:
    errors.append(
        "Source Reconcile must pair its rollback snapshot with a pre-mutation revision stamp before the native transaction and delay the Undo transition until all reconcile work is ready"
    )
begin_call = service[begin:stage]
'''
new = '''snapshot = service.find("var rollback = ProjectStateSnapshot.Capture(project);")
stamp = service.find(
    "var rollbackStamp = SourceReconcileUndoCoordinator.ProjectRevisionStamp.Capture(project);",
    snapshot,
)
transaction = service.find("document.Database.TransactionManager.StartTransaction()", stamp)
begin = service.find("SourceReconcileUndoCoordinator.BeginTransition(", transaction)
marker = service.find("undoTransition.StageNativeMarker();", begin)
prepare = service.find("GeneratedDependentGeometryInvalidator.Prepare", marker)
units = service.find("CadUnitService.TryGetPolicy", prepare)
refresh = service.find("RefreshSourceDerivedState(project", prepare)
metadata = service.find("invalidation.CommitMetadata()", refresh)
after = service.find("var afterSnapshot = ProjectStateSnapshot.Capture(project);", metadata)
stage = service.find("undoTransition.StageAfter(project, afterSnapshot);", after)
commit = service.find("transaction.Commit();", stage)
confirm = service.find("undoTransition.ConfirmCommitted();", commit)
committed = service.find("cadCommitted = true;", confirm)
restore = service.find("rollback.Restore(project);", committed)
positions = (
    snapshot, stamp, transaction, begin, marker, prepare, units, refresh,
    metadata, after, stage, commit, confirm, committed, restore,
)
if any(position < 0 for position in positions) or list(positions) != sorted(positions):
    errors.append(
        "Source Reconcile must capture rollback/stamp -> begin transition -> stage native marker -> mutate topology/semantics -> stage private history -> commit -> publish, while preserving rollback"
    )
if service.count("undoTransition.StageNativeMarker();") != 1:
    errors.append("Source Reconcile must stage exactly one native revision marker before topology mutation")
begin_call = service[begin:marker]
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard service-order block drifted")
source = source.replace(old, new, 1)

old = '''if "rollback,\\n                        rollbackStamp))" not in begin_call:
    errors.append("Source Reconcile must pass the captured pre-mutation stamp with its rollback snapshot")
'''
new = '''if "rollback,\\n                    rollbackStamp))" not in begin_call:
    errors.append("Source Reconcile must pass the captured pre-mutation stamp with its rollback snapshot")
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard rollback-stamp call block drifted")
source = source.replace(old, new, 1)

old = '''stage_start = coordinator.find("public void StageAfter(")
confirm_start = coordinator.find("public void ConfirmCommitted()", stage_start)
dispose_start = coordinator.find("public void Dispose()", confirm_start)
'''
new = '''marker_start = coordinator.find("public void StageNativeMarker(")
stage_start = coordinator.find("public void StageAfter(", marker_start)
confirm_start = coordinator.find("public void ConfirmCommitted()", stage_start)
dispose_start = coordinator.find("public void Dispose()", confirm_start)
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard transition-boundary block drifted")
source = source.replace(old, new, 1)

old = '''stage_body = coordinator[stage_start:confirm_start]
confirm_body = coordinator[confirm_start:dispose_start]
'''
new = '''marker_body = coordinator[marker_start:stage_start]
stage_body = coordinator[stage_start:confirm_start]
confirm_body = coordinator[confirm_start:dispose_start]
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard transition-body block drifted")
source = source.replace(old, new, 1)

old = '''    stage_start, confirm_start, dispose_start, begin_start, will_start,
'''
new = '''    marker_start, stage_start, confirm_start, dispose_start, begin_start, will_start,
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard transition-minimum block drifted")
source = source.replace(old, new, 1)

old = '''    if "markerCarrier.XData = marker" not in stage_body or "_stagedEntries = stagedEntries;" not in stage_body:
        errors.append("native marker must be written only after the private shadow history is fully staged")
    enable_undo = stage_body.find("_markerCarrier.DisableUndoRecording(false);")
    upgrade_write = stage_body.find("_markerCarrier.UpgradeOpen();", enable_undo)
    marker_write = stage_body.find("_markerCarrier.XData = marker;", upgrade_write)
    enable_end = enable_undo + len("_markerCarrier.DisableUndoRecording(false);")
    upgrade_end = upgrade_write + len("_markerCarrier.UpgradeOpen();")
    if (
        min(enable_undo, upgrade_write, marker_write) < 0
        or stage_body[enable_end:upgrade_write].strip()
        or stage_body[upgrade_end:marker_write].strip()
    ):
        errors.append("the read-only ModelSpace BlockBegin carrier must enable native Undo recording, upgrade open, then assign revision XData")
    if (
        coordinator.count(".DisableUndoRecording(") != 1
        or coordinator.count("_markerCarrier.UpgradeOpen(") != 1
        or "DisableUndoRecording" in begin_body
        or "_markerCarrier.UpgradeOpen" in begin_body
    ):
        errors.append("explicit Undo recording and write upgrade must remain isolated to the staged BlockBegin revision marker write")
'''
new = '''    if "_markerCarrier.XData = marker" not in marker_body or "_stagedEntries = stagedEntries;" not in stage_body:
        errors.append("native marker and private semantic history must remain in their separate staging phases")
    if "_stagedEntries = stagedEntries;" in marker_body or "_markerCarrier.XData = marker" in stage_body:
        errors.append("native marker must be staged before topology while semantic history remains post-mutation and private")
    if "if (!_markerStaged)" not in stage_body:
        errors.append("StageAfter must refuse semantic staging until the native marker has been staged")
    enable_undo = marker_body.find("_markerCarrier.DisableUndoRecording(false);")
    upgrade_write = marker_body.find("_markerCarrier.UpgradeOpen();", enable_undo)
    marker_write = marker_body.find("_markerCarrier.XData = marker;", upgrade_write)
    enable_end = enable_undo + len("_markerCarrier.DisableUndoRecording(false);")
    upgrade_end = upgrade_write + len("_markerCarrier.UpgradeOpen();")
    if (
        min(enable_undo, upgrade_write, marker_write) < 0
        or marker_body[enable_end:upgrade_write].strip()
        or marker_body[upgrade_end:marker_write].strip()
    ):
        errors.append("the read-only ModelSpace BlockBegin carrier must enable native Undo recording, upgrade open, then assign revision XData")
    if (
        coordinator.count(".DisableUndoRecording(") != 1
        or coordinator.count("_markerCarrier.UpgradeOpen(") != 1
        or "DisableUndoRecording" in begin_body
        or "_markerCarrier.UpgradeOpen" in begin_body
        or "DisableUndoRecording" in stage_body
        or "_markerCarrier.UpgradeOpen" in stage_body
    ):
        errors.append("explicit Undo recording and write upgrade must remain isolated to StageNativeMarker")
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard marker-stage block drifted")
source = source.replace(old, new, 1)

old = '''    if "markerCarrier.XData = marker" in begin_body or "EnsureRegApp" in begin_body:
        errors.append("BeginTransition must not mutate the native marker before fallible reconcile work completes")
'''
new = '''    if "markerCarrier.XData = marker" in begin_body or "EnsureRegApp" in begin_body:
        errors.append("BeginTransition must remain a read-only capture; StageNativeMarker owns the pre-topology write")
    if "EnsureRegApp(_database, _transaction);" not in marker_body:
        errors.append("StageNativeMarker must ensure the RegApp before assigning the pre-topology revision marker")
'''
if old not in source:
    raise SystemExit("Source Reconcile base guard BeginTransition marker block drifted")
source = source.replace(old, new, 1)
namespace = {"__file__": str(HERE), "__name__": "__main__"}
exec(compile(source, str(HERE), "exec"), namespace)
