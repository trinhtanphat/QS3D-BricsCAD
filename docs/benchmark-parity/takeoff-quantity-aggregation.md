# 2D takeoff quantity aggregation contract

This slice closes a P0 auditability/stability gap between calibrated 2D markup evidence and the Autodesk-style Package / Inventory / Estimate workflow.

## Contract

`TakeoffQuantityAggregator2D` consumes immutable `TakeoffQuantityEvidence2D` rows and produces package-ready `TakeoffEvidenceAggregate2D` rows grouped by **Classification + Zone + Unit** using case-insensitive business identity. Each aggregate preserves its source evidence as a deterministic, read-only drill-down ordered by sheet, revision, markup id, and source handle.

The aggregate quantity uses compensated summation rather than a naive `Enumerable.Sum`. This matters when a package contains mixed-magnitude measurements because the Package/Inventory boundary must not create avoidable floating-point drift merely from evidence ordering.

Evidence identity is `(SheetId, Revision, MarkupId)` and is case-insensitive. Duplicate identity is rejected before aggregation. The aggregator therefore fails closed instead of silently double-counting the same logical markup while still allowing a markup id to appear legitimately on another sheet or revision.

## Workflow fit

The intended flow remains:

`Drawing/BIM -> Package -> Classification -> Quantity -> Formula -> Inventory -> Estimate`

For 2D drawing evidence, `CalibratedTakeoffEngine2D` remains responsible for calibrated count/length/area extraction and provenance. `TakeoffQuantityAggregator2D` is the next boundary: it gives Package UX a stable quantity row plus auditable source rows. Existing formula/rate/estimate systems can consume the same classification/zone/unit grouping without losing evidence provenance.

This change is additive and does not alter PDF/raster ingestion, calibration math, revision comparison semantics, IFC/QuantBIM normalization, or licensed BricsCAD host behavior.

## Compatibility and validation

- No migration is required for existing `TakeoffQuantityEvidence2D` producers.
- Empty input returns an empty aggregate list.
- Null evidence rows, duplicate logical evidence identity, and non-finite quantities fail closed.
- Evidence rows remain immutable/read-only at the aggregation boundary.
- Smoke coverage validates case-insensitive grouping, deterministic evidence ordering, multi-sheet evidence counts, duplicate rejection, and compensated mixed-magnitude summation.
