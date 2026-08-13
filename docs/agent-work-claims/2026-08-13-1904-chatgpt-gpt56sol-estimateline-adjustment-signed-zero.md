# Work claim — EstimateLine commercial adjustment decimal signed-zero canonicality

- Status: `COMPLETED`
- Agent: `chatgpt-gpt56sol-estimateline-adjustment-zero-20260813`
- Registered: `2026-08-13T19:04:00+07:00`
- Completed: `2026-08-13T19:07:00+07:00`
- Baseline main SHA: `2278610fed5ba363ff5c94b07b95f8cb5aba2445`
- Priority: `CST-02 / P1` deterministic frozen estimate-state hardening.

## Confirmed defect

`EstimateLine.Create()` accepted a `decimal commercialAdjustmentQuantity`, used numeric equality to decide whether a reason was required, performed estimating-quantity arithmetic with the raw value, and stored that raw value in immutable `CommercialAdjustmentQuantity`. A decimal negative-zero representation therefore compared equal to zero while remaining representation-distinct in frozen estimate state.

## Implemented scope

- `EstimateLine.Create()` now canonicalizes a zero-valued commercial adjustment to literal `0m` once before commercial handling.
- The canonical value is reused for adjustment-reason validation, estimating-quantity arithmetic and the immutable stored property.
- Non-zero positive/deduction adjustment semantics, rate resolution and final-amount formula remain unchanged.
- `EstimateLineSmoke` constructs a definite decimal negative zero with `new decimal(0, 0, 0, true, 0)`, compares all `decimal.GetBits(CommercialAdjustmentQuantity)` words against canonical `0m`, and verifies reason/estimating/final amount remain unchanged.

## Expected surfaces

- `src/QS3D.Core/Cost/EstimateLine.cs`
- `tests/QS3D.Core.SmokeTests/EstimateLineSmoke.cs`
- this claim file for closeout

## Excluded scope

- No edits to `RateBook.cs`, `EstimateRevisionCostImpact.cs`, measurement contracts or cost formulas.
- No changes to non-zero commercial adjustment semantics, reason policy, rate resolution, decimal overflow/underflow policy, persistence, renderer/export, WPF or BricsCAD adapters.
- No GitHub Actions, packaging or native/local PASS claims.

## Coordination

- Claim-only commit: `230e8540fe77ff04f855b3bae9cf0cff34870a18`.
- Production fix: `efa70d2e50559d1764ed4e3d82c52867a0956cb9` — `fix(cost): canonicalize EstimateLine zero adjustment`.
- Focused regression: `9f7f5392066f5af3cb119b9a6842098180b271b8` — `test(cost): guard EstimateLine zero adjustment`.
- RateItem UnitRate signed-zero lane completed at `2278610fed5ba363ff5c94b07b95f8cb5aba2445` before this claim.
- Concurrent CST-04 frozen estimate projection remained separate: its claim explicitly excludes `EstimateLine`, and its implementation/test/registration landed on new projection surfaces while this lane was active.
- A later MTR-04 adjustment-rule association claim is Measurement/inspector scope and does not overlap these Cost files/invariant.
- CST-03A remains separate and was not edited.

## Validation actually executed

- Refreshed current `main` after claim and again after source/test commits; no competing `EstimateLine`/commercial-adjustment claim appeared.
- Exact production commit diff confirms only canonical-local-value plumbing in `EstimateLine.Create()`.
- Exact test commit diff confirms one focused signed-zero regression plus registration in the existing `EstimateLineSmoke.Run()` path.
- Re-fetched current `src/QS3D.Core/Cost/EstimateLine.cs`; remote blob `ed9540633f838510251a6992da9e0eb2bf14e92a` contains the canonical commercial adjustment flow.
- Re-fetched current `tests/QS3D.Core.SmokeTests/EstimateLineSmoke.cs`; remote blob `7bbb8c45e79620dc6e3a0c3614455b01e6782c10` contains the bit-level negative-zero regression and existing non-zero adjustment/overflow coverage.
- No GitHub Actions were dispatched. No managed executable smoke/build or licensed BricsCAD runtime validation was executed in this turn, so none is reported as PASS.

## Completion condition

Satisfied for this bounded Core source/static lane: zero-valued commercial adjustments are stored and consumed as canonical positive decimal zero without changing non-zero business semantics, focused regression and exact remote readback are present, and unavailable/unexecuted managed/native gates remain explicitly unclaimed.
