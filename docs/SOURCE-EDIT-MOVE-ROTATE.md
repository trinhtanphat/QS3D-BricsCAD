# QS3DEDITSOURCE — guarded MOVE / ROTATE source editing

Parent product gap: #80. Implementation lane: #3459.

## Purpose

`QS3DEDITSOURCE` provides a first-class edit path for CAD objects that are already the authoritative source of a QS3D semantic element. It does not introduce a second geometry model. The command edits the native entity and immediately reuses the existing `QS3DSYNCSOURCE` / `SourceReconcileService` ownership, quantity and dependent-invalidation path.

The source-safe P0 slice supports **MOVE** and **ROTATE** because both are invertible native transforms. It intentionally does **not** label scale as **STRETCH**. Vertex/topology-aware STRETCH plus grip/jig behavior remains follow-up work under #80.

## User flow

1. Preselect one or more tracked authoritative source entities, or run `QS3DEDITSOURCE` and select them interactively.
2. Choose `Move` or `Rotate`.
3. Enter the normal base/target points or rotation center/angle.
4. QS3D rechecks active DWG, project revision and authoritative ownership immediately before the CAD write.
5. The native entity transform commits.
6. The exact edited source selection is passed directly to the existing source reconcile service.
7. Source-derived semantic metrics/provenance refresh and generated dependents are invalidated/removed through the existing ownership-safe reconcile path. Explicit native rebuild remains a separate reviewable action.

## Fail-closed boundaries

The command refuses mutation when selection is generated QS3D output, unknown/untracked CAD, ambiguous ownership, a P0 element has anything other than one authoritative source handle, the project changes during prompting, the active DWG changes, UCS changes while points are entered, or a selected ObjectId/Handle no longer matches at commit time.

If the CAD MOVE/ROTATE transaction itself fails, its BricsCAD transaction aborts. If the later semantic reconcile fails, `QS3DEDITSOURCE` attempts the exact inverse transform on the same source handles. If that inverse also fails, the command emits a hard repair/UNDO warning rather than reporting success.

Cancel/ESC before the forward CAD transform performs no source mutation and no reconcile.

## Rebuild boundary

As with `QS3DSYNCSOURCE`, edit/reconcile does not silently rebuild destructive downstream native output. Run the existing host/rebar/curtain/opening build command appropriate to the semantic element when the new generated geometry is wanted.

## Validation boundary

Repository-safe implementation, source guards and shared CI are expected to run remotely. Licensed BricsCAD V25 interactive proof remains **LOCAL_ONLY** and must be recorded against the exact pushed candidate SHA. The local matrix should cover at minimum MOVE, ROTATE, ESC/cancel before commit, generated-output refusal, stale-project/DWG refusal, reconcile-failure rollback, Undo, save/cold-reopen and multi-DWG isolation.

Source/static CI success must not be reported as licensed runtime PASS.
