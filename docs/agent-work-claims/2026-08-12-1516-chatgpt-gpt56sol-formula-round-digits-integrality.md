# Work claim — Formula round digits integrality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-formula-round-digits-integrality`
- Registered: `2026-08-12T15:16:00+07:00`
- Completed: `2026-08-12T15:23:00+07:00`
- Baseline main SHA: `cb78830ee6846c5b516d5d96961a7a82c0840bf9`
- Claim commit: `f2911e86283caedcf771980f41accba73f6a7ad6`
- Source commit: `7d2edaa9897b6bcb2efa8535e704240fd390556f`
- Regression commit: `fe74b1318a1ee228f471d25de314001e919238a7`
- Priority: P1 — enforce the documented integer contract for `round(value, digits)` without tolerance-based coercion.

## Confirmed defect

`ExpressionEvaluator` reported that the second `round` argument must be an integer from 0 to 15, but the implementation accepted any finite value within `1e-12` of a rounded integer. For example, `round(1.25, 1.0000000000005)` was non-integral yet was silently coerced to one digit and evaluated. Exact integers 0 through 15 are exactly representable as `double`, so the tolerance broadened the public formula language beyond its stated contract.

## Completed scope

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs` — two-argument `round` digits validation now requires exact integrality after range validation.
- `tests/QS3D.Core.SmokeTests/FormulaFiniteSafetySmoke.cs` — focused regression coverage rejects near-integer values on both sides of 1 and preserves exact integer behavior at 1 and 15.
- this claim file.

The initial claim described a standalone ModuleInitializer smoke. Readback of the current smoke harness showed that the repository instead runs registered `Run()` smoke classes through `SmokeTestRegistration.RunAll()`. `FormulaFiniteSafetySmoke.Run()` was already registered, so the regression was added there without changing registration or `Program.cs`.

## Exclusions preserved

- No changes to numeric literal parsing/underflow handling, arithmetic underflow handling, reference extraction, variable normalization, function arity, midpoint rounding mode, or one-argument `round` behavior.
- No changes to Quantity Rule, persistence/schema, UI, BricsCAD runtime, or release workflows.

## Result

- Two-argument `round(value, digits)` rejects every non-integral `digits` value represented by the evaluator, including values within the former `1e-12` acceptance band.
- Exact integer digits from 0 through 15 continue to evaluate with `MidpointRounding.AwayFromZero`.
- Existing out-of-range rejection remains unchanged.

## Validation evidence

- Source commit readback shows a one-condition change from tolerance comparison to exact integrality comparison.
- Regression commit readback shows only `FormulaFiniteSafetySmoke.cs` changed, adding two rejection cases and two exact-integer success cases.
- Ancestry compare from claim `f2911e86283caedcf771980f41accba73f6a7ad6` to regression `fe74b1318a1ee228f471d25de314001e919238a7` is `ahead` by exactly two commits and includes only `ExpressionEvaluator.cs` plus `FormulaFiniteSafetySmoke.cs`.
- GitHub Actions and local BricsCAD qualification were intentionally not run for this Core-only lane, consistent with the claim plan.