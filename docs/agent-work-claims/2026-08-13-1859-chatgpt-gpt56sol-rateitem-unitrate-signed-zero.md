# Work claim — RateItem UnitRate decimal signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-rateitem-unitrate-zero-20260813`
- Registered: `2026-08-13T18:59:00+07:00`
- Completed: `2026-08-13T19:03:00+07:00`
- Baseline main SHA: `a19c8d63cfcbbe8de196e3d5d11e67aed27bfb05`
- Priority: `CST-01 / P1` deterministic frozen commercial-state hardening.

## Confirmed defect

`RateItem` accepted every non-negative `decimal UnitRate` by checking only `unitRate < 0m`, then stored the original decimal value. Decimal negative zero compares equal to positive zero, so a negative-zero representation passed the guard while preserving a distinct sign bit in frozen commercial state.

`RateBookSmoke` covered negative-rate rejection and deterministic lookup but had no bit-level zero-canonicality regression.

## Implemented scope

- `RateItem` now canonicalizes any accepted zero-valued `unitRate` to literal `0m` after the existing negative-rate guard.
- Positive unit rates and negative-rate rejection remain unchanged.
- `RateBookSmoke` constructs a definite decimal negative zero with `new decimal(0, 0, 0, true, 0)` and compares all `decimal.GetBits(UnitRate)` words against canonical `0m`.

## Expected surfaces

- `src/QS3D.Core/Cost/RateBook.cs`
- `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs`
- this claim file for closeout

## Excluded scope

- No edits to `EstimateLine.cs` or `EstimateRevisionCostImpact.cs`.
- No changes to rate lookup, effective-date/version selection, CostCode, currency/unit policy, cost formulas, persistence, renderer/export, WPF or BricsCAD adapters.
- No GitHub Actions, packaging, native build or licensed BricsCAD runtime qualification.

## Coordination

- Claim-only commit: `0ee2a0aaf36281833ae3575b770a9788b0066ff1`.
- Production fix: `31886c429ba7172656759675e0afde23f3d44b8f` — `fix(cost): canonicalize RateItem zero unit rate`.
- Focused regression: `43de2ac4850609d5c16c685e220abc280a126f91` — `test(cost): guard RateItem zero unit rate`.
- CST-03A remains separate; this lane did not edit its source or formulas.
- A concurrent CST-04 frozen estimate projection claim appeared after registration and explicitly excludes changes to `RateBook`; its owned source is a new `FrozenEstimateProjection.cs`, so no capability/file overlap was found.
- Concurrent ElementInstance and QuantityMath/MeasurementTrace canonicality work is disjoint.

## Validation actually executed

- Re-fetched the claim commit and confirmed it contained only this registration file.
- Re-fetched current `src/QS3D.Core/Cost/RateBook.cs`; remote blob `e02b7e0b9508ca8e17850d18da1fb128686531fe` contains the bounded zero canonicalization.
- Re-fetched current `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs`; remote blob `79ed128e9f089188324469619710fd752c86c2f6` contains the bit-level negative-zero regression while retaining existing positive/negative-rate coverage.
- Inspected exact production/test commit diffs; no `EstimateLine`, CST-03, persistence, renderer/export or host surfaces were changed by this lane.
- Re-read the concurrent CST-04 claim and confirmed it explicitly excludes `RateBook` changes.
- No GitHub Actions were dispatched. No managed executable smoke/build or licensed BricsCAD runtime validation was executed in this turn, so none is reported as PASS.

## Completion condition

Satisfied for this bounded Core source/static lane: `RateItem.UnitRate` now stores canonical positive decimal zero for zero-valued accepted input, existing validation/lookup semantics remain unchanged, the focused regression is present on remote `main`, exact source/test readback is recorded, and unavailable/unexecuted managed/native gates remain explicitly unclaimed.
