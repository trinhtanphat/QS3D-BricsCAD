# Work claim — QuantityMath arithmetic underflow

- Status: `ACTIVE`
- Agent: `ChatGPT web / GPT-5.6 Sol`
- Registered: `2026-08-12T15:46:00+07:00`
- Baseline main SHA: `79dbf834335d7b8b43c276b21eb1adc086a20ede`
- Priority: Proven Core quantity arithmetic integrity defect: finite non-zero multiplication/division can underflow to exact zero and silently publish a false zero quantity.

## Reserved scope

Harden `QuantityMath.Multiply()` and `QuantityMath.Divide()` so exact-zero results produced by finite non-zero arithmetic fail closed, while preserving legitimate zero operands, representable subnormal results, existing overflow handling, and denominator validation.

## Expected surfaces

- `src/QS3D.Core/Services/QuantityMath.cs`
- `tests/QS3D.Core.SmokeTests/QuantityMathUnderflowSmoke.cs`
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs`
- this claim file

## Excluded scope

- `MeasuredSolidQuantityPolicy` persisted numeric-literal parsing.
- Curtain/Grid/Geometry arithmetic helpers, formulas, Rebar math, persistence, reporting, native CAD/runtime, release/preflight workflows.

## Validation plan

- Focused smoke regression for multiplication underflow and division underflow.
- Preserve true-zero multiplication/division and smallest representable positive subnormal results.
- Static compile-surface review if executable .NET smoke cannot be run locally; do not claim unexecuted PASS.

## Coordination

The measured-solid numeric-literal-underflow lane completed at `79dbf834335d7b8b43c276b21eb1adc086a20ede` and explicitly excluded `QuantityMath` arithmetic. Recent Curtain multiplication-underflow work is also completed and serves only as precedent; no active neighboring claim found owns these source/test surfaces.

## Completion condition

Product guard and focused regression are pushed to `main`, the claim is updated to `COMPLETED` with exact commit/evidence, and no overlapping active claim is left unresolved.
