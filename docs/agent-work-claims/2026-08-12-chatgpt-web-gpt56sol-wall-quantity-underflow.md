# Work claim — Wall quantity finite underflow integrity

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-underflow-20260812`
- Registered: `2026-08-12`
- Baseline main SHA: `0286d6a0bf5d32f8391633ef2252923feecbdc53`
- Priority: P1 — positive wall/opening quantities must not silently collapse to exact zero.

## Confirmed defect

`OpeningCut.AreaM2` and `WallQuantityCalculator.FiniteProduct(...)` reject non-finite multiplication results but accept IEEE-754 underflow to exact zero. With finite positive operands whose mathematical product is positive but smaller than the representable range, wall/opening area or volume silently becomes zero. This can erase a real opening deduction or wall quantity instead of failing closed.

This lane does not introduce a minimum wall/opening dimension; it only rejects complete loss of positive magnitude in existing arithmetic.

## Reserved scope

- `src/QS3D.Core/Services/WallQuantityCalculator.cs`
- one new focused Core smoke file under `tests/QS3D.Core.SmokeTests/`
- this claim file

## Intended contract

- Preserve zero when either multiplication operand is legitimately zero.
- Preserve finite non-zero products, including representable subnormals.
- Reject multiplication when both operands are non-zero and the result underflows to exact zero.
- Preserve existing finite/nonnegative validation, opening bounds, clamping and overflow behavior.
- Do not change domain minima, CAD/native behavior, or wall/opening formulas.

## Validation boundary

Focused source-safe smoke/readback only. No GitHub Actions, full local .NET build/smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed without execution.
