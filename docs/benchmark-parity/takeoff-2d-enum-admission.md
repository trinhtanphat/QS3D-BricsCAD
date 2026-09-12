# 2D Takeoff enum admission

The P0 2D Takeoff boundary now rejects undefined enum values before they can enter quantity or provenance workflows.

## Contract

- `DrawingSheet2D` accepts only `DrawingSheetSourceKind.Pdf` and `DrawingSheetSourceKind.RasterImage`.
- `TakeoffMarkup2D` accepts only `TakeoffMeasurementKind.Count`, `Length`, and `Area`.
- Undefined values created by casts, deserialization, or future incompatible callers fail closed with `ArgumentOutOfRangeException`.
- Existing valid PDF/raster sheets and count/length/area markups are unchanged.

## Why this matters

Before this hardening, an undefined measurement value reached `CalibratedTakeoffEngine2D`, whose fallback branch interpreted every non-count/non-length value as area. That could publish a scaled area quantity and squared unit for an invalid markup. Undefined sheet-source values could likewise be retained as provenance even though no supported ingestion path existed for them.

Rejecting invalid discriminators at construction keeps Drawing/BIM → Package → Classification → Quantity → Formula → Inventory → Estimate evidence deterministic and auditable.
