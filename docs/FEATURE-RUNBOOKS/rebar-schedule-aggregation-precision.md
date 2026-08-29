# BBS aggregate precision

## Scope

This lane covers only aggregate BBS length/weight validation and the derived user-facing BBS totals used by `QS3DBBS`, `QS3DBBSCSV`, and the modeless Rebar Schedule window. It does not change rebar notation parsing, spacing/count policy, individual-row weight formulas, XLSX/CSV schemas, fabrication semantics, native rebar geometry, or licensed runtime qualification.

## Defect

On protected `main@8a96be180dfedab7c198d1d6e509cdf71613a1d8`, `RebarScheduleBuilder.ValidateAggregate` re-sums already validated row totals with strict pairwise `RebarMath.Add`. The BBS command/UI surfaces then independently repeat pairwise aggregation with `QuantityReportMath.Add`.

A deterministic valid input is three `1D18` rows with cutting lengths `5e15`, `0.5`, `0.5` meters. D18 has exactly `2 kg/m`, so the rows carry lengths `5e15`, `0.5`, `0.5` and weights `1e16`, `1`, `1`. The correctly rounded binary64 final totals `5000000000000001` m and `10000000000000002` kg are representable. Pairwise strictness nevertheless rejects the first small addend before the later contribution can recover the representable final value.

## Required contract

- Keep checked aggregate bar quantity and reject negative/invalid row quantities.
- Keep every row value finite and non-negative.
- Use compensated finite accumulation for aggregate length and weight.
- Permit transient binary64 loss when compensation later yields a representable final total.
- Fail closed on non-finite/overflow arithmetic and on material non-zero final compensation that cannot be represented.
- Expose one canonical Core BBS totals helper and reuse it for schedule validation, QS3DBBS XLSX status, QS3DBBSCSV status, and modeless visible totals.
- Preserve detached-snapshot regeneration, SaveFileDialog ordering, export atomicity, null-row refusal, freshness checks, and exception-detail redaction.

## TDD / validation

1. `scripts/preflight-rebar-schedule-aggregation-precision.py` must be RED against the protected-main pairwise implementation.
2. Implement the smallest canonical compensated totals helper in Core and route all BBS presentation/export totals through it.
3. Reconcile historical BBS guards only where they pin obsolete pairwise implementation tokens; preserve their original safety assertions.
4. Require exact-head Shared CI, then reconcile current protected main without force if needed.
5. Require protected pull-request `preflight` and `core` SUCCESS on the current up-to-date candidate before expected-head merge.
6. Verify exact resulting protected main and post-main dispatcher evidence.

## Runtime classification

`NOT_APPLICABLE` for this deterministic Core/source/V25 compile contract. Hosted validation is not a licensed BricsCAD `LOCAL_PASS` claim.
