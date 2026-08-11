# Work claim — Grid annotation audit-owned Touch

- Status: `ACTIVE`
- Agent: `ChatGPT Web / GPT-5.6 Sol`
- Registered: `2026-08-11T23:23:00+07:00`
- Baseline main SHA: `8d43cb9016699b39118a08fd9a1238ec21516eb7`
- Priority: evidence-driven remote-safe native lifecycle correctness

## Reason

`GridAnnotationBuilder.Build()` calls `ReplaceOne(...)` once per Grid and every successful `ReplaceOne(...)` records `grid.annotation.replace` through `AuditTrail.ForProject(project).Record(...)`. `AuditTrail.Record(...)` owns the semantic `ProjectState.Touch()` for that mutation, but `Build()` currently performs an additional unconditional `project.Touch()` after the loop. A successful batch therefore advances `ChangeVersion` once more than its audit-owned mutations require, unlike the already-correct `RebuildInTransaction(...)` path.

## Reserved scope

- Remove only the redundant batch-level `project.Touch()` from `GridAnnotationBuilder.Build()` while preserving per-Grid audit records, canonical-target validation, native transaction ordering, ownership checks, rollback, and editor regeneration behavior.
- Extend the existing auto-discovered Grid annotation canonical-target preflight to lock the audit-owned revision contract and reject reintroduction of an explicit batch-level `project.Touch()`.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/Cad/GridAnnotationBuilder.cs`
- `scripts/preflight-grid-annotation-canonical-targets.py`
- this claim file

## Excluded scope

- No changes to Grid LINE/ARC geometry, annotation placement/plane math, bubble/text sizing, native ownership/XData, Grid numbering, intersection/system planning, Direct Draw, or UI.
- No changes to `AuditTrail` semantics.
- No GitHub Actions dispatch or release workflow.
- No claim of licensed BricsCAD V25 runtime qualification.

## Validation plan

- Re-fetch current `main` and the exact target blobs before implementation; never force-push.
- Source/static proof keeps `AuditTrail.ForProject(project).Record("grid.annotation.replace", ...)` in `ReplaceOne(...)` and removes the extra `project.Touch()` only from `Build()`.
- Extend `preflight-grid-annotation-canonical-targets.py` so `Build()` must retain canonical validation -> rollback snapshot -> native transaction -> `ReplaceOne(...)` -> transaction commit ordering while containing no explicit `project.Touch()`.
- Preserve rollback through `ProjectStateSnapshot` and the existing transactional rebuild path.
- Record source/static verification only; exact V25 behavior remains LOCAL_ONLY.

## Coordination

Recent Grid annotation work hardens canonical target identity, ownership, liveness and stale-handle behavior. No current claim or recent commit was found for Grid annotation audit-owned revision semantics. Concurrent active work in quantity-rule UI and unrelated persistence/docs domains is outside this reserved scope.

## Completion condition

`GridAnnotationBuilder.Build()` no longer double-advances project revision beyond its audit-owned per-Grid mutations, the lifecycle preflight guards that invariant, the patch is merged to `main`, and this claim is marked `COMPLETED`.