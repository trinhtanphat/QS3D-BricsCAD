# Bulk rate assignment unmatched preview

## Scope

This contract covers deterministic managed-Core behavior when a bulk rate assignment request retains a selected estimating-line id that is no longer present in the current `EstimatingPortfolio`.

## Required behavior

- `PreviewBulkRateAssignment` must return a reviewable preview instead of propagating `KeyNotFoundException` for an unknown selected line id.
- Unknown selected ids are emitted in deterministic `UnmatchedLineIds` ordering and make `CanCommit` false.
- Unknown ids do not contribute source-line state, unit distribution, quantity, commercial totals, replacement count, or audit/mutation authority.
- `AffectedCount` remains the cardinality of the reviewed request so the preview does not silently rewrite the user's selection.
- Matched blocked/stale lines retain their existing blocked handling; missing unit-rate assignments for matched lines continue to use the existing unmatched contract.
- Portfolio line-id lookup remains case-insensitive, matching the canonical portfolio dictionary semantics.
- Commit must refuse any preview that contains unmatched or blocked rows before publication to `CommercialAuditLog`.

## Regression

`BulkRateAssignmentUnmatchedPreviewSmoke` covers mixed known/unknown selection, all-unknown selection, casing, blocked + unknown coexistence, distribution/totals exclusion, and no-audit commit refusal.

`preflight-bulk-rate-assignment-unmatched-preview.py` is auto-discovered by shared feature-source validation and locks the admission ordering: unknown selected ids must be classified before `sourceLines` publication.

## Runtime boundary

This is deterministic Core commercial correctness. No licensed BricsCAD runtime evidence is required or claimed.
