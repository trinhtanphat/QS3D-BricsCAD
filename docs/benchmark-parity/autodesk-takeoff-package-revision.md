# Autodesk Takeoff package revision comparison

This follow-up extends the canonical 2D takeoff foundation merged in #6474. It does not introduce a second measurement or package engine.

## Workflow

`AutodeskTakeoffPackageRevisionComparer` compares two revisions of the same logical `TakeoffPackageDefinition` and aggregates the existing `DrawingRevisionComparer2D` evidence at package level.

For sheets present in both revisions, existing markup identity drives Added / Removed / Changed / Unchanged classification. A sheet added to a package produces Added deltas for all of its markup evidence; a removed sheet produces Removed deltas. The package result exposes changed-sheet and markup counts, total quantity delta, and `RequiresReview` for UI/workflow gating.

The comparison intentionally uses calibrated `TakeoffQuantityEvidence2D`, preserving sheet revision, source reference, source handle, classification, zone and layer traceability already produced by `CalibratedTakeoffEngine2D`.

## Compatibility

Existing `AutodeskTakeoffPackageCoordinator`, `DrawingRevisionComparer2D`, formula/rate delegates, quantity evidence and estimate workflow remain unchanged. Consumers may adopt package revision review incrementally without changing existing package build calls.

## UX acceptance

A takeoff host should present revision review before accepting a materially changed package. At minimum it should show changed sheets, Added / Removed / Changed markup counts, quantity delta and drill-through to each existing markup evidence record. `RequiresReview` is deterministic and true whenever a sheet has an Added, Removed or Changed markup.
