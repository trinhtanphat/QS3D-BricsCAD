# BOQ Review Approval & Manual-Adjustment Ledger

Issue: #6417
Parent program: #6127

## Purpose

This lane adds a professional review/approval loop to the existing Quantity Summary detail workflow without replacing QS3D quantity arithmetic.

`ProjectQuantityReportBuilder.Detail` remains the authoritative measurement source. Review state is persisted separately and keyed by the canonical semantic `ElementId`; CAD handles are provenance only and are never the review identity.

## Review states

- `Pending` â€” draft/not yet reviewed.
- `Reviewed` â€” reviewed but not approved for downstream adjustment use.
- `Approved` â€” a current entry may expose its manual-adjustment overlay through `BqReviewService.EffectiveApprovedValue`.
- `Rejected` â€” requires a review note.

All non-pending states require an explicit reviewer.

## Manual adjustment contract

Manual adjustments are proposals with `metric + delta + reason`. They never rewrite the measured `QuantityReportRow` fields. A proposal must target a metric with authoritative evidence, must be finite, must have a non-zero delta and reason, and cannot make the proposed quantity negative.

Downstream callers must use Core `EffectiveApprovedValue`; stale, pending, reviewed or rejected entries resolve to the original measured value.

## Freshness and provenance

Every review entry stores a SHA-256 source signature over the authoritative detail-row semantics, quantities, evidence flags, drawing fingerprint and canonicalized source handles. The stable key remains `ElementId`.

If any signed measurement/provenance field changes, `IsCurrent` returns false. The UI visibly reports `STALE` and requires the operator to save a new review against a freshly recalculated detail row.

## Persistence

Project-owned state uses the reserved metadata root `QS3D.BQReview.` and a bounded versioned codec. Unsupported reserved keys, malformed payloads, duplicate identities, invalid numbers and trailing payload data fail closed.

## BricsCAD operator flow

1. Open Quantity Summary and switch to detail/explanation mode.
2. Select one semantic element row.
3. Choose review status, reviewer and optional note.
4. Optionally select a supported metric and enter a delta plus reason.
5. Save review. The UI recalculates the authoritative detail row, verifies project/backing-store freshness, snapshots project state, persists the ledger, records an audit event and saves the project.
6. If persistence fails, project state is rolled back and the document cache is discarded rather than leaving a half-saved review.

Clearing a review follows the same snapshot/save/rollback safety boundary.

## Validation boundary

REMOTE_SAFE validation covers Core contracts, smoke tests, XAML/event source guards and protected V25 compile CI. This workstation does not have a licensed BricsCAD runtime, so no interactive/native runtime PASS is claimed here.

The active Review Workbook snapshot/export lane #6409 owns its own export paths; this feature does not modify those files or introduce a second workbook/export authority.
