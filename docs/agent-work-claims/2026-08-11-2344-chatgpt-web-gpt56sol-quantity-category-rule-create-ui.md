# Work claim — Quantity Settings create missing category rule UI

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-category-rule-create-ui-20260811-2344`
- Registered: `2026-08-11T23:44:00+07:00`
- Baseline main SHA: `e9bb3ca787dc3554a75cf8a55dbd190810823ab3`
- Priority: P1 — complete the owner-requested rule authoring workflow for imported/partial Quantity Settings where an integer category exists only in intersection rules and therefore has no editable Category Rule row.

## Confirmed gap

`QuantityCalculationMatrixDiagnostics` explicitly reports `IntersectionOnlyCategoryCodes`, and `QuantityCalculationRuleSet.TryGetCategoryRule(...)` returns false when no category row exists. `QS3DSETUP` currently displays only existing `CategoryRows`; unlike the newly completed missing A → B action, it offers no way to add a Category Rule for an intersection-only code without editing JSON outside QS3D.

## Reserved scope

- expose intersection-only category codes in the existing `Thông số Cốp pha` tab;
- add one contextual `Tạo quy tắc loại` action for the selected missing code;
- confirmed creation adds exactly one in-memory `QuantityCategoryRuleRow` with conservative defaults: `ExtractSide=false`, `ExtractBottom=false`, `FaceAngleThresholdDeg=30` (the existing QS3D category-rule default threshold); no quantity extraction becomes enabled by creation itself;
- do not persist until the existing `Lưu Cài Đặt` flow;
- after creation, refresh category choices and the intersection browser without synthesizing intersection rules.

## Expected surfaces

- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml`
- `src/QS3D.BricsCAD.V25/UI/QuantitySettingsWindow.xaml.cs`
- `scripts/preflight-quantity-category-rule-create-ui.py` (new)
- this claim file for close-out

## Explicit exclusions

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs` and the currently active clone-cardinality claim;
- `QuantitySettingsStore.cs`, rule engine/runtime arithmetic, matrix diagnostics implementation;
- command-line `QS3DRULECREATE`, intersection-rule creation semantics, project/CAD mutation;
- Build3D, geometry, updater/release, documentation/native artifact claims;
- GitHub Actions and licensed V25 runtime qualification.

## Coordination

The active Quantity Settings clone-cardinality lane is Core-only and explicitly excludes WPF. Project-session recovery, Build3D single-touch, quantity-rule-engine ownership and several geometry/release lanes are also active and untouched. This claim owns only the missing Category Rule authoring affordance inside the already existing Quantity Settings WPF window.

## Validation gates

- missing choices are derived only from `IntersectionRows` source/target codes absent from `CategoryRows`;
- existing category codes cannot be duplicated;
- future-schema read-only state disables the create action;
- handler rechecks selection/missing status, requires explicit Yes/No, appends exactly one category row with both extraction flags false and threshold 30, then refreshes choices/browser;
- handler contains no `_store.Save/Import/Export`, project lifecycle, CAD transaction/selection or direct file/JSON writes;
- static preflight checks XAML well-formedness and these source contracts;
- no GitHub Actions dispatch.

## Completion condition

A user can repair an intersection-only category directly in `QS3DSETUP` without external JSON editing; creation is explicit, non-destructive and unsaved until the existing Save action, duplicate/cancel/future-schema paths remain fail-closed, focused static coverage is on `main`, and this claim is marked `COMPLETED` with exact SHA evidence.