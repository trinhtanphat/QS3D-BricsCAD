# Work claim — ElementInstance measurement signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-element-instance-signed-zero-20260813`
- Registered: `2026-08-13T18:59:00+07:00`
- Completed: `2026-08-13T19:02:00+07:00`
- Baseline main SHA: `a19c8d63cfcbbe8de196e3d5d11e67aed27bfb05`
- Priority: P0 deterministic Core measurement canonicality.

## Confirmed defect

`ElementInstance` has thirteen non-negative physical measurement setters that all call `RequireNonNegativeFinite()`. IEEE-754 negative zero passes the existing `value < 0d` guard; before this lane the helper returned the raw value, so every measurement property could persist a `-0d` sign bit. The existing `ElementInstanceNonNegativeMeasurementsSmoke` verified rejection of negative/non-finite inputs and acceptance of numeric zero/positive inputs, but did not read values back or check the zero sign bit.

This is separate from the completed `ProjectElement.SetQuantity` signed-zero lane because `ElementInstance` owns strongly typed measurement fields and its own validation helper.

## Implemented scope

- The shared `RequireNonNegativeFinite()` helper keeps the existing NaN/infinity/negative rejection contract and now returns literal `+0d` for every accepted zero-valued measurement.
- All thirteen typed measurement setters therefore canonicalize `-0d` without duplicating setter-specific logic.
- Positive measurement values remain unchanged.
- The existing registered smoke now maintains parallel setter/getter coverage for all thirteen properties and bit-checks each value after assigning `-0d` using `BitConverter.DoubleToInt64Bits`.
- The smoke also verifies `NetConcreteM3` exposes canonical positive zero for zero-valued gross/deduction inputs; no NetConcrete formula, flooring or overflow behavior was changed.

## Excluded scope

- `ProjectElement`, QuantityMath, Wall Quantity and Cost/RateItem signed-zero lanes;
- quantity formulas, persistence/report/export, UI, BricsCAD adapter/native operations;
- unrelated `ElementInstance` id/floor/family/source-handle contracts;
- GitHub Actions, packaging, release and licensed V25/V26 runtime qualification.

## Coordination / moving-main reconciliation

- Existing ElementInstance history contains the completed finite `NetConcreteM3` hardening and the 2026-08-12 negative/non-finite measurement setter fix; both contracts were preserved.
- Immediately before this claim landed, concurrent commit `0ee2a0aaf36281833ae3575b770a9788b0066ff1` claimed RateItem UnitRate signed-zero canonicality. It became the actual parent of this claim, but reserves only Cost/RateBook source/test files, so it is disjoint from ElementInstance.
- Claim commit: `facf25014e70077ab1c015d7f28ce73afd3968a9`.
- Production fix: `c3f2eafcea469dc6a7181909e7de6bb7dbe9673d` — `fix(domain): canonicalize ElementInstance signed zero`.
- Focused regression: `88bcc024c8593cb15bb316a70c2438acf580af3f` — `test(domain): guard ElementInstance signed zero`.
- After this regression, the RateItem owner landed source/test work on disjoint files; latest checked `main` was `43de2ac4850609d5c16c685e220abc280a126f91`, whose parent is this lane's regression commit. No concurrent commit modified the reserved ElementInstance source/test files.

## Validation actually executed

- Exact production readback confirmed blob `ba891c35c74abe2b9e5d0aabc773d4efb1b9ab36`; the only production change is zero canonicalization in the shared non-negative finite helper.
- Exact smoke readback confirmed blob `70e4a5835fc712f2d7f88dfc7caefdda9066cb31`; it bit-checks all thirteen setter/getter pairs, guards coverage-length drift, verifies NetConcrete zero, and preserves existing negative/non-finite and positive-input coverage.
- Moving-main ancestry was reconciled against concurrent Cost work; current-main readback after regression retained both exact ElementInstance blobs unchanged.
- Hosted environment has no `dotnet`, `csc`, `mcs` or `msbuild`, so managed compile/smoke execution is `NOT_RUN`; no managed PASS is claimed.
- No GitHub Actions, packaging, adapter build or licensed BricsCAD runtime qualification was dispatched/executed.

## Completion condition

Satisfied for this bounded Core source/static lane: all accepted zero-valued `ElementInstance` physical measurements are stored as canonical positive zero, existing negative/non-finite rejection and positive behavior remain intact, focused bit-level coverage is on `main`, concurrent work was not overwritten, and unavailable managed/native gates remain explicitly unclaimed.
