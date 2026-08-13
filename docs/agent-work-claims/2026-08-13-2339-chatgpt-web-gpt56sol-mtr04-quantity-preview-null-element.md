# Work claim — MTR-04 quantity preview null-element integrity (reclaim)

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-mtr04-null-element-20260813-2339`
- Registered: `2026-08-13T23:39:00+07:00`
- Baseline main SHA: `0be74ac84f44fb4158ab74b2c1cc3de93803cca9`
- Priority: `P0` measurement-rule trust integrity.

## Confirmed defect

The earlier MTR-04 null-element claim is explicitly `RELEASED` for a future claimant. Current `QuantityRulePreviewService.ValidateUniqueElementIds()` still skips null collection members, while preview/snapshot paths subsequently enumerate and dereference project elements. Corrupt `ProjectState.Elements` therefore escapes the intended global integrity gate instead of failing closed with a deterministic domain error.

## Reserved scope

- `src/QS3D.Core/Rules/QuantityRulePreviewService.cs`
- `tests/QS3D.Core.SmokeTests/QuantityRulePreviewGlobalElementIntegritySmoke.cs`
- this claim file

Change only global element-integrity validation so a null member fails closed before preview projection. Preserve duplicate-ID behavior and valid preview semantics.

## Excluded scope

Quantity formulas, rule matching/output/provenance semantics, persistence, reports/UI, BricsCAD/native behavior, MeasurementTrace/MTR-05, MAP-01B and other current claims.

## Validation plan

Refresh `main` after claim publication and recheck overlap; add focused regressions for both `PreviewElement` and `PreviewProject`; preserve duplicate-ID and canonical positive regressions; reconcile moving `main`; publish without force; no Actions or unexecuted runtime PASS claims.

## Completion condition

Both quantity-rule preview entry points fail closed on null element collection corruption, focused smoke coverage is on current `main`, and this claim is closed `COMPLETED` with truthful validation evidence.