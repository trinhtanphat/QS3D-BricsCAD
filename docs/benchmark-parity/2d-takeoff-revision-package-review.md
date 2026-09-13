# 2D takeoff revision package review

`RevisionTakeoffPackageReview2D` is the additive UI projection for CostX/Autodesk-style drawing revision review.

It converts the validated `RevisionTakeoffPackage2D.Overlay` into deterministic rows ordered by markup id. Each row carries the change kind, classification, zone, layer, unit, previous/current source references and handles, previous/current quantities, and the signed quantity delta.

## Estimate eligibility

A row is estimate-eligible only when current-revision evidence exists. Added, Changed, and Unchanged rows therefore remain eligible; Removed rows are review-only and cannot be presented as current estimate evidence.

The actual Inventory/Estimate boundary remains `RevisionTakeoffPackage2D.BuildInventoryAndEstimate`, which continues to consume canonical `CurrentEvidence` only.

## Grouping compatibility

For a retained markup id, classification, zone, layer, and unit define the downstream grouping identity. If any of those values changes between revisions, the review projection fails closed instead of flattening two incompatible grouping identities into one ambiguous row. Callers should model that semantic regrouping as remove/add with distinct markup identities.

This is additive: existing revision comparer, overlay, package, quantity, formula, inventory, and estimate APIs are unchanged.