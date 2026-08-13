# Work claim — QuantityMath signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-quantity-math-signed-zero-20260813`
- Registered: `2026-08-13T18:43:00+07:00`
- Completed: `2026-08-13T18:49:00+07:00`
- Baseline main SHA: `001af72347ca05fc43ebc2d6de86d2ca7d99fe66`
- Priority: P0 deterministic Core quantity canonicality hardening.

## Confirmed defect

`QuantityMath.RequireNonNegativeFinite()` accepts IEEE-754 negative zero because `-0d < 0d` is false. Before this lane, `Multiply()` and `Divide()` returned their already-validated raw arithmetic result, so representable zero inputs could emit `-0d`. The existing `QuantityMathUnderflowSmoke` compared zero numerically and therefore could not detect the sign bit.

The pre-existing arithmetic-underflow lane remains authoritative for nonzero-operands-to-zero refusal. This lane changes only representable zero canonicality and does not weaken overflow/underflow or denominator validation.

## Implemented scope

- `QuantityMath.Multiply()` now returns literal `+0d` when its already-validated result compares equal to zero.
- `QuantityMath.Divide()` now returns literal `+0d` when its already-validated result compares equal to zero.
- Existing finite/overflow and nonzero-underflow checks remain before normalization.
- Ordinary positive and representable subnormal results remain unchanged.
- `QuantityMathUnderflowSmoke` now checks `-0d * x`, `x * -0d`, and `-0d / x` with `BitConverter.DoubleToInt64Bits`, while retaining the previous zero, subnormal and underflow cases.

## Excluded scope

- Wall quantity, ProjectElement, MeasurementTrace and other signed-zero lanes already completed by other agents;
- `Add`, `SubtractFloorZero`, `Hypot`, `Clamp`; these require separate demonstrated defects/claims if pursued;
- quantity business rules, formulas, UI/export, BricsCAD adapter/native paths;
- GitHub Actions, packaging, release and licensed V25/V26 qualification.

## Coordination

- Claim-only commit: `e2b0748d0c88fccb08a3b23ee557e313c5eb9fe5` — `chore(agent): claim QuantityMath signed-zero canonicality`.
- Production fix: `5a6b5729ba40999f13fbcc82fa3dd864e9aa0174` — `fix(core): canonicalize QuantityMath signed zero`.
- Focused regression: `4e034639ed7b83dd7854cbfe0f1f4413bc3ad849` — `test(core): guard QuantityMath signed zero`.
- Recent history before claim contained only the completed QuantityMath arithmetic-underflow lane (`b942ff6` → `2f83c7b` → `13794a8`/`94ca583` → `9c9b918`) and its nullable-safe smoke follow-up; no competing QuantityMath signed-zero lane was present.
- Post-regression refresh showed `main` exactly at `4e034639ed7b83dd7854cbfe0f1f4413bc3ad849`; no concurrent overwrite touched the reserved source/test before closeout.

## Validation actually executed

- Refreshed current `main` before claim, immediately after claim and before source mutation.
- Re-fetched exact pushed production blob `d77c10e5fef300dda0c469473206a78a894346d2`; source readback shows only the two zero-return sites changed and the pre-existing underflow guards remain before normalization.
- Re-fetched exact pushed smoke blob `f50a5bf471a427d96d8a409f4a7894e3ad154d4f`; regression readback confirms sign-bit assertions plus all prior underflow/subnormal cases.
- GitHub commit readback confirms the production commit is 2 additions / 2 deletions in `QuantityMath.cs`, and the regression commit is 10 additions only in `QuantityMathUnderflowSmoke.cs`.
- Hosted toolchain probe found no `dotnet`, `csc`, `mcs` or `msbuild`, so managed compile/smoke execution was `NOT_RUN` and no managed PASS is claimed.
- No GitHub Actions, BricsCAD adapter build, packaging or licensed native runtime qualification was dispatched/executed.

## Completion condition

Satisfied for this bounded Core source/static lane: multiplication/division zero outputs are canonical positive zero, pre-existing overflow/underflow semantics remain intact, focused bit-level regression coverage is on `main` and was read back exactly, concurrent work was reconciled without overwrite/force-push, and unavailable managed/native gates remain explicitly unclaimed.
