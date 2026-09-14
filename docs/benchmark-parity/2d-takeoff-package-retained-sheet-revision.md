# 2D Takeoff package retained-sheet revision comparison

Autodesk-style takeoff packages have a package release revision and independent drawing-sheet revisions. A package can move from `P5` to `P6` while a retained drawing such as `A101@R3` is unchanged.

## Behavior

`AutodeskTakeoffPackageRevisionComparer` now treats a retained sheet with the same drawing revision as an immutable revision identity rather than routing it through `DrawingRevisionComparer2D`, whose contract requires distinct drawing revisions.

For a retained sheet whose drawing revision is unchanged:

- source kind and source reference must remain identical;
- the markup-id set must remain identical;
- each evidence record must retain sheet/revision/source provenance, handle, classification, zone, layer, unit and quantity;
- valid retained evidence is emitted as deterministic `Unchanged` markup deltas;
- any content or provenance mutation under the same drawing revision fails closed and requires the caller to publish a new drawing revision.

For a retained sheet whose drawing revision changed, the existing drawing revision comparer remains canonical. Added and removed sheets retain their existing synthetic Added/Removed behavior.

Package comparisons also require distinct package revision identifiers. This prevents a single package revision identity from representing two package states.

## Compatibility

The public comparer signature and result types are unchanged. Existing consumers continue to receive `TakeoffPackageRevisionComparison` and `TakeoffPackageRevisionDelta` instances. The only newly admitted case is a package release that retains one or more unchanged drawing revisions; those sheets contribute `Unchanged` rows and zero quantity delta.

This keeps the Package UX consistent with the package workflow `Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate`: unchanged drawing evidence stays review-visible without being misclassified as a revision change, while same-revision content mutation cannot silently alter downstream quantities or estimates.
