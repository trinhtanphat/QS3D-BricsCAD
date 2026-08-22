# Work claim — REB-01A canonical rebar stock/cut demand model

- Status: `COMPLETED`
- Agent: `gpt56sol-rebar-procurement-model-20260814-0800`
- Baseline main SHA: `a761e9c88d7df029b4c8a5c61bb6b30ba92d1e19`
- Priority: `REB-01 / P3` specialist rebar depth.

## Confirmed gap

The current Core rebar surface had layout/planning, shape, quantity and fabrication-qualification capabilities but no canonical stock/cut demand model for stock length, required cuts, diameter/grade/group identity, allowance/kerf policy, procurement quantity and off-cut quantity. The roadmap explicitly separates this REB-01 model from REB-02 cutting optimisation and from BBS/report presentation.

## Implemented scope

- `src/QS3D.Core/Rebar/RebarStockDemand.cs`
  - immutable `RebarCutRequirement` with canonical identity, positive finite length and positive quantity;
  - immutable `RebarCutAllowancePolicy` with finite non-negative kerf-per-cut and allowance-per-required-cut policy;
  - bounded `RebarStockDemand` with case-insensitive unique cut identities, stock/diameter/grade/group identity, explicit required-cut length, allowance length and demand-before-kerf totals;
  - `RebarStockProcurementQuantities` with explicit procured stock length, kerf quantity and off-cut quantity;
  - finite/overflow/resource guards reuse the canonical `RebarMath` boundary.
- Kerf is deliberately not inferred from required-piece count because actual cutting operations belong to REB-02. Kerf quantity is result-side data rather than speculative optimisation math.
- `tests/QS3D.Core.SmokeTests/RebarStockDemandSmoke.cs` covers separated quantities, canonical identity, duplicate cut identity, non-finite policy values, and procurement waste bounds.
- `tests/QS3D.Core.SmokeTests/SmokeTestRegistration.cs` registers the focused smoke.

## Coordination / commits

- Claim-first: `bc396b797a74dee0ff1eeb2c63491bea0eb00e96`.
- Initial Core model: `59241683407085f2b8523d327ed8ea883bc17273`.
- Initial focused smoke: `a16482b88ec76c2c9942059a003cc09230302c01`.
- Smoke registration: `e010d1a5cf2cf64ae10a2d61d0331e1adf3350bb`.
- REB-01/REB-02 boundary correction: `25db9f3b98e2728c26956d9a5726aa4688fd790a`.
- Focused semantic regression refinement: `52aba3c902aa3a546a0a5797d5360f3cd883b91a`.
- Concurrent commits were preserved on current `main`; no force update was used.

## Excluded scope

REB-02 cutting optimisation/tie-breaking, BBS/report projection, persistence/schema work, CAD host/native application, standards-specific lap/splice/anchorage rules and procurement pricing are not part of this claim.

## Validation actually executed

- Refreshed `main` and ownership before the claim and again before source writes.
- Read back the exact remote source/test/registration content and verified registration diff only adds `RebarStockDemandSmoke.Run()` (plus final newline normalization).
- Verified `52aba3c902aa3a546a0a5797d5360f3cd883b91a` is an ancestor of the later live `main` (`53044477d37979156d6be4b5952a0636536fb286` was ahead by one unrelated IFC claim and behind by zero).
- GitHub combined status for the registration SHA exposed zero status checks; no Actions were dispatched in this lane.
- The execution container exposes no `dotnet`, `csc` or `mcs`, so no managed executable build/smoke run is reported as PASS. No licensed BricsCAD/native validation was executed.

## Completion condition

Satisfied for this bounded REB-01 Core/static lane: the canonical model and focused regression are on remote `main`, the REB-02 boundary is preserved, concurrent work remains in lineage, and unavailable runtime/native gates are explicitly unclaimed.
