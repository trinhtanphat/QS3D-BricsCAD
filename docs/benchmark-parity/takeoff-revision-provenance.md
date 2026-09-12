# 2D Takeoff revision provenance

QS3D treats a 2D markup's `SourceHandle` as the identity of the measured drawing geometry/evidence within a logical sheet. Revision comparison therefore reports `Changed` when a markup keeps the same markup id, classification, zone, layer, unit and numeric quantity but its source handle changes.

This closes an audit gap where redrawn or replaced geometry could previously appear `Unchanged` merely because the extracted quantity happened to remain equal. The resulting `Changed` delta can legitimately have a zero `QuantityDelta`; Autodesk-style package revision review still marks the sheet/package for review because the underlying evidence changed.

`SourceReference` is intentionally not part of per-markup equivalence. A sheet revision commonly moves from one PDF/image source reference to another while retaining unchanged markup geometry. Comparing the sheet-level source reference per markup would turn every stable markup into a false positive. `SourceHandle` is the bounded per-markup provenance signal; `SourceReference` and sheet `Revision` remain preserved in `TakeoffQuantityEvidence2D` for traceability.

The compatibility contract remains unchanged for classification, zone, layer, unit and quantity comparisons. Source handles are compared ordinally because they are opaque evidence identifiers rather than user-facing classification labels.

Smoke coverage validates changed-handle/zero-quantity-delta behavior, stable-handle behavior across different sheet source references, and propagation through `AutodeskTakeoffPackageRevisionComparer` into `RequiresReview`.