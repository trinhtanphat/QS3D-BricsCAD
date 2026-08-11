# QS3D quantity report revision review — source contract

Updated: 2026-08-11

Status: CAD-independent Core source and deterministic smoke coverage. No BricsCAD runtime or adapter behavior is claimed.

## Purpose and authority

`QuantityReportRevisionService` provides the BLT-style quantity-change review layer for two named project revisions without creating another quantity engine.

- `ProjectQuantityReportBuilder.Detail(project)` remains the authoritative BQ/ED2 report projection and supplies every captured report row.
- `RevisionService.Capture` and `RevisionService.Compare` remain the semantic revision/diff authority.
- `QuantityReportRevisionService` only snapshots the existing report projection and classifies its visible row changes.
- Existing `QuantityRevisionReport` remains the raw per-Element/per-quantity report; this service covers the authoritative BQ detail-row view instead of replacing it.

## Snapshot and diff contract

Each in-memory report snapshot records:

- canonical project ID;
- canonical, named snapshot ID;
- source `ProjectState.ChangeVersion`;
- the corresponding semantic `RevisionSnapshot`;
- immutable copies of authoritative detail rows.

Each report row uses its stable semantic Element ID as `StableKey`. Native CAD handles and the drawing fingerprint are deliberately excluded from stable revision identity. Rows and changes are ordered case-insensitively by stable key with ordinal tie-breaking.

Comparison rejects cross-project snapshots and equal snapshot IDs. It emits deterministic Added / Removed / Changed rows. A Changed row lists the report-visible identity, descriptive or quantity fields that differ; Added and Removed rows retain the complete after or before report row respectively.

`SemanticDeltaCount` is contextual validation output from `RevisionService`; it may include semantic changes such as source-reference or non-report fields that do not create a report-row change. It is not a second quantity-change count.

## Safety boundaries

- Capture requires exactly one semantic Element per authoritative detail row.
- Report quantities are checked for finite/overflow behavior by the existing reporting/revision math paths; delta overflow fails closed.
- Capture detects a `ChangeVersion` change during the read and refuses an inconsistent snapshot.
- Capture and Compare are read-only: the service does not mutate `ProjectState`, regenerate quantities, write Audit Trail entries or touch native CAD state.
- The snapshot is currently an in-memory Core review model. Persisted artifact format, export/UI wiring and modeless V25 lifecycle are separate future scopes.

## Regression coverage

`QuantityReportRevisionReviewSmoke` covers deterministic Added / Removed / Changed classification, stable key ordering, report-field changes, cross-project and duplicate-snapshot identity refusal, non-finite capture refusal, overflow refusal and before/after live-project invariants.

`scripts/preflight-quantity-report-revision-review.py` locks the authority boundary and is auto-discovered by the aggregate preflight runner.
