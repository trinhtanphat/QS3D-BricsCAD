# Work claim — MTR-05 wall quantity signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-20260813-1839`
- Registered: `2026-08-13T18:39:00+07:00`
- Baseline main SHA: `5528c64dc7152303f449345cb6bce147639b95cd`
- Priority: P0 / MTR-05 continuous hardening.

## Confirmed defect

`WallQuantityCalculator.RequireFiniteNonNegative()` accepts IEEE-754 negative zero because `-0d < 0d` is false. `FiniteProduct()` returns the raw product, so inputs such as `lengthM = -0d` and a positive height can produce a `WallQuantities.GrossAreaM2` value whose numeric comparison is zero but whose sign bit remains negative. `OpeningCut.AreaM2` has the same raw multiplication path. Existing underflow coverage checks zero numerically and therefore does not detect signed-zero canonicality. Neighboring canonical quantity contracts now normalize zero, including `MeasurementTrace` and `ProjectElement.SetQuantity`, so Wall quantity output can disagree at the bit/canonical representation boundary even though its numeric value is zero.

## Reserved scope

Normalize representable zero results emitted by `OpeningCut.AreaM2` and the Wall quantity arithmetic path to canonical positive zero, without changing ordinary positive quantities, non-finite/underflow rejection, opening bounds, trace formulas, or category business rules.

## Expected surfaces

- `src/QS3D.Core/Services/WallQuantityCalculator.cs`
- `tests/QS3D.Core.SmokeTests/WallQuantityArithmeticUnderflowSmoke.cs`
- this claim file for close-out

## Excluded scope

- MeasurementTrace contract or MTR-03R projection behavior;
- structural-wall semantic services, Wall Quantity UI/report/export, Direct Draw or native BricsCAD paths;
- generic signed-zero refactors outside these Wall arithmetic outputs;
- opening-count bounds, arithmetic overflow/underflow policy, business deduction/clamping formulas;
- GitHub Actions, packaging, release or native V25/V26 qualification.

## Coordination / overlap

- The prior wall arithmetic-underflow lane (`f24892f` → `74423dc` → `9a8eb62` → `7b83740`) is completed; this lane preserves that policy and extends only zero canonicality.
- MTR-03R Wall quantity trace projection is completed and does not retain ownership.
- Wall Quantity responsive-footer UI work is completed and touches host/UI surfaces, not these Core arithmetic files.
- Recent ProjectElement and MAP signed-zero lanes are completed and provide neighboring canonicality evidence but do not reserve this Wall calculator/test.

## Validation plan

- add focused bit-level smoke assertions using `BitConverter.DoubleToInt64Bits` for negative-zero wall/opening inputs;
- preserve ordinary wall quantities and the existing representable-subnormal/underflow cases;
- refresh current `main` after this claim lands and recheck newly published claims before source mutation;
- inspect exact remote diffs/readback after source + regression commits;
- do not dispatch Actions or claim managed/native PASS when the local toolchain/runtime is unavailable.

## Completion condition

Current `main` emits canonical positive zero from the bounded Wall quantity arithmetic surfaces, focused bit-level regression coverage is pushed and read back, concurrent work is reconciled without overwrite/force-push, and this claim is closed `COMPLETED` with only actually executed validation recorded.
