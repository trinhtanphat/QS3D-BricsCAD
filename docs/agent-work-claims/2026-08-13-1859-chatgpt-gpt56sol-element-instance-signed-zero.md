# Work claim — ElementInstance measurement signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-element-instance-signed-zero-20260813`
- Registered: `2026-08-13T18:59:00+07:00`
- Baseline main SHA: `a19c8d63cfcbbe8de196e3d5d11e67aed27bfb05`
- Priority: P0 deterministic Core measurement canonicality.

## Confirmed defect

`ElementInstance` has thirteen non-negative physical measurement setters that all call `RequireNonNegativeFinite()`. IEEE-754 negative zero passes the existing `value < 0d` guard, and the helper returns the raw value, so every measurement property can persist a `-0d` sign bit. The existing `ElementInstanceNonNegativeMeasurementsSmoke` verifies rejection of negative/non-finite inputs and acceptance of numeric zero/positive inputs, but it never reads values back or checks the zero sign bit.

This differs from the recently completed `ProjectElement.SetQuantity` signed-zero lane: `ElementInstance` owns separate strongly typed measurement fields and a separate validation helper.

## Reserved scope

- `src/QS3D.Core/Domain/ElementInstance.cs`
- `tests/QS3D.Core.SmokeTests/ElementInstanceNonNegativeMeasurementsSmoke.cs`
- this claim file for closeout

## Intended change

- keep the existing finite/non-negative rejection contract unchanged;
- canonicalize every accepted zero measurement in the shared setter helper to literal `+0d`;
- extend the existing focused smoke so all thirteen setters are exercised with `-0d` and their corresponding getters are checked at bit level;
- verify `NetConcreteM3` also exposes canonical positive zero when gross/deduction inputs are zero-valued, without changing its positive/flooring/overflow business behavior.

## Excluded scope

- `ProjectElement`, QuantityMath and Wall Quantity signed-zero lanes already completed;
- quantity formulas, persistence/report/export, UI, BricsCAD adapter/native operations;
- unrelated `ElementInstance` id/floor/family/source-handle contracts;
- GitHub Actions, packaging, release and licensed V25/V26 runtime qualification.

## Coordination

- Recent exact searches for `ElementInstance signed zero` and `ElementInstance measurement canonicality` returned no competing lane.
- Existing ElementInstance history contains completed finite `NetConcreteM3` hardening and the 2026-08-12 negative/non-finite measurement setter fix; this lane preserves both contracts and only closes the signed-zero representation gap.
- Current `main` was refreshed at `a19c8d63cfcbbe8de196e3d5d11e67aed27bfb05` immediately before claim; the concurrent MeasurementTrace `none` policy lane is already completed and disjoint.

## Validation plan

- refresh `main` after this claim and recheck ElementInstance recent commits before source mutation;
- keep production change in the shared measurement validator/canonicalizer unless exact source evidence requires more;
- extend the existing registered smoke rather than adding another runner;
- re-fetch exact pushed source/test and inspect moving-main ancestry before closeout;
- report managed/native execution as `NOT_RUN` when unavailable; do not fabricate PASS.

## Completion condition

All accepted zero-valued `ElementInstance` physical measurements are stored as canonical positive zero, existing negative/non-finite rejection and positive values remain intact, focused bit-level regression coverage is on current `main`, and the claim is closed with exact readback and truthful validation boundaries.
