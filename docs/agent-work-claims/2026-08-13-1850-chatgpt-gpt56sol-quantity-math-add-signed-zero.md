# Work claim — QuantityMath Add signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-quantity-math-add-signed-zero-20260813`
- Registered: `2026-08-13T18:50:00+07:00`
- Baseline main SHA: `ebeea64c372e8e2a6930a926c27e82bc49fa7c13`
- Priority: P0 deterministic Core quantity canonicality hardening.

## Confirmed defect

`QuantityMath.Add()` validates both operands with the non-negative finite guard, which accepts IEEE-754 negative zero, then returns the raw `left + right` result. A pair of negative-zero operands can therefore preserve a negative-zero sign bit even though the completed QuantityMath multiplication/division lane and neighboring quantity contracts canonicalize zero to `+0d`.

## Reserved scope

- `src/QS3D.Core/Services/QuantityMath.cs`
- `tests/QS3D.Core.SmokeTests/QuantityMathUnderflowSmoke.cs`
- this claim file for closeout

## Intended change

- canonicalize an already finite `Add()` result equal to zero to literal positive zero;
- preserve addition overflow behavior and all ordinary positive/subnormal sums;
- add a bit-level regression for `Add(-0d, -0d)` using the existing QuantityMath reflection smoke and its `CanonicalPositiveZero` helper.

## Excluded scope

- completed QuantityMath Multiply/Divide signed-zero lane;
- `SubtractFloorZero`, `Hypot`, `Clamp` unless independently demonstrated and separately claimed;
- business formulas, UI/export, BricsCAD adapter/native paths, Actions/release/runtime qualification.

## Coordination

- Previous QuantityMath Multiply/Divide lane closed at `ebeea64c372e8e2a6930a926c27e82bc49fa7c13` and explicitly excluded `Add`.
- Two recent exact commit searches (`QuantityMath Add signed zero`, `quantity addition signed zero`) returned no competing lane.
- This claim reuses the same two Core files only after the prior claim is `COMPLETED`; no simultaneous ownership is being asserted.

## Validation plan

- refresh `main` after claim and recheck collision before source mutation;
- keep the production diff to the Add return only;
- retain all existing Multiply/Divide underflow and signed-zero regression cases;
- re-fetch exact pushed source/test and close only with actually executed validation.

## Completion condition

`QuantityMath.Add()` emits canonical positive zero for zero-valued sums, existing overflow and prior QuantityMath regressions remain intact, source/test readback confirms the bounded change, and this claim is closed `COMPLETED` without fabricated managed/native PASS.
