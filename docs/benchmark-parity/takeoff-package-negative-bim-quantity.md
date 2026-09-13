# Takeoff Package negative BIM quantity validation

## Contract

The Autodesk-style package workflow keeps one fail-closed quantity boundary across:

`Drawing/BIM -> Package -> Classification -> Quantity -> Formula -> Inventory -> Estimate`

Source quantity evidence entering a publishable package must be finite and non-negative. 2D drawing evidence already enforces this at `TakeoffQuantityEvidence2D`; the package coordinator now applies the same rule to BIM `IfcQtoItem` input.

A BIM item with `Quantity < 0` produces `PKG.NEGATIVE_BIM_QUANTITY` at Error severity. The package becomes `Blocked`, `CanEstimate` is false, and no formula/rate evaluation, inventory publication, or estimate publication is allowed.

Zero remains valid source evidence and continues to contribute to provenance/evidence counts. Positive finite BIM quantities retain the existing aggregation, classification, formula, rate, and estimate behavior.

## Revision compatibility

Signed revision deltas are intentionally unchanged. A negative `RevisionMarkupDelta2D.QuantityDelta` represents a derived reduction/removal between revisions; it is not negative source quantity evidence and must remain representable.

## Migration guidance

Upstream IFC/QTO importers that currently encode corrections as negative source quantities must normalize those records before creating a Takeoff Package. Model removal/reduction as revision/change evidence rather than silently applying `Abs()` to the source value, because changing sign would destroy provenance semantics.

This carrier deliberately validates at the Takeoff Package boundary instead of changing the global `IfcQtoItem` constructor. Other benchmark aggregation workflows can retain their own compatibility contracts, and the active C02 aggregation carrier remains isolated.
