#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PLAN = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeGeneratedRebuildPlan.cs"
EXECUTOR = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeGeneratedRebuildExecutor.cs"
ADAPTER = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "InterchangeFieldMergeImportService.cs"
RUNBOOK = ROOT / "docs" / "FIELDMERGE-GENERATED-REBUILD.md"

errors = []
for path in (PLAN, EXECUTOR, ADAPTER, RUNBOOK):
    if not path.is_file():
        errors.append("missing FieldMerge generated-rebuild contract file: " + str(path.relative_to(ROOT)))

if not errors:
    plan = PLAN.read_text(encoding="utf-8")
    executor = EXECUTOR.read_text(encoding="utf-8")
    adapter = ADAPTER.read_text(encoding="utf-8")
    runbook = RUNBOOK.read_text(encoding="utf-8")

    for token in (
        "InterchangeGeneratedOutputKind.NativeGeometry",
        "InterchangeGeneratedOutputKind.Quantity",
        "Workbook = 1 << 2",
        "Trace = 1 << 3",
        "requestedKinds & ~SupportedKinds",
        "Only atomic NativeGeometry and Quantity rebuilds are supported",
        ".Distinct(StringComparer.OrdinalIgnoreCase)",
        ".OrderBy(id => id, StringComparer.OrdinalIgnoreCase)",
    ):
        if token not in plan:
            errors.append("FieldMerge rebuild plan missing bounded/fail-closed token: " + token)

    for token in (
        'GeneratedSolidHandleKey = "GeneratedSolidHandle"',
        "GeneratedHandleOwnershipPolicy.IsOwnerSlot",
        "StructuralSolidBuilder.Supports(element.Category)",
        "SlabOpeningPeerReplayService.CaptureAppliedOpeningIds",
        "CadHandleService.Resolve(document, element.SourceHandles)",
        "claimedSources.Add(sourceHandle)",
        "StructuralSolidBuilder.BuildSelected",
        "RegenerationEngine",
        "RegenerateDirtySubset(project, manifest.Plan.ElementIds)",
        "FieldMerge rebuild plan escaped the reviewed affected closure",
        "FieldMerge atomic rebuild does not support generated owner slot",
        "FieldMerge atomic native rebuild requires exactly one live CAD source",
    ):
        if token not in executor:
            errors.append("FieldMerge rebuild executor missing ownership/bounded-runtime token: " + token)

    for token in (
        "AutomaticRebuildKinds",
        "InterchangeGeneratedOutputKind.NativeGeometry",
        "InterchangeGeneratedOutputKind.Quantity",
        "InterchangeFieldMergeGeneratedRebuildPlan.Create",
        "InterchangeFieldMergeGeneratedRebuildExecutor.Prepare",
        "SourceReconcileUndoCoordinator.BeginTransition",
        "undoTransition.StageNativeMarker()",
        "SourceReconcileUndoCoordinator.BeginExternalTransitionScope",
        "GeneratedDependentGeometryInvalidator.Prepare",
        "ProjectInterchangeFieldMergeImporter.Import",
        "invalidation.CommitMetadata()",
        "InterchangeFieldMergeGeneratedRebuildExecutor.Execute",
        "undoTransition.StageAfter",
        "transaction.Commit()",
        "undoTransition.ConfirmCommitted()",
        "rollback.Restore(project)",
    ):
        if token not in adapter:
            errors.append("FieldMerge adapter missing rebuild/Undo/rollback token: " + token)

    rebuild_prepare_at = adapter.find("InterchangeFieldMergeGeneratedRebuildExecutor.Prepare")
    undo_begin_at = adapter.find("SourceReconcileUndoCoordinator.BeginTransition")
    invalidate_at = adapter.find("GeneratedDependentGeometryInvalidator.Prepare")
    core_at = adapter.find("ProjectInterchangeFieldMergeImporter.Import")
    cleanup_at = adapter.find("invalidation.CommitMetadata()")
    rebuild_execute_at = adapter.find("InterchangeFieldMergeGeneratedRebuildExecutor.Execute")
    stage_after_at = adapter.find("undoTransition.StageAfter")
    cad_commit_at = adapter.find("transaction.Commit()")
    undo_commit_at = adapter.find("undoTransition.ConfirmCommitted()")
    ordered = (
        rebuild_prepare_at,
        undo_begin_at,
        invalidate_at,
        core_at,
        cleanup_at,
        rebuild_execute_at,
        stage_after_at,
        cad_commit_at,
        undo_commit_at,
    )
    if min(ordered) < 0 or list(ordered) != sorted(ordered):
        errors.append(
            "FieldMerge must preserve preflight rebuild -> outer Undo -> invalidate -> exact Core apply -> old-owner cleanup -> rebuild -> stage-after -> CAD commit -> Undo confirm ordering"
        )

    automatic_block_start = adapter.find("private const InterchangeGeneratedOutputKind AutomaticRebuildKinds")
    automatic_block_end = adapter.find(";", automatic_block_start)
    automatic_block = adapter[automatic_block_start:automatic_block_end + 1] if automatic_block_start >= 0 and automatic_block_end >= 0 else ""
    for forbidden in ("Workbook", "Trace"):
        if forbidden in automatic_block:
            errors.append("FieldMerge automatic rebuild widened into unsupported output: " + forbidden)

    for token in (
        "SOURCE_READY / PENDING_LOCAL",
        "NativeGeometry",
        "Quantity",
        "Workbook",
        "Trace",
        "must **not** patch production source",
        "licensed V25 FieldMerge matrix",
    ):
        if token not in runbook:
            errors.append("FieldMerge local handoff runbook missing token: " + token)

if errors:
    print("QS3D FieldMerge generated-rebuild preflight FAILED")
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: FieldMerge automatic rebuild stays bounded to reviewed NativeGeometry + Quantity, fail-closes unsupported ownership/outputs, and remains inside one rollback-capable native/semantic Undo boundary before CAD commit; licensed runtime remains PENDING_LOCAL.")
