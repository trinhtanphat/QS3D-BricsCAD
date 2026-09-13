# 2D Takeoff revision package group summary

## Purpose

`RevisionTakeoffPackageReview2D.BuildResult(...)` now exposes a deterministic `GroupSummaries` projection for Autodesk Takeoff-style package review. The grouping identity matches the downstream 2D quantity aggregation boundary:

`Classification + Zone + Layer + Unit`

This avoids requiring UI callers to regroup raw revision rows independently before showing package deltas or handing current quantities toward Inventory/Estimate workflows.

## Semantics

Each `RevisionPackageGroupSummary2D` contains:

- classification, zone, layer, and unit identity;
- previous-revision quantity;
- current-revision quantity;
- signed revision delta (`current - previous`);
- current estimate-eligible quantity.

Removed evidence remains visible in previous quantity and signed delta for revision review, but contributes zero to `EstimateEligibleCurrentQuantity`. Added, Changed, and Unchanged rows with current evidence remain estimate eligible.

Grouping is case-insensitive, consistent with `TakeoffQuantityAggregator2D`, and output is ordered deterministically by classification, zone, layer, then unit. Quantity accumulation keeps the existing compensated finite-sum behavior and fails closed on non-finite totals.

## Compatibility

This is additive. Existing callers of `RevisionTakeoffPackageReview2D.Build(...)`, `BuildResult(...)`, `Rows`, and `QuantitySummaries` require no changes. `QuantitySummaries` continues to provide package-wide unit totals; `GroupSummaries` is the more detailed Package UX projection for classification/zone/layer drill-down.

No formula, classification, inventory, estimate, overlay, comparer, ingestion, or evidence API is replaced. The new projection consumes the already validated revision review rows so retained-markup grouping metadata ambiguity remains fail-closed.

## Recommended UI flow

Use `Rows` for individual markup review and provenance navigation, `QuantitySummaries` for package-wide unit totals, and `GroupSummaries` for the Package → Classification → Quantity drill-down before Formula → Inventory → Estimate. Estimate publication should continue through `RevisionTakeoffPackage2D.BuildInventoryAndEstimate(...)`, which is bound to current-revision evidence only.
