# Work claim — Bulk Edit ID-target structural freshness

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-bulk-id-target-structural-freshness-20260812-1225`
- Registered: `2026-08-12T12:25:00+07:00`
- Baseline main SHA: `5c462b3a79eb58db3c9caba6f9950058ce3d9532`
- Priority: P1 — ID-based bulk edits must not silently resolve replacement same-ID elements introduced during lazy target enumeration.
- Task Key: `CORE-BULK-ID-TARGET-STRUCTURAL-FRESHNESS`

## Confirmed defect

Completed Bulk Edit structural-freshness lanes protect object-target enumeration and verify target/Family ownership after `OwnedDistinctByIds(...)` returns. However `OwnedDistinctByIds(...)` currently materializes caller-provided IDs first and only then resolves each ID from live `project.Elements`. A lazy ID source can directly replace target `B1` with a new same-ID `ProjectElement` without calling `Touch()`. The subsequent `FindElement("B1")` resolves the replacement, and later ownership guards see that replacement as current, so ID-based `SetProperty(...)` / `AssignFamily(...)` can mutate a different instance than the structure that existed when enumeration began while `ChangeVersion` remains unchanged.

## Reserved scope

- `src/QS3D.Core/Services/BulkEditService.cs`
- `tests/QS3D.Core.SmokeTests/BulkEditIdTargetStructuralFreshnessSmoke.cs`
- this claim file

## Intended contract

- Snapshot exact project element references before materializing caller ID targets, without changing caller-input validation precedence.
- After target IDs are materialized, resolve IDs against that pre-enumeration snapshot rather than live project state.
- Reuse existing `OwnedDistinct(...)` current-ownership validation so same-ID replacement/removal during enumeration fails before mutation.
- Preserve current `ChangeVersion` freshness checks, target input bounds, duplicate/missing-ID behavior, object-target APIs, Family ownership checks, property policy, atomic rollback and successful mutation semantics.
- Do not require unrelated non-target structure to remain unchanged when it does not affect the selected bulk operation.

## Validation plan

Add focused auto-registered Core smoke coverage where a lazy target-ID source replaces selected `B1` with a new same-ID element without `Touch()`. ID-based `SetProperty(...)` must fail before mutation and preserve revision/state. Include a stable ID-based edit control. Reuse the same helper path for `AssignFamily(...)` without widening source scope.

## Validation boundary

No GitHub Actions will be dispatched. No local .NET/full executable smoke or licensed BricsCAD V25/V26 runtime PASS will be claimed unless actually executed.
