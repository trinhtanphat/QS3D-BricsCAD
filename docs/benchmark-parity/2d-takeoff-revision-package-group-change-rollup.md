# 2D Takeoff revision package group change-state rollup

## Purpose

`RevisionTakeoffPackageReview2D.BuildResult(...)` already exposes deterministic Package UX summaries grouped by `Classification + Zone + Layer + Unit`. Each `RevisionPackageGroupSummary2D` now also carries change-state counts for the markups inside that same grouping identity.

This lets Autodesk Takeoff-style package review surfaces show quantity deltas and revision-status badges from one canonical projection instead of rescanning raw rows with potentially different grouping rules.

## Group change-state fields

Each group exposes:

- `AddedCount`
- `RemovedCount`
- `ChangedCount`
- `UnchangedCount`
- `MarkupCount`

`MarkupCount` is the checked sum of the four mutually exclusive revision states and therefore represents the exact number of review rows in the group.

Counts are computed from the already validated revision review rows, under the same case-insensitive Classification + Zone + Layer + Unit identity used by the quantity aggregator and the existing group summary projection.

## Quantity and estimate semantics

This change does not alter quantity semantics. Previous/current quantities, signed revision delta, and `EstimateEligibleCurrentQuantity` retain their existing behavior. Removed markups remain review-visible and contribute to `RemovedCount`, previous quantity, and signed delta, while contributing zero to current estimate-eligible quantity.

Added, Changed, and Unchanged rows with current evidence remain estimate eligible through the existing current-revision package path.

## Invariants

For a complete `RevisionPackageReviewResult2D`:

- the sum of `MarkupCount` across all groups equals `Rows.Count`;
- the sum of each group change-state count equals the corresponding package-level Added/Removed/Changed/Unchanged count;
- grouping remains case-insensitive and deterministically ordered by Classification, Zone, Layer, then Unit;
- no group can be empty and count arithmetic uses checked integer addition.

## Compatibility

This is additive. Existing callers of `Build(...)`, `BuildResult(...)`, `Rows`, `QuantitySummaries`, `GroupSummaries`, and all quantity fields require no changes.

No ingestion, calibration, comparer, overlay, formula, classification, inventory, snapshot, or estimate API is replaced.

## Recommended Package UX flow

Use package-level counts for top-level revision badges, `GroupSummaries` for Classification → Zone → Layer → Unit drill-down with status counts and quantities, and `Rows` for individual markup/provenance navigation. Continue publishing estimate data through `RevisionTakeoffPackage2D.BuildInventoryAndEstimate(...)`, which remains bound to current-revision evidence only.
