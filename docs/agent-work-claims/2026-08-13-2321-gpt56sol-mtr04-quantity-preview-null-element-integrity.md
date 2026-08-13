# Work claim — MTR-04 quantity preview null-element integrity

- Status: `ACTIVE`
- Agent: `gpt56sol-mtr04-quantity-preview-null-element-integrity-20260813-2321`
- Registered: `2026-08-13T23:21:00+07:00`
- Baseline main SHA: `d9aff385c63effd895a933c0e6e60fcddb268427`
- Reactivated: `2026-08-13T23:37:00+07:00`
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

## Validation plan

- Refresh `main` and claims immediately before production writes.
- Add focused regression covering both element and project preview entry points against a null collection member.
- Preserve duplicate-ID and canonical positive regressions.
- Read back pushed files and inspect commit ancestry; no manual GitHub Actions rerun and no native PASS claims.

## Coordination

The earlier quantity-preview global element-integrity work is `COMPLETED` and covers unrelated duplicate element IDs only. This exact claim was released only because the prior session's source-write route was blocked. It has now been reactivated on refreshed `main`; the production defect remains present: `ValidateUniqueElementIds` skips nulls while preview paths later require every collection member to be a valid element.

## Completion condition

Null element collection corruption fails closed before preview projection, focused deterministic regression coverage is pushed to current `main`, and this claim is closed `COMPLETED` with actual readback evidence and remaining LOCAL/native gates recorded.
