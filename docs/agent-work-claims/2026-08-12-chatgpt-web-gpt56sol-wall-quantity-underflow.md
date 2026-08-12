# Work claim — Wall quantity finite underflow integrity

- Status: `COMPLETED`
- Agent: `chatgpt-web-gpt56sol-wall-quantity-underflow-20260812`
- Registered: `2026-08-12`
- Baseline main SHA: `0286d6a0bf5d32f8391633ef2252923feecbdc53`
- Priority: P1 — positive wall/opening quantities must not silently collapse to exact zero.

## Confirmed defect

`OpeningCut.AreaM2` and `WallQuantityCalculator.FiniteProduct(...)` rejected non-finite multiplication results but accepted IEEE-754 underflow to exact zero. With finite positive operands whose mathematical product is positive but smaller than the representable range, wall/opening area or volume silently became zero. This could erase a real opening deduction or wall quantity instead of failing closed.

## Resolution

- Source fix: `9a8eb62553274edd419a22497e095419572f5ec3`
- Focused regression: `74423dc7f87966229b606c20b0a97911fdc3fbd3`
- `OpeningCut.AreaM2` now rejects positive-factor multiplication that collapses to exact zero.
- `WallQuantityCalculator.FiniteProduct(...)` applies the same fail-closed rule to gross area, gross/deduction volume and other wall products.
- Legitimate zero operands remain zero.
- Finite non-zero results, including representable subnormals, remain accepted.
- Existing finite/nonnegative validation, opening bounds, clamping and overflow behavior are unchanged.

## Validation boundary

Source and focused smoke were read back from `main`. The smoke was added but not executed in this lane. No GitHub Actions, full local .NET build/smoke PASS, or BricsCAD V25/V26 runtime PASS is claimed.
