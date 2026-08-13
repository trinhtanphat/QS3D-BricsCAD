# Work claim — EstimateLine commercial adjustment decimal signed-zero canonicality

- Status: `ACTIVE`
- Agent: `chatgpt-gpt56sol-estimateline-adjustment-zero-20260813`
- Registered: `2026-08-13T19:04:00+07:00`
- Baseline main SHA: `2278610fed5ba363ff5c94b07b95f8cb5aba2445`
- Priority: `CST-02 / P1` deterministic frozen estimate-state hardening.

## Confirmed defect

Current `EstimateLine.Create()` accepts a `decimal commercialAdjustmentQuantity`, uses numeric equality to decide whether a reason is required, performs estimating-quantity arithmetic with the raw value, and stores that raw value in immutable `CommercialAdjustmentQuantity`. A decimal negative-zero representation compares equal to zero, so it needs no reason but can remain representation-distinct in the frozen estimate line.

The completed RateItem zero lane established the same canonical-commercial-state invariant for `UnitRate`. Current `EstimateLineSmoke` has no bit-level adjustment-zero regression.

## Reserved scope

Canonicalize zero-valued `commercialAdjustmentQuantity` once at `EstimateLine.Create()` entry to commercial handling, then use the canonical value for reason validation, estimating-quantity arithmetic and the stored frozen property.

## Expected surfaces

- `src/QS3D.Core/Cost/EstimateLine.cs`
- `tests/QS3D.Core.SmokeTests/EstimateLineSmoke.cs`
- this claim file for closeout

## Excluded scope

- No edits to `RateBook.cs`, `EstimateRevisionCostImpact.cs`, measurement contracts or cost formulas.
- No changes to non-zero commercial adjustment semantics, reason policy, rate resolution, decimal overflow/underflow policy, persistence, renderer/export, WPF or BricsCAD adapters.
- CST-04 frozen estimate projection remains owned by its ACTIVE claim; this lane does not edit its new projection source/test and only hardens the already-existing canonical EstimateLine dependency that CST-04 explicitly excludes from its own writes.
- No GitHub Actions, packaging or native/local PASS claims.

## Validation plan

- Refresh `main` after claim and recheck Cost/CST claims before source mutation.
- Keep production change to one canonical local decimal value reused for reason/arithmetic/store.
- Add focused regression using `new decimal(0, 0, 0, true, 0)` and compare `decimal.GetBits(CommercialAdjustmentQuantity)` to canonical `0m`; confirm zero adjustment still requires no reason and ordinary non-zero positive/negative adjustments remain covered by existing smoke.
- Re-fetch exact source/test blobs and inspect pushed diffs.
- Reconcile moving `main` and concurrent CST-04 before closeout; report only validation actually executed.

## Coordination

- RateItem UnitRate signed-zero lane completed at `2278610fed5ba363ff5c94b07b95f8cb5aba2445` before this claim.
- CST-04 claim `652fc68bacf74723bc5120bffdd5e5ed41ede527` explicitly excludes `EstimateLine` edits and owns only frozen projection source + focused regression.
- CST-03A remains separate and is consumed read-only; no revision-cost decomposition changes are owned here.
- Targeted recent commit searches found no competing `EstimateLine` / `CommercialAdjustmentQuantity` signed-zero lane before registration.

## Completion condition

Zero-valued commercial adjustments are stored and consumed as canonical positive decimal zero without changing non-zero business semantics, focused bit-level regression is on remote `main`, exact source/test readback is verified, and this claim is marked `COMPLETED` with managed/native gates reported honestly.
