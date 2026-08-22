# Work claim — MTR-04 quantity preview null-element integrity

- Status: `COMPLETED`
- Agent: `gpt56sol-mtr04-quantity-preview-null-element-integrity-20260813-2321`
- Registered: `2026-08-13T23:21:00+07:00`
- Baseline main SHA: `d9aff385c63effd895a933c0e6e60fcddb268427`
- Reactivated: `2026-08-13T23:37:00+07:00`
- Completed: `2026-08-14T08:00:00+07:00`
- Priority: `P0` measurement-rule trust integrity.

## Reserved scope

Make quantity-rule preview fail closed with a deterministic domain error when `ProjectState.Elements` contains a null member. Preserve the existing duplicate-element identity guard and canonical preview behavior.

## Expected surfaces

- `src/QS3D.Core/Rules/QuantityRulePreviewService.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRulePreviewGlobalElementIntegritySmoke.cs`
- this claim file

## Excluded scope

- `MeasurementTrace` / MTR-05 fact payload conflict surfaces.
- Quantity formulas, rule identity/output semantics, persistence, reports, UI, BricsCAD/native behavior.
- Other current agent claims.

## Validation evidence

- Production fix: `1017370e7ac55b1fc70d996882e1e3b9f78ffc66` — `ValidateUniqueElementIds` rejects null collection members with the deterministic domain error `Project contains a null element.` before preview projection.
- Focused regression: `192c7e794ff0e998f8e49e010fc8e1beea72fe5b` — covers null collection members through both `PreviewElement` and `PreviewProject`, including a singleton-null project, while preserving duplicate-ID and canonical positive regressions.
- Both changed files were read back from current `main` after the regression write.
- No manual GitHub Actions rerun and no BricsCAD/native PASS are claimed by this remote-safe lane.

## Coordination

The earlier quantity-preview global element-integrity work is `COMPLETED` and covers unrelated duplicate element IDs. This claim is now complete for the null-member invariant; persistence, MTR-05, UI, native runtime, and other active agent scopes remain excluded.

## Completion condition

Satisfied: null element collection corruption fails closed before preview projection and focused deterministic regression coverage is present on `main`.
