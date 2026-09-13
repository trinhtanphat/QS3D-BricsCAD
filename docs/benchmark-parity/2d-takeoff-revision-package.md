# 2D takeoff revision-aware Package → Estimate

`RevisionTakeoffPackage2D` connects validated drawing revision review to the Autodesk-style Package workflow without changing the existing comparer, overlay, evidence or estimate APIs.

Construct the package from canonical previous/current `TakeoffSheetResult2D` values. The package publishes the deterministic revision overlay plus Added/Removed/Changed/Unchanged counts for Package UX, while `CurrentEvidence` contains only evidence belonging to the current drawing revision.

`BuildInventoryAndEstimate` delegates to the existing `AutodeskTakeoffWorkflow`, so the established Drawing/BIM → Classification → Quantity → Formula → Inventory → Estimate behavior, zone/layer segmentation, compensated aggregation and rate handling remain authoritative. Drawing input is intentionally sourced from `CurrentEvidence`; Previous evidence retained by overlay rows is review provenance only. Consequently a Removed markup cannot leak into Inventory/Estimate.

Compatibility is additive. Existing callers may continue passing evidence directly to `AutodeskTakeoffWorkflow`. Revision-aware Package consumers should prefer `RevisionTakeoffPackage2D` when transitioning between drawing revisions so review provenance and estimate freshness share one explicit boundary.