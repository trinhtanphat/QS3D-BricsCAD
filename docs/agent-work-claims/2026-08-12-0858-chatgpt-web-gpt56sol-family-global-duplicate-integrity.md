# Work claim — Family target operations global duplicate integrity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-family-global-duplicate-integrity-20260812-0858`
- Registered: `2026-08-12T08:58:00+07:00`
- Baseline main SHA: `a0baa00cef3f16df671539af50329bcbcd4ee8e9`
- Priority: P1 — Family target operations must reject globally ambiguous Family identity state.
- Task Key: `CORE-FAMILY-TARGET-OPS-GLOBAL-DUPLICATE-ID`

## Confirmed defect

`ProjectFamilyService.FindRequired(...)` delegates to `ProjectState.FindFamily(targetId)`, which rejects duplicate Family IDs only when they match the requested target. A project may therefore contain an unrelated duplicate pair such as `F1`/`f1` plus unique `F2`, and target-based operations on `F2` can proceed even though QSDB/interchange identity rules reject the project globally. Mutating paths include Rename, SetProperty, RemoveProperty, Assign and Delete; ReferenceCount can also return a normal result, while Duplicate can resolve a unique source and then delegate into Create.

## Reserved scope

- `src/QS3D.Core/Domain/ProjectFamilyService.cs`
- `tests/QS3D.Core.SmokeTests/ProjectFamilyGlobalDuplicateIntegritySmoke.cs`
- this claim file

## Intended contract

- All existing non-null Family IDs are checked case-insensitively for uniqueness before target resolution.
- Create reuses the same uniqueness helper introduced by the completed Create-specific lane.
- Rename, SetProperty, RemoveProperty, Assign, Delete, ReferenceCount and Duplicate source resolution fail closed on unrelated duplicate Family IDs before mutation/result production.
- Existing null-entry behavior remains delegated to the current Create guard / `ProjectState.FindFamily`; valid no-op, inheritance, category and assignment semantics remain unchanged.
- No Floor/Zone services, Family UI, persistence/interchange or native BricsCAD changes.

## Validation plan

Focused auto-registered Core smoke seeds `F1`/`f1` plus unique `F2` and verifies representative target operations reject without Family/element/revision/timestamp mutation; read-only ReferenceCount rejects too. Valid controls preserve Rename/SetProperty/Assign/ReferenceCount semantics. Re-fetch current source/claim before writes. No force-push, Actions dispatch, .NET smoke PASS or licensed BricsCAD runtime qualification claim unless actually executed.
