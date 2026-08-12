# Work claim — QuantityCalculationSettings NormalizeAndValidate atomicity

- Status: `ACTIVE`
- State: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-normalize-atomicity-20260812-1009`
- Registered: `2026-08-12T10:09:00+07:00`
- Baseline main SHA observed: `b4c85122c344429d06d4581d2fa79d8203a2e34a`
- Priority: P1 public settings mutation atomicity
- Task Key: `CORE-QUANTITY-SETTINGS-NORMALIZE-ATOMICITY`

## Confirmed defect

`QuantityCalculationSettings.NormalizeAndValidate()` currently mutates normalization targets before validation is complete: schema `0` is rewritten to `CurrentSchemaVersion`, null rule collections are replaced with empty lists, and `DimColor` is trimmed/uppercased before numeric, color and rule validation finish. If a later check fails (for example `DimTextHeight <= 0`, a negative numeric value, an invalid category rule or duplicate intersection rule), the method throws after partially modifying the caller-owned settings object.

This violates all-or-nothing behavior for a public normalize+validate boundary and can make a failed settings edit differ from the state the caller attempted to validate.

## Reserved scope

- `src/QS3D.Core/Reporting/QuantityCalculationSettings.cs` — `NormalizeAndValidate()` atomicity only
- `tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsNormalizationAtomicitySmoke.cs` — focused regression
- this claim file

## Intended contract

- Preserve existing validation order and exception semantics.
- Compute normalized schema, null-to-empty rule collections and normalized `DimColor` into locals.
- Validate all scalar values, color, category rules and intersection rules against those candidate values.
- Commit `SchemaVersion`, `CategoryRules`, `IntersectionRules` and `DimColor` only after every validation succeeds.
- On failure, those normalization targets remain exactly as they were before the call.
- Successful normalization remains behavior-compatible: schema `0` becomes current, null collections become empty lists, blank color becomes `#FFFFFF`, and valid padded/lowercase color becomes trimmed uppercase.

## Excluded scope

No changes to settings persistence/UI close workflow, cardinality limits, clone semantics, schema version policy, rule DTOs, calculation semantics, CAD/UI/runtime, Actions/build/release.

## Validation plan

Add auto-registered Core smoke coverage proving a late validation failure leaves schema, null collections and color untouched, plus a successful normalization control proving the established normalized outputs remain unchanged. Re-fetch moving `main`, compare exact overlap, squash-merge with expected head SHA, then close this claim with exact integration evidence.

No GitHub Actions/full build/release dispatch and no licensed BricsCAD V25/V26 runtime PASS claim from this lane.
