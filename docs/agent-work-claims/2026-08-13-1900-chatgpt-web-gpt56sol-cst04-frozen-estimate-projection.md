# Work claim — CST-04 frozen estimate/BQ projection

- Status: `ACTIVE`
- Agent: `chatgpt-web-gpt56sol-cst04-frozen-estimate-projection-20260813-1900`
- Registered: `2026-08-13T19:00:00+07:00`
- Baseline main SHA: `facf25014e70077ab1c015d7f28ce73afd3968a9`
- Priority: `CST-04 / P1` — frozen estimate/BQ projection consuming canonical EstimateLine state

## Confirmed gap

The current workstream requires CST-04 to provide a frozen estimate/BQ projection where renderer/export consumes canonical estimate state without recreating commercial formulas. Current Core has CST-01 `RateBook`/`RateItem`/`CostCode`, CST-02 `EstimateLine`, and CST-03 `EstimateRevisionCostImpact`, but no dedicated frozen estimate/BQ projection surface in `src/QS3D.Core/Cost`.

## Reserved scope

Add one pure-Core deterministic, detached estimate/BQ projection over already-canonical `EstimateLine` values.

The projection will copy commercial facts from each source line instead of recalculating them. It may expose canonical line identity, measurement/rate provenance identifiers already present on the source line, cost code, unit, currency, measured quantity, commercial adjustment quantity/reason, estimating quantity, unit rate and final amount.

Rows will be materialized in deterministic canonical order and exposed through a read-only collection so later mutation/replacement of caller collections cannot alter the frozen projection.

## Expected surfaces

- new `src/QS3D.Core/Cost/FrozenEstimateProjection.cs`;
- new focused smoke under `tests/QS3D.Core.SmokeTests/` following current registration conventions;
- this claim file.

## Excluded scope

- No commercial recomputation in the projector: no `quantity * rate`, tax, markup, discount, waste formula, FX or valuation logic.
- No changes to `EstimateLine`, `RateBook`, `EstimateRevisionCostImpact`, measurement contracts, persistence, UI renderer, XLSX/PDF exporters or native host code unless a separate blocking defect is proven and claimed.
- No speculative product-boundary expansion from BLT3D research.
- No GitHub Actions dispatch and no native/local PASS claim without an actually available toolchain.

## Validation plan

- Re-fetch current `main` and all current claim metadata after this claim becomes visible; abort/release if a conflicting ACTIVE/BLOCKED CST/Cost projection claim appeared.
- Regression source will prove deterministic ordering, exact preservation of canonical monetary/quantity values, detached/read-only materialization, and acceptance/rejection behavior for invalid projection inputs without reproducing business formulas.
- Re-fetch exact remote source/test/registration blobs after implementation.
- Managed build/smoke remains `NOT_RUN` unless an actual local .NET toolchain is available; GitHub Actions will not be dispatched.

## Coordination

- CST-01/02/03 are consumed read-only as dependencies.
- CST-04 owns only the pure Core frozen estimate/BQ projection contract and its focused regression.
- Mapping, revision diagnostics, native editing, UI rendering/export implementation and unrelated Cost changes remain outside this claim.

## Completion condition

Claim-first projection implementation plus focused registered regression is present on current `main`; remote blobs are verified; the projection is deterministic and formula-free by construction; validation status is recorded accurately; and this claim is closed `COMPLETED` with implementation evidence.