# Estimating bulk-rate preview affected count

Lane-Key: issue-5842

## Defect

`PreviewBulkRateAssignment` distinguishes unresolved selected ids by placing them in `UnmatchedLineIds`, but historical production code reported `AffectedCount` from the raw request cardinality. A request containing one real estimating line and one unknown id therefore reported two affected rows even though only one portfolio row exists.

## Contract

- `AffectedCount` is derived from the resolved `sourceLines` snapshot.
- Unknown selected ids remain in `UnmatchedLineIds` and keep `CanCommit == false`.
- Unknown ids do not contribute unit distribution or commercial totals.
- An all-existing selection preserves one affected row per resolved selected line and remains committable when no blocked/stale/unmatched condition exists.
- `ReplacementCount`, rate provenance, totals, blocked/stale handling and commit stale-preview validation remain unchanged.

## Deterministic evidence

`EstimatingBulkRateAffectedCountSmoke` covers a mixed existing/unknown selection and an all-existing control. `scripts/preflight-estimating-bulk-rate-affected-count.py` pins the production derivation to `sourceLines.Count` and requires the smoke to remain auto-discovered through `ModuleInitializer`.

Runtime classification: REMOTE_SAFE / NOT_APPLICABLE. This is managed Core commercial-preview correctness; licensed BricsCAD runtime evidence is not required and must not be claimed.
