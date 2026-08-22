# Work claim — QuantityCalculationSettings NormalizeAndValidate atomicity

- Status: `COMPLETED`
- State: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-quantity-settings-normalize-atomicity-20260812-1009`
- Registered: `2026-08-12T10:09:00+07:00`
- Completed: `2026-08-12T10:13:00+07:00`
- Baseline main SHA observed: `b4c85122c344429d06d4581d2fa79d8203a2e34a`
- Claim commit: `c94a12322b8b9d7751c3d0d8972a0868375726d4`
- Pull Request: `#739`
- Reviewed head: `8948948f9d154abcebe43020ee91626d9393f5a8`
- Merge SHA: `8bb3539395046e081330d8570029184645499708`
- Priority: P1 public settings mutation atomicity
- Task Key: `CORE-QUANTITY-SETTINGS-NORMALIZE-ATOMICITY`

## Confirmed defect

`QuantityCalculationSettings.NormalizeAndValidate()` previously rewrote schema `0`, null rule collections and `DimColor` before numeric/color/rule validation completed, so a later exception left the caller-owned object partially normalized despite validation failure.

## Completed implementation

- Compute normalized schema, null-to-empty rule collections and normalized `DimColor` into local candidates.
- Preserve the established validation order: schema, collection cardinality, scalar values, color, category rules, intersection rules.
- Validate against candidate values without mutating normalization targets.
- Commit `SchemaVersion`, `CategoryRules`, `IntersectionRules` and `DimColor` only after every validation succeeds.
- Successful normalization behavior remains unchanged.

## Regression evidence

`tests/QS3D.Core.SmokeTests/QuantityCalculationSettingsNormalizationAtomicitySmoke.cs` proves a late `DimTextHeight` failure leaves schema, null collections and raw color untouched, and proves a successful call still upgrades schema 0, creates empty rule lists and trims/uppercases color.

Moving-main comparison showed no overlap with `QuantityCalculationSettings.cs` or the new smoke, and the pre-fix source blob was re-read unchanged immediately before the head-locked squash merge.

## Validation boundary

No GitHub Actions/full build/release dispatch occurred. No local/full .NET build or licensed BricsCAD V25/V26 runtime PASS is claimed.
