# Work claim — QuantityMath Add signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-quantity-math-add-signed-zero-20260813`
- Registered: `2026-08-13T18:50:00+07:00`
- Completed: `2026-08-13T18:52:00+07:00`
- Baseline main SHA: `ebeea64c372e8e2a6930a926c27e82bc49fa7c13`
- Priority: P0 deterministic Core quantity canonicality hardening.

## Confirmed defect

`QuantityMath.Add()` validates both operands with the non-negative finite guard, which accepts IEEE-754 negative zero, then previously returned the raw `left + right` result. A pair of negative-zero operands could therefore preserve a negative-zero sign bit while the completed QuantityMath multiplication/division lane and neighboring quantity contracts canonicalize zero to `+0d`.

## Implemented scope

- `QuantityMath.Add()` now canonicalizes an already finite zero result to literal `+0d`.
- Addition overflow validation remains before zero normalization.
- Ordinary positive and representable subnormal addition remains unchanged.
- `QuantityMathUnderflowSmoke` now checks `Add(-0d, -0d)` via `CanonicalPositiveZero`, plus ordinary `1 + 2 == 3` and representable `double.Epsilon + 0 == double.Epsilon` sanity cases.
- All prior Multiply/Divide signed-zero, subnormal and underflow regressions remain present.

## Excluded scope

- completed QuantityMath Multiply/Divide signed-zero lane;
- `SubtractFloorZero`, `Hypot`, `Clamp`; these require independently demonstrated defects and separate claims;
- business formulas, UI/export, BricsCAD adapter/native paths, Actions/release/runtime qualification.

## Coordination

- Claim commit: `15fd8d8c9ed7902dd6864cc89b9cbd0d60044f4b`.
- Production fix: `3df9a904a5bf2d2da79a058fc900d8ec92ed9ddb` — `fix(core): canonicalize QuantityMath Add signed zero`.
- Focused regression: `53523ee0060fbc1104e48ad6eedc96012e37ef82` — `test(core): guard QuantityMath Add signed zero`.
- Previous Multiply/Divide lane closed before this claim, so the same two files were not simultaneously reserved.
- Post-regression refresh showed `main` exactly at `53523ee0060fbc1104e48ad6eedc96012e37ef82`; no concurrent overwrite touched the reserved files before closeout.

## Validation actually executed

- Refreshed `main` immediately after claim and rechecked recent QuantityMath commits before source mutation.
- Exact source readback confirms blob `e1131a6625dc5585806af15f6528e4c3f6aa84f7` and the production diff is limited to the `Add()` zero return.
- Exact smoke readback confirms blob `75d7440aa50883879abc62fd215ac19da3080214`, including the new Add sign-bit/positive/subnormal cases and all previously added Multiply/Divide coverage.
- Hosted environment still has no `dotnet`, `csc`, `mcs` or `msbuild`; managed compile/smoke execution remains `NOT_RUN`, not PASS.
- No GitHub Actions, packaging, BricsCAD adapter build or licensed native runtime qualification was dispatched/executed.

## Completion condition

Satisfied for this bounded Core source/static lane: `QuantityMath.Add()` emits canonical positive zero for zero-valued sums, existing overflow and prior QuantityMath regressions remain intact, exact source/test were read back on current `main`, and unavailable managed/native gates remain explicitly unclaimed.
