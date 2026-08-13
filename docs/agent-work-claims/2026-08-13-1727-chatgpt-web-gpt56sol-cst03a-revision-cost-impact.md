# Work claim — CST-03A deterministic revision cost impact

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cst03a-revision-cost-impact-20260813-1727`
- Registered: `2026-08-13T17:27:00+07:00`
- Baseline main SHA: `f335c39d2ef538c92c2709926f25cf6bc9d4c5a3`
- Priority: `CST-03 / P1` — reconcile quantity-driven and rate-driven cost change from two frozen CST-02 EstimateLine states

## Confirmed gap

CST-02A is completed and current history/search contains no CST-03 revision-cost model. The workstream requires previous/current quantity, previous/current rate, quantity delta, rate delta and cost delta to reconcile deterministically, with quantity-driven and rate-driven effects separated where possible.

## Reserved scope

Add one pure-Core immutable `EstimateRevisionCostImpact` comparing two already-frozen `EstimateLine` values in the same comparable commercial scope.

Comparability for this sub-lane is intentionally strict:

- exact `EstimateLineId` must match;
- unit and currency must match exactly;
- `CostCode` identity must match using its canonical case-insensitive value semantics;
- differing measurement/rate snapshots and selected rate versions are allowed and retained through the two source lines.

The impact will expose:

- previous/current measured quantity, commercial adjustment quantity, estimating quantity, unit rate and final amount;
- measured, commercial-adjustment, estimating-quantity and unit-rate deltas;
- quantity-driven cost effect using the explicit convention `(current estimating quantity - previous estimating quantity) * previous unit rate`;
- rate-driven cost effect using `current estimating quantity * (current unit rate - previous unit rate)`;
- total cost delta `current final amount - previous final amount`;
- exact reconciliation `quantity-driven effect + rate-driven effect == total cost delta` using checked decimal arithmetic.

The decomposition convention assigns the quantity/rate interaction term to the rate-driven effect by evaluating the rate delta at current estimating quantity. This convention is explicit in the contract/test; it is not hidden renderer logic.

## Expected surfaces

- new `src/QS3D.Core/Cost/EstimateRevisionCostImpact.cs`;
- new `tests/QS3D.Core.SmokeTests/EstimateRevisionCostImpactSmoke.cs`;
- new `tests/QS3D.Core.SmokeTests/EstimateRevisionCostImpactRegistration.cs`;
- this claim file.

## Excluded scope

- No cross-currency, cross-unit, cross-cost-code or renamed-line reconciliation; those states fail visibly in this narrow lane.
- No mapping-change attribution, geometry/rule reason classification, FX, tax/markup/discount, persistence or renderer/export.
- No edits to EstimateLine, RateBook, MeasurementSnapshot or REV-02 contracts unless a separate blocking defect is proven/claimed.
- No WPF/BricsCAD/native/local qualification and no GitHub Actions dispatch.

## Validation plan

- Re-fetch current main after claim and reconcile Cost/CST overlap.
- Smoke covers quantity-only, rate-only, simultaneous quantity+rate change, commercial-adjustment delta, unchanged state, strict comparability failures and checked-overflow behavior.
- Assert decomposition and total delta reconcile exactly in decimal arithmetic.
- Re-fetch exact source/test/registration blobs before closeout.
- Managed build/smoke remains `NOT_RUN` without an actual .NET toolchain.

## Coordination

- CST-02A is completed and consumed read-only.
- CST-04 renderer/projection remains a future lane.
- Current review/diagnostic/UI/native/MTR/Curtain work is excluded.

## Completion condition

A claim-first immutable revision-cost impact model plus focused registered smoke is on current main; exact deterministic reconciliation is demonstrated by committed regression source; remote blobs are verified; and unexecuted gates are recorded honestly.
