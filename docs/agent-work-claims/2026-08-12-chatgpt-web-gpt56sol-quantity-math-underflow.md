# Work claim — Shared quantity arithmetic underflow integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-quantity-math-underflow-20260812`
- Registered: `2026-08-12`
- Baseline main SHA: `f0c2d97ae57dc196caac5c3edd83c97cfd5306e2`
- Priority: P1 — positive quantity arithmetic must not silently collapse to exact zero.

## Confirmed defect

`QuantityMath.Multiply(...)` and `QuantityMath.Divide(...)` reject non-finite results but accept IEEE-754 underflow to exact zero. Public regeneration paths can therefore turn strictly positive finite semantic dimensions into zero quantities or zero stair fallbacks instead of failing closed. This is inconsistent with the wall quantity underflow contract already established in the repository.

This lane does not introduce minimum dimensions. It only rejects complete loss of positive magnitude in shared quantity arithmetic.

## Reserved scope

- `src/QS3D.Core/Services/QuantityMath.cs`
- `tests/QS3D.Core.SmokeTests/QuantityMathUnderflowSmoke.cs`
- this claim file

## Intended contract

- Preserve zero for multiplication when either operand is legitimately zero.
- Preserve zero for division when the numerator is legitimately zero.
- Preserve finite non-zero results, including representable subnormals.
- Reject multiplication when both operands are positive and the result underflows to exact zero.
- Reject division when the numerator and denominator are positive and the result underflows to exact zero.
- Preserve current non-finite, negative-input, denominator, overflow and ordinary regeneration behavior.

## Validation boundary

Focused public-regenerator smoke/readback only. No GitHub Actions, full local .NET build/smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed without execution.