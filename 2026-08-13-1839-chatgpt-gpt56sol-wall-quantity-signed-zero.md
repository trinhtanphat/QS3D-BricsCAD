# Work claim — MTR-05 wall quantity signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-20260813-1839`
- Registered: `2026-08-13T18:39:00+07:00`
- Completed: `2026-08-13T18:42:15+07:00`
- Baseline main SHA: `5528c64dc7152303f449345cb6bce147639b95cd`
- Priority: P0 / MTR-05 continuous hardening.

## Confirmed defect

`WallQuantityCalculator.RequireFiniteNonNegative()` accepts IEEE-754 negative zero because `-0d < 0d` is false. Before this lane, `FiniteProduct()` returned the raw multiplication result and `OpeningCut.AreaM2` did the same after its finite/underflow guards. Inputs such as `lengthM = -0d` with positive height could therefore emit a zero-valued `WallQuantities.GrossAreaM2` whose sign bit remained negative. Existing underflow coverage compared zero numerically and did not detect that canonicality defect. Neighboring canonical quantity contracts normalize zero, so the Wall arithmetic surface was inconsistent at the bit/canonical representation boundary.

## Implemented scope

- `OpeningCut.AreaM2` now returns canonical `+0d` whenever the already-validated product compares equal to zero.
- `WallQuantityCalculator.FiniteProduct()` now returns canonical `+0d` whenever the already-validated product compares equal to zero.
- Non-finite checks and the pre-existing nonzero-operands-to-zero underflow guard remain before normalization, so this change does not hide arithmetic underflow.
- No wall deduction/clamping formula, opening enumeration bound, MTR-03R trace projection, structural-wall semantic behavior or UI/report behavior changed.
- `WallQuantityArithmeticUnderflowSmoke` now exercises negative-zero wall length, negative-zero thickness and negative-zero opening width and checks the sign bit with `BitConverter.DoubleToInt64Bits`, while retaining the existing underflow, representable-subnormal and ordinary-quantity cases.

## Coordination / overlap reconciliation

- Claim-only commit on `main`: `89ee73ab100a4e7c800fa4748e40e5f3fca37383` — `chore(agent): claim wall quantity signed-zero canonicality`.
- The prior wall arithmetic-underflow lane (`f24892f` → `74423dc` → `9a8eb62` → `7b83740`) was already completed; this lane preserved its failure policy and extended only zero canonicality.
- MTR-03R Wall quantity trace projection and Wall Quantity responsive-footer work were already completed and did not retain ownership of these Core arithmetic files.
- Recent ProjectElement and MAP signed-zero lanes were completed and provided neighboring canonicality evidence without reserving this calculator/test.
- Post-claim refresh showed the claim commit at current `main` before source mutation.
- Immediately after the focused regression commit, `main` was re-fetched and remained exactly `18992bfb9aeb6a490230a7b87bd5759ef64cd750`; therefore no concurrent commit touched the reserved files before close-out.

## Implementation commits

- `29508a20fa725ea9504fb1354cb06629c7a80b1a` — `fix(quantity): canonicalize wall signed zero`.
- `18992bfb9aeb6a490230a7b87bd5759ef64cd750` — `test(quantity): guard wall signed zero`.
- GitHub diff readback confirms the production commit changes only the two zero-return sites in `WallQuantityCalculator.cs` (plus terminal newline normalization), and the regression commit changes only `WallQuantityArithmeticUnderflowSmoke.cs`.

## Validation actually executed

- Executed: current-`main` refresh before claim, post-claim HEAD verification, exact production/test commit diff inspection, direct remote source/test readback and final pre-close current-`main` refresh.
- Remote readback at the regression commit confirmed production blob `45cd13a0f08eb7eb80d313969f4dfadeb8380438` and smoke blob `6891f9fa8c57f3941a0198bd8b59b17f7a06cf08`.
- The remote smoke source verifies zero sign using `BitConverter.DoubleToInt64Bits`, rather than numeric `== 0`, for every affected zero-valued Wall output covered by this lane.
- Local toolchain capability was probed in this session: no `dotnet`, `csc`, `mcs` or `msbuild` executable is installed, so no managed compile/smoke PASS is claimed.
- Not executed: GitHub Actions, repository build, registered Core smoke executable, installed-reference BricsCAD V25/V26 build or licensed BricsCAD runtime qualification. No PASS is claimed for any unexecuted managed/native gate.

## Completion condition

Satisfied for this bounded MTR-05 repair: claim-first ownership was published, current source evidence proved the signed-zero gap, Wall multiplication outputs now canonicalize representable zero without weakening finite/underflow guards, focused bit-level regression coverage is on `main` and was read back remotely, no concurrent overwrite occurred before close-out, and all unavailable managed/native gates remain explicitly unclaimed.
