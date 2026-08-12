# Work claim — Formula round digits integrality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-formula-round-digits-integrality`
- Registered: `2026-08-12T15:16:00+07:00`
- Baseline main SHA: `cb78830ee6846c5b516d5d96961a7a82c0840bf9`
- Priority: P1 — enforce the documented integer contract for `round(value, digits)` without tolerance-based coercion.

## Confirmed defect

`ExpressionEvaluator` reports that the second `round` argument must be an integer from 0 to 15, but the current implementation accepts any finite value within `1e-12` of a rounded integer. For example, `round(1.25, 1.0000000000005)` is non-integral yet is silently coerced to one digit and evaluated. Because exact integer values 0 through 15 are exactly representable as `double`, this tolerance broadens the public formula language beyond its stated contract.

## Reserved scope

- `src/QS3D.Core/Formulas/ExpressionEvaluator.cs` — two-argument `round` digits validation only.
- one focused ModuleInitializer smoke under `tests/QS3D.Core.SmokeTests/`.
- this claim file.

## Exclusions

- Do not change numeric literal parsing/underflow handling, arithmetic underflow handling, reference extraction, variable normalization, function arity, midpoint rounding mode, or one-argument `round` behavior.
- Do not change Quantity Rule, persistence/schema, UI, BricsCAD runtime, or release workflows.

## Intended contract

- Two-argument `round(value, digits)` rejects every non-integral `digits` value, including values arbitrarily close to an integer.
- Exact integer digits from 0 through 15 continue to evaluate with `MidpointRounding.AwayFromZero`.
- Existing out-of-range rejection remains unchanged.

## Validation plan

Focused smoke proves near-integer values on both sides of an exact integer are rejected and exact boundary/canonical integer values still round correctly. Source/test will be read back from current `main`; ancestry will be verified before closeout. No GitHub Actions or local BricsCAD qualification will be run.