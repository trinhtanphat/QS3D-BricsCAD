# Work claim — RateItem UnitRate decimal signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-rateitem-unitrate-zero-20260813`
- Registered: `2026-08-13T18:59:00+07:00`
- Baseline main SHA: `a19c8d63cfcbbe8de196e3d5d11e67aed27bfb05`
- Priority: `CST-01 / P1` deterministic frozen commercial-state hardening.

## Confirmed defect

Current `RateItem` accepts every non-negative `decimal UnitRate` by checking only `unitRate < 0m`, then stores the original decimal value. Decimal negative zero compares equal to positive zero, so `-0m` passes the guard while preserving a distinct sign-bit representation. `RateBook` is frozen commercial state consumed by `EstimateLine` and revision-cost logic, so equivalent zero rates should not retain representation-dependent state.

Current `RateBookSmoke` covers negative-rate rejection and deterministic lookup but has no bit-level zero-canonicality regression.

## Reserved scope

Canonicalize an accepted zero `RateItem.UnitRate` to literal positive `0m` while preserving all positive rate values and the existing negative-rate rejection.

## Expected surfaces

- `src/QS3D.Core/Cost/RateBook.cs`
- `tests/QS3D.Core.SmokeTests/RateBookSmoke.cs`
- this claim file for closeout

## Excluded scope

- No edits to `EstimateLine.cs` or `EstimateRevisionCostImpact.cs`.
- No changes to rate lookup, effective-date/version selection, CostCode, currency/unit policy, cost formulas, persistence, renderer/export, WPF or BricsCAD adapters.
- CST-03A revision cost impact remains owned by its current ACTIVE claim and is consumed read-only.
- No GitHub Actions, packaging, native build or licensed BricsCAD runtime qualification.

## Validation plan

- Refresh current `main` after this claim and recheck Cost/CST claims before source mutation.
- Keep production change to the `RateItem` constructor assignment after the existing non-negative guard.
- Add a focused regression constructing `RateItem` with a decimal negative-zero representation and assert `decimal.GetBits(UnitRate)` matches canonical `0m`; retain positive and negative-rate cases.
- Re-fetch exact source/test blobs and inspect the pushed diffs.
- Reconcile against moving `main` before closeout; managed smoke execution is reported only if an actual .NET execution path exists.

## Coordination

- CST-01A RateBook core and CST-02A EstimateLine core are completed and reused without reopening their feature scope.
- CST-03A is currently ACTIVE and explicitly excludes RateBook changes unless a separate blocking defect is independently proven/claimed; this is that separate narrow invariant and it does not edit CST-03 surfaces or decomposition formulas.
- Recent targeted commit searches for `RateItem` and `UnitRate` returned no competing claim/implementation before registration.
- Concurrent MTR-05 and QuantityMath signed-zero lanes are completed/disjoint.

## Completion condition

`RateItem.UnitRate` stores canonical positive decimal zero for any zero-valued accepted input, existing rate validation/lookup semantics remain unchanged, focused bit-level regression is pushed, exact remote readback confirms the bounded change, and this claim is marked `COMPLETED` with only actually executed validation reported.
