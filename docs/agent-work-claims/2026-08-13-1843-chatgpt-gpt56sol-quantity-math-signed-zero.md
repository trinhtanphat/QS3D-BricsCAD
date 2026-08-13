# Work claim — QuantityMath signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-quantity-math-signed-zero-20260813`
- Registered: `2026-08-13T18:43:00+07:00`
- Baseline main SHA: `001af72347ca05fc43ebc2d6de86d2ca7d99fe66`
- Priority: P0 deterministic Core quantity canonicality hardening.

## Confirmed defect

`QuantityMath.RequireNonNegativeFinite()` accepts IEEE-754 negative zero because `-0d < 0d` is false. `Multiply()` and `Divide()` preserve their already-validated raw arithmetic result, so representable zero inputs can emit `-0d` even though neighboring persisted/calculated quantity contracts now canonicalize zero to `+0d`. The existing `QuantityMathUnderflowSmoke` compares zero numerically and therefore cannot detect the sign bit.

The pre-existing arithmetic-underflow lane is completed and remains authoritative for nonzero-operands-to-zero refusal. This claim changes only representable zero canonicality and must not weaken overflow/underflow or denominator validation.

## Reserved scope

- `src/QS3D.Core/Services/QuantityMath.cs`
- `tests/QS3D.Core.SmokeTests/QuantityMathUnderflowSmoke.cs`
- this claim file for closeout

## Intended change

- canonicalize already-validated zero results from `Multiply()` and `Divide()` to positive zero;
- preserve multiplication/division overflow and nonzero-underflow guards in their existing order;
- preserve ordinary positive and representable subnormal results;
- extend the existing focused smoke with bit-level signed-zero assertions using `BitConverter.DoubleToInt64Bits` rather than numeric equality alone.

## Excluded scope

- Wall quantity, ProjectElement, MeasurementTrace and other signed-zero lanes already completed by other agents;
- `Add`, `SubtractFloorZero`, `Hypot`, `Clamp` unless a separate demonstrated signed-zero defect requires a new claim;
- quantity business rules, formulas, UI/export, BricsCAD adapter/native paths;
- GitHub Actions, packaging, release and licensed V25/V26 qualification.

## Coordination

- Current `main` was refreshed immediately before this claim at `001af72347ca05fc43ebc2d6de86d2ca7d99fe66`.
- Recent commit search found only the completed QuantityMath arithmetic-underflow lane (`b942ff6` → `2f83c7b` → `13794a8`/`94ca583` → `9c9b918`) and a nullable-safe smoke follow-up; no QuantityMath signed-zero claim/commit was found.
- Active claim directory/path search found no QuantityMath signed-zero reservation.
- Recent Wall Quantity/ProjectElement signed-zero work is completed and does not reserve these files.

## Validation plan

- refresh `main` after this claim lands and recheck newly published collisions before touching source;
- make the smallest production diff after the existing underflow checks;
- preserve all existing underflow smoke cases and add exact sign-bit checks for negative-zero multiplication/division;
- re-fetch exact pushed source/test and inspect commit diffs/ancestry before closeout;
- execute available focused validation only; do not claim managed/native PASS for unavailable gates.

## Completion condition

Current `main` emits canonical positive zero from the reserved QuantityMath multiplication/division surfaces, existing overflow/underflow semantics remain intact, focused bit-level regression coverage is pushed/read back, concurrent work is reconciled without overwrite/force-push, and this claim is closed `COMPLETED` with only actually executed validation recorded.
