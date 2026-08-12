# Work claim — release #30 ED2 unit read-only preflight reconciliation

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-release30-ed2-unit-readonly-preflight`
- Registered: `2026-08-12T09:56:00+07:00`
- Baseline main SHA: `dae1e356c251edb699f28f3434468385d8a55c81`
- Priority: QS3D Cloud V25 Preview Build & Release #30 reports two ED2 unit/read-only failures after ED2 adopted pre-save detached validation and DrawingUnitWorkflow generalized read-only quantity preparation across ED2 and BQ.

## Reserved scope

Reconcile only `scripts/preflight-ed2-unit-export-readonly.py` with the current ED2 command ordering and shared `readOnlyQuantityPreparation` unit contract. Preserve Commands/DrawingUnitWorkflow production source unchanged.

## Canonical evidence

- `QS3DED2` now requires an existing project and non-empty semantic state before `DrawingUnitWorkflow.EnsureResolved`, then builds/validates detached report state before SaveFileDialog and writes only after confirmation.
- Existing project lookup is read-only and does not create/cache replacement state.
- `DrawingUnitWorkflow` defines `readOnlyExportPreparation` (ED2), `readOnlyBqPreparation` (BQ), and `readOnlyQuantityPreparation = readOnlyExportPreparation || readOnlyBqPreparation`.
- Resolved-unit legacy binding persists only when `!readOnlyQuantityPreparation`, so ED2/BQ resolved checks stay non-mutating.
- Unresolved read-only quantity preparation returns false before `PromptAndPersist`; explicit QS3DUNITS remains persistence owner.

## Expected surfaces

- `scripts/preflight-ed2-unit-export-readonly.py`
- this claim file for close-out

## Excluded scope

- No edits to Commands.cs, DrawingUnitWorkflow.cs, CadUnitService, unit policy/project metadata or export behavior.
- No changes to explicit QS3DUNITS persistence.
- No unrelated run #30 failures, GitHub Actions dispatch, build/release publication or BricsCAD runtime qualification.

## Validation plan

- Require ED2 ordering existing read-only project -> unit EnsureResolved -> detached snapshot/report/live-handle validation -> Save confirmation -> export.
- Explicitly forbid GetOrCreate/live mutation in ED2 command section.
- Replace stale exact resolved guard with current `readOnlyQuantityPreparation` marker and `if (!readOnlyQuantityPreparation)` persistence guard.
- Retain unresolved read-only guard and require it to return false before PromptAndPersist without GetOrCreate/Save/Touch.
- Re-fetch exact gate before write, read back after commit, verify ancestry and close with exact SHA.

## Coordination

Repository search found no active reservation for this ED2 unit preflight or DrawingUnitWorkflow read-only quantity path. Current Grid/Project Name/Recognition claims are unrelated.

## Completion condition

The ED2 unit gate matches current existing-project-first/pre-save-validation and shared ED2+BQ read-only unit semantics without weakening non-mutation guarantees, is pushed to `main`, and this claim is closed with exact evidence.
