# 2D takeoff revision package summary

`RevisionTakeoffPackageReview2D.BuildResult` adds a package-level projection for Autodesk/CostX-style revision review without changing the existing row API.

## Contract

The result exposes the previous/current drawing revision identifiers, Added/Removed/Changed/Unchanged counts, the deterministic review rows, and quantity summaries grouped by unit.

Quantities are intentionally **not** summed across unlike units. Each `RevisionPackageQuantitySummary2D` reports:

- previous revision quantity;
- current revision quantity;
- signed `current - previous` delta;
- estimate-eligible current quantity.

Removed markups remain visible in review rows and previous/delta totals, but are excluded from the estimate-eligible current subtotal. This preserves the current-revision-only estimate boundary established by `RevisionTakeoffPackage2D`.

Aggregation uses compensated summation and fails closed if a non-finite intermediate/result would be published.

## Compatibility

`RevisionTakeoffPackageReview2D.Build` remains available and returns the same deterministic row list as before. Existing consumers do not need to migrate. Package/UI consumers that need summary badges or quantity cards can opt into `BuildResult`.

No drawing ingestion, scale calibration, markup extraction, classification, formula, inventory, or estimate API is changed by this addition.
