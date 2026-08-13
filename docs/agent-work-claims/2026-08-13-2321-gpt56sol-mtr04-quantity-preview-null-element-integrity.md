# Work claim — MTR-04 quantity preview null-element integrity

- Status: `ACTIVE`
- Agent: `gpt56sol-mtr04-quantity-preview-null-element-integrity-20260813-2321`
- Registered: `2026-08-13T23:21:00+07:00`
- Baseline main SHA: `df846111efbb1777babadeee4c312bdb4a58a4ba`
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

- Refresh `main` and claims after this claim-only commit.
- Add focused regression covering both element and project preview entry points against a null collection member.
- Preserve duplicate-ID and canonical positive regressions.
- Read back pushed files and inspect commit ancestry; no GitHub Actions and no native PASS claims.

## Coordination

The earlier quantity-preview global element-integrity work is `COMPLETED` and covers unrelated duplicate element IDs only. Current source skips nulls in `ValidateUniqueElementIds`, while `PreviewProject` subsequently dereferences every element. No visible current ACTIVE/BLOCKED claim reserves these two quantity-rule preview surfaces.

## Completion condition

Null element collection corruption fails closed before preview projection, focused deterministic regression coverage is pushed to current `main`, and this claim is closed `COMPLETED` with actual validation evidence and remaining LOCAL/native gates recorded.
